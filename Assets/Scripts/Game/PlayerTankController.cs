using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using TMPro;
using UnityEngine.UI;

namespace DiepMultiplayer
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerTankController : TankBase, IPunObservable
    {
        [Header("Player Components")]
        public Rigidbody rb;
        public Camera playerCamera;
        public float recoilForce = 2.5f;

        [Header("Floating Overhead UI")]
        public Canvas overheadCanvas;
        public TMP_Text nameText;
        public Slider healthSlider;
        public TMP_Text ammoText;

        [Header("Legacy Prefab References")]
        public GameObject bulletPrefab;

        // Network sync variables for remote players
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
            isBot = false;

            if (overheadCanvas != null)
            {
                overheadCanvas.gameObject.SetActive(false);
            }

            bool isLocal = (photonView == null || !PhotonNetwork.IsConnected || photonView.IsMine);

            if (isLocal)
            {
                tankName = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Player" : PhotonNetwork.NickName;
                actorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : 1;

                if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
                {
                    ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                    {
                        { "Kills", 0 },
                        { "Name", tankName }
                    };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                }

                CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
                if (camFollow != null)
                {
                    camFollow.SetTarget(transform);
                }
            }
            else
            {
                actorNumber = photonView.Owner != null ? photonView.Owner.ActorNumber : 2;
                tankName = photonView.Owner != null ? photonView.Owner.NickName : "Player";
                rb.isKinematic = true;
            }

            SetupTronCycleVisuals(GetAssignedNeonColor(isLocal, actorNumber));
            InitializeDirectionAndTrail(GetDirectionTowardsCenter(transform.position));
            networkPosition = transform.position;
            networkRotation = transform.rotation;

            UpdateOverheadUI();
        }

        protected override bool IsAuthoritativeInstance()
        {
            return (photonView == null || !PhotonNetwork.IsConnected || photonView.IsMine);
        }

        public static Vector3 GetDirectionTowardsCenter(Vector3 pos)
        {
            Vector3 toCenter = -pos;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude < 1f) return Vector3.forward;
            return SnapToCardinal(toCenter);
        }

        protected override void Update()
        {
            base.Update();

            if (IsAuthoritativeInstance())
            {
                HandleTronTurnInput();
                ProcessQueuedTurn();
                UpdateTrailSpawning();
                CheckAuthoritativeCollisions();
                UpdateOverheadUI();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UpdateHUD(currentHealth, maxHealth, kills, currentPowerUp, powerUpTimer);
                }
            }
            else
            {
                // Smooth interpolation for remote players
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 18f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 22f);
                UpdateTrailSpawning();
                UpdateOverheadUI();
            }
        }

        private void FixedUpdate()
        {
            if (IsAuthoritativeInstance() && currentHealth > 0)
            {
                HandleContinuousTronMovement();
            }
        }

        private void HandleTronTurnInput()
        {
            if (Keyboard.current == null || currentHealth <= 0) return;

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                RequestTurn90(Vector3.forward);
            }
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                RequestTurn90(Vector3.back);
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                RequestTurn90(Vector3.right);
            }
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                RequestTurn90(Vector3.left);
            }
        }

        private void HandleContinuousTronMovement()
        {
            Vector3 vel = moveDirection * currentMoveSpeed;
            vel.y = 0f;
            rb.linearVelocity = vel;
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        public override void ApplyPowerUp(PowerUpType type, float duration)
        {
            if (type == PowerUpType.EMP)
            {
                if (IsAuthoritativeInstance() && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null)
                {
                    photonView.RPC(nameof(RPC_TriggerPlayerEMP), RpcTarget.All);
                    return;
                }
            }
            base.ApplyPowerUp(type, duration);
        }

        [PunRPC]
        public void RPC_TriggerPlayerEMP()
        {
            base.ApplyPowerUp(PowerUpType.EMP, 2.0f);
        }

        public override void Die(string attackerName, int attackerActorNumber, bool attackerIsBot)
        {
            if (IsAuthoritativeInstance())
            {
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null)
                {
                    photonView.RPC(nameof(RPC_NotifyPlayerDied), RpcTarget.All, attackerName, attackerActorNumber, attackerIsBot);
                }
                else
                {
                    RPC_NotifyPlayerDied(attackerName, attackerActorNumber, attackerIsBot);
                }
            }
        }

        [PunRPC]
        public void RPC_NotifyPlayerDied(string attackerName, int attackerActorNumber, bool attackerIsBot)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayExplosion(transform.position);
                if (IsAuthoritativeInstance())
                {
                    SoundManager.Instance.PlayGameOver();
                }
            }

            TronExplosionVFX.SpawnCrash(transform.position, moveDirection, cycleNeonColor, 1.0f, false);

            // Hapus semua jejak cahaya milik player yang mati
            ClearAllTrails();
            currentHealth = 0;

            // Sembunyikan kendaraan untuk semua client
            gameObject.SetActive(false);

            // Beri kill ke penyerang (jika bukan bunuh diri ke dinding/jejak sendiri)
            if (attackerActorNumber != -1)
            {
                if (!attackerIsBot && PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == attackerActorNumber)
                {
                    PlayerTankController localPlayer = GameManager.Instance != null ? GameManager.Instance.localPlayerTank : this;
                    if (localPlayer != null)
                    {
                        localPlayer.kills++;
                        if (PhotonNetwork.IsConnected)
                        {
                            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                            {
                                { "Kills", localPlayer.kills }
                            };
                            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                        }
                    }
                }
                else if (attackerIsBot && (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddBotKill(attackerActorNumber);
                    }
                }
            }

            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.RefreshLeaderboard();
            }

            if (IsAuthoritativeInstance())
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnLocalPlayerDied(attackerName);
                }
            }
        }

        public override void Respawn()
        {
            if (!IsAuthoritativeInstance()) return;

            Vector3 spawnPos = GameManager.Instance != null ? GameManager.Instance.GetRandomSpawnPosition() : Vector3.zero;
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && photonView != null)
            {
                photonView.RPC(nameof(RPC_RespawnPlayer), RpcTarget.All, spawnPos);
            }
            else
            {
                RPC_RespawnPlayer(spawnPos);
            }
        }

        [PunRPC]
        public void RPC_RespawnPlayer(Vector3 spawnPos)
        {
            ClearAllTrails();
            currentHealth = maxHealth;
            ResetPowerUps();

            // Reset kill count dan ranking kembali menjadi 0 saat restart
            kills = 0;
            if (IsAuthoritativeInstance())
            {
                if (PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer != null)
                {
                    ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                    {
                        { "Kills", 0 }
                    };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                }
            }

            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.RefreshLeaderboard();
            }

            transform.position = spawnPos;
            networkPosition = spawnPos;
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
            }

            InitializeDirectionAndTrail(GetDirectionTowardsCenter(spawnPos));
            networkRotation = transform.rotation;

            gameObject.SetActive(true);
            UpdateOverheadUI();

            if (IsAuthoritativeInstance())
            {
                CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
                if (camFollow != null) camFollow.SetTarget(transform);
            }
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
