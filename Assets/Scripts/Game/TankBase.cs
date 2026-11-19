using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

namespace DiepMultiplayer
{
    public class TronTrailSegment : MonoBehaviour
    {
        public TankBase ownerTank;
        public string ownerName = "";
        public int ownerActorNumber = -1;
        public bool ownerIsBot = false;
        public float spawnTime;
        public float lifetime = 4.5f;
        public float initialHeight = 1.15f;
        public Vector3 initialScale;

        public void Initialize(TankBase owner, float duration, float height)
        {
            ownerTank = owner;
            ownerName = owner != null ? owner.tankName : "LightCycle";
            ownerActorNumber = owner != null ? owner.actorNumber : -1;
            ownerIsBot = owner != null && owner.isBot;
            spawnTime = Time.time;
            lifetime = duration;
            initialHeight = height;
            initialScale = transform.localScale;
        }

        private void Update()
        {
            float elapsed = Time.time - spawnTime;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Smooth digital shrink in the last 0.35s before disappearing
            float remaining = lifetime - elapsed;
            if (remaining < 0.35f)
            {
                float ratio = Mathf.Clamp01(remaining / 0.35f);
                transform.localScale = new Vector3(initialScale.x * ratio, initialScale.y * ratio, initialScale.z);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TankBase hitTank = other.GetComponentInParent<TankBase>();
            if (hitTank != null)
            {
                hitTank.OnHitTrailSegment(this);
            }
        }
    }

    public class TronEMPShockwave : MonoBehaviour
    {
        public float duration = 0.45f;
        public float maxRadius = 34f;
        private float startTime;

        private void Start()
        {
            startTime = Time.time;
        }

        private void Update()
        {
            float t = (Time.time - startTime) / duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float radius = Mathf.Lerp(1.5f, maxRadius * 2f, t);
            transform.localScale = new Vector3(radius, 0.12f, radius);
        }
    }

    public abstract class TankBase : MonoBehaviourPunCallbacks
    {
        [Header("Light Cycle Identity")]
        public string tankName = "LightCycle";
        public int actorNumber = -1;
        public bool isBot = false;

        [Header("Stats")]
        public int maxHealth = 100;
        public int currentHealth = 100;
        public int ammo = 0;
        public int kills = 0;
        public float baseMoveSpeed = 20f;
        public float currentMoveSpeed = 20f;

        [Header("Legacy Shooting Config (Unused in Tron)")]
        public float baseFireRate = 0.28f;
        public float currentFireRate = 0.28f;
        protected float nextFireTime = 0f;

        [Header("Visual References")]
        public GameObject bodySphere;
        public Transform turretPivot;
        public GameObject defaultBarrel;
        public GameObject tripleBarrels;
        public GameObject bigBarrel;
        public GameObject shieldBubble;

        [Header("Legacy Fire Points")]
        public Transform defaultFirePoint;
        public Transform[] tripleFirePoints;
        public Transform bigFirePoint;

        [Header("Power-Up State")]
        public PowerUpType currentPowerUp = PowerUpType.None;
        public float powerUpTimer = 0f;
        public bool isShieldActive = false;

        [Header("Tron Grid & Trail Settings")]
        public Vector3 moveDirection = Vector3.forward;
        protected Vector3 queuedTurnDirection = Vector3.zero;
        protected Vector3 lastTurnPosition;
        public float minDistanceBetweenTurns = 1.35f;
        public float baseTrailLifetime = 4.5f;
        public float boostedTrailLifetime = 9.0f;
        public float baseTrailWidth = 0.36f;
        public float boostedTrailWidth = 0.80f;
        public float trailHeight = 1.1f;
        public float segmentStepDistance = 0.65f;
        public float arenaBoundLimit = 33.4f;
        public float spawnGraceTimer = 0.5f;

