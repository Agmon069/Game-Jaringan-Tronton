using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace DiepMultiplayer
{
    [RequireComponent(typeof(Rigidbody))]
    public class BotController : TankBase, IPunObservable
    {
        [Header("Bot Components")]
        public Rigidbody rb;
        public float recoilForce = 2.0f;
        public float detectionRadius = 30f;
        public float attackRange = 18f;

        [Header("Floating Overhead UI")]
        public Canvas overheadCanvas;
        public TMP_Text nameText;
        public Slider healthSlider;
        public TMP_Text ammoText;

        [Header("Legacy Prefab References")]
        public GameObject bulletPrefab;

        // AI state
        private float nextAITurnTime = 0f;

        // Network interpolation for non-master clients
        private Vector3 networkPosition;
        private Quaternion networkRotation;

        protected override void Awake()
        {
            base.Awake();
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.useGravity = false;
            }
            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            isBot = true;

            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }

            if (IsAuthoritativeInstance())
            {
                rb.isKinematic = false;
            }
            else
            {
                rb.isKinematic = true;
            }

            int colorSeed = actorNumber > 0 ? actorNumber : GetInstanceID();
            SetupTronCycleVisuals(GetAssignedNeonColor(false, colorSeed));
            InitializeDirectionAndTrail(PlayerTankController.GetDirectionTowardsCenter(transform.position));
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            nextAITurnTime = Time.time + Random.Range(1.0f, 2.5f);

            UpdateOverheadUI();
        }

        protected override bool IsAuthoritativeInstance()
        {
            return (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient);
        }

        public void InitializeBot(string botName, int botId)
        {
            tankName = botName;
            actorNumber = botId;
            SetupTronCycleVisuals(GetAssignedNeonColor(false, botId));
            UpdateOverheadUI();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_SyncBotIdentity), RpcTarget.OthersBuffered, botName, botId);
            }
        }

        [PunRPC]
        public void RPC_SyncBotIdentity(string botName, int botId)
        {
            tankName = botName;
            actorNumber = botId;
            SetupTronCycleVisuals(GetAssignedNeonColor(false, botId));
        }

        protected override void Update()
        {
            base.Update();

            if (IsAuthoritativeInstance())
            {
                HandleTronAI();
                ProcessQueuedTurn();
                UpdateTrailSpawning();
                CheckAuthoritativeCollisions();
                UpdateOverheadUI();
            }
            else
            {
                // Smooth interpolation for clients
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 16f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 20f);
                UpdateTrailSpawning();
                UpdateOverheadUI();
            }
        }

        private void FixedUpdate()
        {
            if (IsAuthoritativeInstance() && currentHealth > 0)
            {
                Vector3 vel = moveDirection * currentMoveSpeed;
                vel.y = 0f;
                rb.linearVelocity = vel;
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }

        private void HandleTronAI()
        {
            if (currentHealth <= 0) return;

            float forwardClear = GetClearDistanceInDirection(moveDirection, 28f);
            Vector3 leftDir = SnapToCardinal(Vector3.Cross(moveDirection, Vector3.up));
            Vector3 rightDir = -leftDir;

            // 1. Emergency obstacle & wall avoidance
            float dangerThreshold = (currentPowerUp == PowerUpType.SpeedBoost) ? 8.5f : 6.2f;
            if (forwardClear < dangerThreshold)
            {
                float leftClear = GetClearDistanceInDirection(leftDir, 32f);
                float rightClear = GetClearDistanceInDirection(rightDir, 32f);

                if (leftClear > rightClear + 0.5f)
                {
                    RequestTurn90(leftDir);
                }
                else if (rightClear > leftClear + 0.5f)
                {
                    RequestTurn90(rightDir);
                }
                else
                {
                    RequestTurn90(Random.value > 0.5f ? leftDir : rightDir);
                }

                nextAITurnTime = Time.time + Random.Range(0.7f, 1.8f);
                return;
            }

            // 2. Tactical turns (seek power-ups, cut off enemies, or grid weave)
            if (Time.time >= nextAITurnTime)
            {
                float leftClear = GetClearDistanceInDirection(leftDir, 28f);
                float rightClear = GetClearDistanceInDirection(rightDir, 28f);

                // Try seeking nearby power-up
                PowerUpItem targetPU = FindNearestPowerUp(22f);
                if (targetPU != null)
                {
                    Vector3 toPU = (targetPU.transform.position - transform.position).normalized;
                    if (Vector3.Dot(toPU, leftDir) > 0.55f && leftClear > 10f)
                    {
                        RequestTurn90(leftDir);
                        nextAITurnTime = Time.time + Random.Range(0.9f, 2.0f);
                        return;
                    }
                    if (Vector3.Dot(toPU, rightDir) > 0.55f && rightClear > 10f)
                    {
                        RequestTurn90(rightDir);
                        nextAITurnTime = Time.time + Random.Range(0.9f, 2.0f);
                        return;
                    }
                }

                // Otherwise make a safe tactical turn to weave a light barrier
                if (leftClear > 12f && rightClear > 12f)
                {
                    RequestTurn90(Random.value > 0.5f ? leftDir : rightDir);
                }
                else if (leftClear > 12f)
                {
                    RequestTurn90(leftDir);
                }
                else if (rightClear > 12f)
                {
                    RequestTurn90(rightDir);
                }

                nextAITurnTime = Time.time + Random.Range(1.2f, 2.8f);
            }
        }

        private float GetClearDistanceInDirection(Vector3 dir, float maxCheckDist)
        {
            dir = SnapToCardinal(dir);
            Vector3 pos = transform.position;

            // Distance to arena boundary wall
            float wallDist = maxCheckDist;
            if (dir.x > 0.5f) wallDist = arenaBoundLimit - pos.x;
            else if (dir.x < -0.5f) wallDist = pos.x - (-arenaBoundLimit);
            else if (dir.z > 0.5f) wallDist = arenaBoundLimit - pos.z;
            else if (dir.z < -0.5f) wallDist = pos.z - (-arenaBoundLimit);

            float minDist = Mathf.Clamp(wallDist, 0f, maxCheckDist);

            // Check trail segments and cycles along ray
            Vector3 castOrigin = new Vector3(pos.x, 0.5f, pos.z) + dir * 0.6f;
            RaycastHit[] hits = Physics.SphereCastAll(castOrigin, 0.55f, dir, minDist);
            foreach (var h in hits)
            {
                if (h.collider == null) continue;

                TronTrailSegment seg = h.collider.GetComponent<TronTrailSegment>();
                if (seg != null)
                {
                    // If Shield is active, enemy trails aren't fatal obstacles
                    bool isOwn = (seg.ownerActorNumber == actorNumber && seg.ownerIsBot == isBot);
                    if (isOwn && (Time.time - seg.spawnTime) < 0.5f) continue;
                    if (isShieldActive && !isOwn) continue;

                    if (h.distance < minDist) minDist = h.distance;
                    continue;
                }

                TankBase otherCycle = h.collider.GetComponentInParent<TankBase>();
                if (otherCycle != null && otherCycle != this)
                {
                    if (h.distance < minDist) minDist = h.distance;
                }
            }

            return minDist;
        }

        private PowerUpItem FindNearestPowerUp(float maxDist)
        {
            PowerUpItem[] items = FindObjectsByType<PowerUpItem>(FindObjectsSortMode.None);
            PowerUpItem best = null;
            float bestDist = maxDist;

            foreach (var item in items)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                float d = Vector3.Distance(transform.position, item.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = item;
                }
            }
            return best;
        }

        public override void ApplyPowerUp(PowerUpType type, float duration)
        {
            if (type == PowerUpType.EMP)
            {
                if (IsAuthoritativeInstance() && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null)
                {
                    photonView.RPC(nameof(RPC_TriggerBotEMP), RpcTarget.All);
                    return;
                }
            }
            base.ApplyPowerUp(type, duration);
        }

        [PunRPC]
        public void RPC_TriggerBotEMP()
        {
            base.ApplyPowerUp(PowerUpType.EMP, 2.0f);
        }

        public override void Die(string attackerName, int attackerActorNumber, bool attackerIsBot)
        {
            if (IsAuthoritativeInstance())
            {
                if (attackerActorNumber != -1 && attackerIsBot)
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddBotKill(attackerActorNumber);
                    }
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RespawnBotAfterDelay(this, 3.0f);
                }

                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null)
                {
                    photonView.RPC(nameof(RPC_NotifyBotDied), RpcTarget.All, attackerName, attackerActorNumber, attackerIsBot);
                }
                else
                {
                    RPC_NotifyBotDied(attackerName, attackerActorNumber, attackerIsBot);
                }
            }
        }

        [PunRPC]
        public void RPC_NotifyBotDied(string attackerName, int attackerActorNumber, bool attackerIsBot)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayExplosion(transform.position);
            }

            // Hapus semua jejak cahaya milik bot yang mati
            ClearAllTrails();
            currentHealth = 0;

            gameObject.SetActive(false);

            if (attackerActorNumber != -1 && !attackerIsBot)
            {
                bool isLocalKiller = (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == attackerActorNumber)
                                     || (!PhotonNetwork.IsConnected && attackerActorNumber == 1);

                if (isLocalKiller)
                {
                    PlayerTankController localTank = GameManager.Instance != null ? GameManager.Instance.localPlayerTank : FindFirstObjectByType<PlayerTankController>();
                    if (localTank != null)
                    {
                        localTank.kills++;
                        if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
                        {
                            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                            {
                                { "Kills", localTank.kills }
                            };
                            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                        }
                    }
                }
            }

            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.RefreshLeaderboard();
            }
        }

        [PunRPC]
        public void RPC_RespawnBot()
        {
            gameObject.SetActive(true);
            Respawn();
        }

        public override void Respawn()
        {
            ClearAllTrails();
            currentHealth = maxHealth;
            ResetPowerUps();

            Vector3 spawnPos = GameManager.Instance != null ? GameManager.Instance.GetRandomSpawnPosition() : Vector3.zero;
            transform.position = spawnPos;
            networkPosition = spawnPos;
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }

            InitializeDirectionAndTrail(PlayerTankController.GetDirectionTowardsCenter(spawnPos));
            networkRotation = transform.rotation;
            nextAITurnTime = Time.time + Random.Range(1.0f, 2.5f);

            gameObject.SetActive(true);
            UpdateOverheadUI();
        }

        private void UpdateOverheadUI()
        {
            if (overheadCanvas != null && overheadCanvas.gameObject.activeSelf)
            {
                overheadCanvas.gameObject.SetActive(false);
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(moveDirection);
                stream.SendNext(currentHealth);
                stream.SendNext(kills);
                stream.SendNext((int)currentPowerUp);
                stream.SendNext(powerUpTimer);
                stream.SendNext(isShieldActive);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkRotation = (Quaternion)stream.ReceiveNext();
                moveDirection = (Vector3)stream.ReceiveNext();
                currentHealth = (int)stream.ReceiveNext();
                kills = (int)stream.ReceiveNext();
                int powerUpInt = (int)stream.ReceiveNext();
                powerUpTimer = (float)stream.ReceiveNext();
                isShieldActive = (bool)stream.ReceiveNext();

                if (currentPowerUp != (PowerUpType)powerUpInt)
                {
                    currentPowerUp = (PowerUpType)powerUpInt;
                }
                if (shieldBubble != null && shieldBubble.activeSelf != isShieldActive)
                {
                    shieldBubble.SetActive(isShieldActive);
                }
            }
        }
    }
}