        public Color cycleNeonColor = new Color(0f, 0.95f, 1f);
        protected Vector3 lastTrailPoint;
        protected bool trailInitialized = false;
        protected List<TronTrailSegment> activeTrailSegments = new List<TronTrailSegment>();
        protected Material cachedTrailMat;
        protected Material cachedDarkMat;
        protected GameObject tronVisualRoot;

        private static readonly Color[] EnemyNeonPalette = new Color[]
        {
            new Color(1.0f, 0.32f, 0.0f),   // Classic Tron Orange-Red
            new Color(1.0f, 0.08f, 0.68f),  // Neon Magenta
            new Color(0.15f, 1.0f, 0.25f),  // Neon Lime
            new Color(1.0f, 0.85f, 0.05f),  // Neon Gold
            new Color(0.65f, 0.25f, 1.0f),  // Electric Purple
            new Color(1.0f, 0.12f, 0.22f)   // Neon Crimson
        };

        protected virtual void Awake()
        {
            baseMoveSpeed = 20f;
            currentHealth = maxHealth;
            currentMoveSpeed = baseMoveSpeed;
            currentFireRate = baseFireRate;
            SetWeaponVisual(PowerUpType.None);
        }

        protected virtual void Update()
        {
            if (spawnGraceTimer > 0f)
            {
                spawnGraceTimer -= Time.deltaTime;
            }

            UpdatePowerUpTimer();
        }

        protected virtual void OnDisable()
        {
            ClearAllTrails();
        }

        protected virtual void OnDestroy()
        {
            ClearAllTrails();
        }

        public Color GetAssignedNeonColor(bool isLocalPlayer, int id)
        {
            if (isLocalPlayer)
            {
                return new Color(0.0f, 0.95f, 1.0f); // Iconic Tron Cyan for local player
            }
            int idx = Mathf.Abs(id) % EnemyNeonPalette.Length;
            return EnemyNeonPalette[idx];
        }

        public void SetupTronCycleVisuals(Color neonColor)
        {
            cycleNeonColor = neonColor;

            // Hide legacy tank visuals
            if (bodySphere != null) bodySphere.SetActive(false);
            if (turretPivot != null) turretPivot.gameObject.SetActive(false);
            if (defaultBarrel != null) defaultBarrel.SetActive(false);
            if (tripleBarrels != null) tripleBarrels.SetActive(false);
            if (bigBarrel != null) bigBarrel.SetActive(false);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            cachedTrailMat = new Material(shader);
            cachedTrailMat.color = neonColor;
            if (cachedTrailMat.HasProperty("_EmissionColor"))
            {
                cachedTrailMat.EnableKeyword("_EMISSION");
                cachedTrailMat.SetColor("_EmissionColor", neonColor * 1.8f);
            }

            cachedDarkMat = new Material(shader);
            cachedDarkMat.color = new Color(0.07f, 0.08f, 0.12f);

            Transform existingVisual = transform.Find("TronCycleVisual");
            if (existingVisual != null)
            {
                tronVisualRoot = existingVisual.gameObject;
                ApplyNeonMaterialToVisual(tronVisualRoot.transform);
            }
            else
            {
                BuildProceduralTronCycle();
            }

            if (shieldBubble != null)
            {
                shieldBubble.transform.localScale = new Vector3(2.1f, 2.0f, 3.0f);
            }
        }

        private void ApplyNeonMaterialToVisual(Transform root)
        {
            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                if (mr.gameObject.name.Contains("Neon") || mr.gameObject.name.Contains("Wheel") || mr.gameObject.name.Contains("Canopy"))
                {
                    mr.sharedMaterial = cachedTrailMat;
                }
                else
                {
                    mr.sharedMaterial = cachedDarkMat;
                }
            }
        }

        private void BuildProceduralTronCycle()
        {
            tronVisualRoot = new GameObject("TronCycleVisual");
            tronVisualRoot.transform.SetParent(transform);
            tronVisualRoot.transform.localPosition = Vector3.zero;
            tronVisualRoot.transform.localRotation = Quaternion.identity;

            // 1. Dark Cyber Chassis Core
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "ChassisCore";
            chassis.transform.SetParent(tronVisualRoot.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            chassis.transform.localScale = new Vector3(0.72f, 0.48f, 2.0f);
            chassis.GetComponent<MeshRenderer>().sharedMaterial = cachedDarkMat;
            Destroy(chassis.GetComponent<Collider>());

            // 2. Glowing Neon Canopy Spine
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "NeonCanopy";
            canopy.transform.SetParent(tronVisualRoot.transform);
            canopy.transform.localPosition = new Vector3(0f, 0.52f, -0.05f);
            canopy.transform.localScale = new Vector3(0.46f, 0.28f, 1.45f);
            canopy.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            Destroy(canopy.GetComponent<Collider>());

            // 3. Glowing Side Neon Rails
            GameObject sideRails = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideRails.name = "NeonRails";
            sideRails.transform.SetParent(tronVisualRoot.transform);
            sideRails.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            sideRails.transform.localScale = new Vector3(0.86f, 0.14f, 1.75f);
            sideRails.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            Destroy(sideRails.GetComponent<Collider>());

            // 4. Front Light Wheel
            GameObject frontWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            frontWheel.name = "FrontWheelNeon";
            frontWheel.transform.SetParent(tronVisualRoot.transform);
            frontWheel.transform.localPosition = new Vector3(0f, 0.24f, 0.92f);
            frontWheel.transform.localScale = new Vector3(0.45f, 0.72f, 0.78f);
            frontWheel.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            Destroy(frontWheel.GetComponent<Collider>());

            // 5. Rear Light Wheel
            GameObject rearWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearWheel.name = "RearWheelNeon";
            rearWheel.transform.SetParent(tronVisualRoot.transform);
            rearWheel.transform.localPosition = new Vector3(0f, 0.24f, -0.92f);
            rearWheel.transform.localScale = new Vector3(0.48f, 0.72f, 0.78f);
            rearWheel.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            Destroy(rearWheel.GetComponent<Collider>());
        }

        public void InitializeDirectionAndTrail(Vector3 initialDir)
        {
            moveDirection = SnapToCardinal(initialDir);
            queuedTurnDirection = Vector3.zero;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            lastTurnPosition = transform.position;
            lastTrailPoint = GetRearEmitterPosition();
            trailInitialized = true;
            spawnGraceTimer = 0.55f;
        }

        public static Vector3 SnapToCardinal(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return Vector3.forward;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            {
                return dir.x >= 0f ? Vector3.right : Vector3.left;
            }
            return dir.z >= 0f ? Vector3.forward : Vector3.back;
        }

        public Vector3 GetRearEmitterPosition()
        {
            Vector3 pos = transform.position;
            pos.y = 0.5f;
            return pos;
        }

        public bool RequestTurn90(Vector3 desiredDir)
        {
            desiredDir = SnapToCardinal(desiredDir);

            // Only allow 90-degree perpendicular turns (prevents 180-degree instant self-collision)
            if (Mathf.Abs(Vector3.Dot(desiredDir, moveDirection)) > 0.1f)
            {
                // If we already have a queued perpendicular turn, allow queuing a second turn perpendicular to the queued turn (for fast U-turns)
                if (queuedTurnDirection != Vector3.zero && Mathf.Abs(Vector3.Dot(desiredDir, queuedTurnDirection)) < 0.1f)
                {
                    return false;
                }
                return false;
            }

            float distSinceLastTurn = Vector3.Distance(transform.position, lastTurnPosition);
            if (distSinceLastTurn >= minDistanceBetweenTurns)
            {
                ExecuteTurn(desiredDir);
                return true;
            }
            else
            {
                queuedTurnDirection = desiredDir;
                return false;
            }
        }

        protected void ProcessQueuedTurn()
        {
            if (queuedTurnDirection != Vector3.zero)
            {
                float distSinceLastTurn = Vector3.Distance(transform.position, lastTurnPosition);
                if (distSinceLastTurn >= minDistanceBetweenTurns)
                {
                    Vector3 turnDir = queuedTurnDirection;
                    queuedTurnDirection = Vector3.zero;
                    if (Mathf.Abs(Vector3.Dot(turnDir, moveDirection)) < 0.1f)
                    {
                        ExecuteTurn(turnDir);
                    }
                }
            }
        }

        protected void ExecuteTurn(Vector3 newCardinalDir)
        {
            // Seal trail segment right up to the turn corner
            if (trailInitialized && spawnGraceTimer <= 0.25f)
            {
                Vector3 cornerPos = transform.position;
                cornerPos.y = 0.5f;
                if (Vector3.Distance(lastTrailPoint, cornerPos) > 0.15f)
                {
                    SpawnTrailSegmentBetween(lastTrailPoint, cornerPos);
                }
                lastTrailPoint = cornerPos;
            }

            moveDirection = newCardinalDir;
            transform.rotation = Quaternion.LookRotation(moveDirection);
            lastTurnPosition = transform.position;
            queuedTurnDirection = Vector3.zero;
        }

        protected void UpdateTrailSpawning()
        {
            if (currentHealth <= 0 || !gameObject.activeInHierarchy) return;

            Vector3 rearPos = GetRearEmitterPosition();
            if (!trailInitialized)
            {
                lastTrailPoint = rearPos;
                trailInitialized = true;
                return;
            }

            float dist = Vector3.Distance(lastTrailPoint, rearPos);
            if (dist >= segmentStepDistance)
            {
                // Avoid spawning huge teleport segments on respawn
                if (dist < 8f)
                {
                    SpawnTrailSegmentBetween(lastTrailPoint, rearPos);
                }
                lastTrailPoint = rearPos;
            }
        }

        protected void SpawnTrailSegmentBetween(Vector3 startPos, Vector3 endPos)
        {
            Vector3 diff = endPos - startPos;
            diff.y = 0f;
            float length = diff.magnitude;
            if (length < 0.08f) return;

            bool isTrailBoosted = (currentPowerUp == PowerUpType.TrailBoost);
            float width = isTrailBoosted ? boostedTrailWidth : baseTrailWidth;
            float duration = isTrailBoosted ? boostedTrailLifetime : baseTrailLifetime;

            GameObject segObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segObj.name = "TronTrail_" + tankName;

            Vector3 mid = (startPos + endPos) * 0.5f;
            mid.y = trailHeight * 0.5f;
            segObj.transform.position = mid;
            segObj.transform.rotation = Quaternion.LookRotation(diff.normalized);
            segObj.transform.localScale = new Vector3(width, trailHeight, length + width * 0.45f);

            if (cachedTrailMat != null)
            {
                segObj.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            }

            BoxCollider col = segObj.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            TronTrailSegment seg = segObj.AddComponent<TronTrailSegment>();
            seg.Initialize(this, duration, trailHeight);
            activeTrailSegments.Add(seg);
        }

        public void ClearAllTrails()
        {
            for (int i = activeTrailSegments.Count - 1; i >= 0; i--)
            {
                if (activeTrailSegments[i] != null)
                {
                    Destroy(activeTrailSegments[i].gameObject);
                }
            }
            activeTrailSegments.Clear();
            trailInitialized = false;
        }

        public void TriggerEMPBlast(float radius = 34f)
        {
            // Destroy all enemy trail segments within radius
            TronTrailSegment[] allSegments = FindObjectsByType<TronTrailSegment>(FindObjectsSortMode.None);
            foreach (var seg in allSegments)
            {
                if (seg == null) continue;
                bool isOwnTrail = (seg.ownerActorNumber == actorNumber && seg.ownerIsBot == isBot);
                if (!isOwnTrail)
                {
                    float d = Vector3.Distance(transform.position, seg.transform.position);
                    if (d <= radius)
                    {
                        Destroy(seg.gameObject);
                    }
                }
            }

            // Spawn visual EMP Shockwave ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "EMPShockwave";
            ring.transform.position = new Vector3(transform.position.x, 0.25f, transform.position.z);
            ring.transform.localScale = new Vector3(1.5f, 0.1f, 1.5f);
            Destroy(ring.GetComponent<Collider>());

            if (cachedTrailMat != null)
            {
                ring.GetComponent<MeshRenderer>().sharedMaterial = cachedTrailMat;
            }

            TronEMPShockwave wave = ring.AddComponent<TronEMPShockwave>();
            wave.maxRadius = radius;
        }

        protected void CheckAuthoritativeCollisions()
        {
            if (currentHealth <= 0 || !gameObject.activeInHierarchy) return;

            Vector3 pos = transform.position;

            // 1. Check Arena Outer Boundary Walls
            if (Mathf.Abs(pos.x) >= arenaBoundLimit || Mathf.Abs(pos.z) >= arenaBoundLimit)
            {
                if (isShieldActive || spawnGraceTimer > 0f)
                {
                    // Auto-bounce / turn 90 degrees inside arena when Shielded
                    pos.x = Mathf.Clamp(pos.x, -arenaBoundLimit + 0.6f, arenaBoundLimit - 0.6f);
                    pos.z = Mathf.Clamp(pos.z, -arenaBoundLimit + 0.6f, arenaBoundLimit - 0.6f);
                    transform.position = pos;

                    Vector3 safeTurn = GetSafeBounceDirection(pos);
                    ExecuteTurn(safeTurn);
                    return;
                }
                else
                {
                    CrashIntoWallOrSelf("Dinding Arena");
                    return;
                }
            }

            if (spawnGraceTimer > 0f) return;

            // 2. Check Overlap at Front Nose of Light Cycle
            Vector3 nosePos = transform.position + moveDirection * 0.55f;
            Collider[] hits = Physics.OverlapSphere(nosePos, 0.42f);
            foreach (Collider hit in hits)
            {
                if (hit == null) continue;

                TronTrailSegment seg = hit.GetComponent<TronTrailSegment>();
                if (seg != null)
                {
                    OnHitTrailSegment(seg);
                    if (currentHealth <= 0) return;
                    continue;
                }

                TankBase otherCycle = hit.GetComponentInParent<TankBase>();
                if (otherCycle != null && otherCycle != this && otherCycle.currentHealth > 0 && otherCycle.gameObject.activeInHierarchy)
                {
                    OnHitOtherCycle(otherCycle);
                    if (currentHealth <= 0) return;
                }
            }
        }

        private Vector3 GetSafeBounceDirection(Vector3 pos)
        {
            if (Mathf.Abs(moveDirection.x) > 0.5f)
            {
                // Currently moving East/West -> turn North or South towards center
                return pos.z > 0f ? Vector3.back : Vector3.forward;
            }
            else
            {
                // Currently moving North/South -> turn East or West towards center
                return pos.x > 0f ? Vector3.left : Vector3.right;
            }
        }

        public virtual void OnHitTrailSegment(TronTrailSegment seg)
        {
            if (seg == null || currentHealth <= 0 || spawnGraceTimer > 0f) return;
            if (!IsAuthoritativeInstance()) return;

            bool isOwnSegment = (seg.ownerActorNumber == actorNumber && seg.ownerIsBot == isBot);

            // Ignore the freshly emitted trail right behind our own rear wheel
            if (isOwnSegment && (Time.time - seg.spawnTime) < 0.6f)
            {
                return;
            }

            if (isShieldActive)
            {
                if (!isOwnSegment)
                {
                    if (SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlayShieldBlock(transform.position);
                    }
                    Destroy(seg.gameObject);
                }
                return;
            }

            if (isOwnSegment)
            {
                CrashIntoWallOrSelf("Jejak Sendiri");
            }
            else
            {
                currentHealth = 0;
                Die(seg.ownerName, seg.ownerActorNumber, seg.ownerIsBot);
            }
        }

        public virtual void OnHitOtherCycle(TankBase otherCycle)
        {
            if (otherCycle == null || otherCycle == this || currentHealth <= 0 || spawnGraceTimer > 0f) return;
            if (!IsAuthoritativeInstance()) return;

            if (isShieldActive)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayShieldBlock(transform.position);
                }
                return;
            }

            currentHealth = 0;
            Die(otherCycle.tankName, otherCycle.actorNumber, otherCycle.isBot);
        }

        protected void CrashIntoWallOrSelf(string defaultCause)
        {
            if (currentHealth <= 0) return;
            currentHealth = 0;

            // Check if an opponent trapped/forced us within 8 units so they get credit for the trap kill!
            string killerName = defaultCause;
            int killerActor = -1;
            bool killerIsBot = false;
            float closestDist = 8.0f;

            TronTrailSegment[] allSegs = FindObjectsByType<TronTrailSegment>(FindObjectsSortMode.None);
            foreach (var seg in allSegs)
            {
                if (seg == null) continue;
                if (seg.ownerActorNumber == actorNumber && seg.ownerIsBot == isBot) continue;

                float d = Vector3.Distance(transform.position, seg.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    killerName = seg.ownerName;
                    killerActor = seg.ownerActorNumber;
                    killerIsBot = seg.ownerIsBot;
                }
            }

            Die(killerName, killerActor, killerIsBot);
        }

        protected virtual bool IsAuthoritativeInstance()
        {
            return true;
        }

        protected void UpdatePowerUpTimer()
        {
            if (currentPowerUp != PowerUpType.None)
            {
                powerUpTimer -= Time.deltaTime;
                if (powerUpTimer <= 0f)
                {
                    ResetPowerUps();
                }
            }
        }

        public virtual void AddAmmo(int amount)
        {
            ammo += amount;
        }

        public virtual void ApplyPowerUp(PowerUpType type, float duration)
        {
            if (type == PowerUpType.EMP)
            {
                TriggerEMPBlast(36f);
                currentPowerUp = PowerUpType.EMP;
                powerUpTimer = 2.0f;
                return;
            }

            ResetPowerUps();
            currentPowerUp = type;
            powerUpTimer = duration;

            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    currentMoveSpeed = baseMoveSpeed * 1.65f;
                    break;

                case PowerUpType.Shield:
                    isShieldActive = true;
                    if (shieldBubble != null) shieldBubble.SetActive(true);
                    break;

                case PowerUpType.TrailBoost:
                    // Trail segments spawned while active are thicker and last 2x longer
                    break;
            }
        }

        public virtual void ResetPowerUps()
        {
            currentPowerUp = PowerUpType.None;
            powerUpTimer = 0f;
            currentMoveSpeed = baseMoveSpeed;
            currentFireRate = baseFireRate;
            isShieldActive = false;

            if (shieldBubble != null) shieldBubble.SetActive(false);
            SetWeaponVisual(PowerUpType.None);
        }

        protected void SetWeaponVisual(PowerUpType type)
        {
            if (defaultBarrel != null) defaultBarrel.SetActive(false);
            if (tripleBarrels != null) tripleBarrels.SetActive(false);
            if (bigBarrel != null) bigBarrel.SetActive(false);
        }

        public virtual void TakeDamage(int damage, string attackerName, int attackerActorNumber, bool attackerIsBot)
        {
            if (isShieldActive || spawnGraceTimer > 0f)
            {
                return;
            }

            currentHealth -= damage;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die(attackerName, attackerActorNumber, attackerIsBot);
            }
        }

        public abstract void Die(string attackerName, int attackerActorNumber, bool attackerIsBot);
        public abstract void Respawn();
    }
}
