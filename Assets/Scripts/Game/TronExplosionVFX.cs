using System.Collections.Generic;
using UnityEngine;

namespace DiepMultiplayer
{
    public class TronExplosionVFX : MonoBehaviour
    {
        [Header("Tron Neon Appearance")]
        public Color neonColor = new Color(0.0f, 0.95f, 1.0f);
        [Range(0.2f, 5.0f)]
        public float explosionScale = 1.0f;
        public Vector3 crashDirection = Vector3.forward;

        [Header("Playback Behavior")]
        public bool playOnStart = true;
        public bool loopInShowcase = false;
        [Range(0.8f, 6.0f)]
        public float loopInterval = 2.4f;
        public bool autoDestroyWhenFinished = false;
        public bool playSound = true;

        [Header("Procedural Materials (Auto-created if empty)")]
        public Material sparkMaterial;
        public Material ringMaterial;
        public Material voxelMaterial;
        public Material beamMaterial;

        [Header("Hierarchy References")]
        public ParticleSystem coreFlashPS;
        public ParticleSystem derezVoxelsPS;
        public ParticleSystem neonSparksPS;
        public ParticleSystem electricArcsPS;
        public Transform floorCollisionPlane;
        public Transform shatteredCarPiecesRoot;
        public Transform shockwaveRingGround;
        public Transform shockwaveRingTilted;
        public Light explosionPointLight;

        private const float EffectLifetime = 1.55f;
        private const float DestroyDelay = 1.85f;
        private const int WheelChunkCount = 4;
        private const int BodyChunkCount = 10;
        private const int TotalChunkCount = WheelChunkCount + BodyChunkCount;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int BodyColorId = Shader.PropertyToID("_BodyColor");

        private static readonly Dictionary<PrimitiveType, Mesh> CachedPrimitiveMeshes = new Dictionary<PrimitiveType, Mesh>();

        private struct WreckChunkSpec
        {
            public string name;
            public PrimitiveType primitive;
            public Vector3 localOffset;
            public Vector3 baseScale;
            public float massFactor;
        }

        private static readonly WreckChunkSpec[] ChunkSpecs = new WreckChunkSpec[]
        {
            // 4 Detached Wheels
            new WreckChunkSpec { name = "WheelChunk_0", primitive = PrimitiveType.Cylinder, localOffset = new Vector3(-0.75f, -0.15f,  0.85f), baseScale = new Vector3(0.65f, 0.16f, 0.65f), massFactor = 1.15f },
            new WreckChunkSpec { name = "WheelChunk_1", primitive = PrimitiveType.Cylinder, localOffset = new Vector3( 0.75f, -0.15f,  0.85f), baseScale = new Vector3(0.65f, 0.16f, 0.65f), massFactor = 1.15f },
            new WreckChunkSpec { name = "WheelChunk_2", primitive = PrimitiveType.Cylinder, localOffset = new Vector3(-0.75f, -0.15f, -0.85f), baseScale = new Vector3(0.65f, 0.16f, 0.65f), massFactor = 1.15f },
            new WreckChunkSpec { name = "WheelChunk_3", primitive = PrimitiveType.Cylinder, localOffset = new Vector3( 0.75f, -0.15f, -0.85f), baseScale = new Vector3(0.65f, 0.16f, 0.65f), massFactor = 1.15f },

            // 10 Car Body & Chassis Chunks (Hood, Roof, Doors, Bumpers, Chassis)
            new WreckChunkSpec { name = "BodyChunk_0", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.00f,  0.10f,  0.65f), baseScale = new Vector3(0.85f, 0.12f, 0.95f), massFactor = 0.95f }, // Hood
            new WreckChunkSpec { name = "BodyChunk_1", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.00f,  0.38f, -0.05f), baseScale = new Vector3(0.75f, 0.14f, 0.85f), massFactor = 0.85f }, // Roof
            new WreckChunkSpec { name = "BodyChunk_2", primitive = PrimitiveType.Cube, localOffset = new Vector3(-0.62f,  0.12f,  0.00f), baseScale = new Vector3(0.12f, 0.38f, 0.95f), massFactor = 0.90f }, // Left door
            new WreckChunkSpec { name = "BodyChunk_3", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.62f,  0.12f,  0.00f), baseScale = new Vector3(0.12f, 0.38f, 0.95f), massFactor = 0.90f }, // Right door
            new WreckChunkSpec { name = "BodyChunk_4", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.00f,  0.05f,  1.10f), baseScale = new Vector3(0.90f, 0.22f, 0.25f), massFactor = 1.00f }, // Front bumper
            new WreckChunkSpec { name = "BodyChunk_5", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.00f,  0.08f, -1.10f), baseScale = new Vector3(0.90f, 0.22f, 0.25f), massFactor = 1.00f }, // Rear bumper
            new WreckChunkSpec { name = "BodyChunk_6", primitive = PrimitiveType.Cube, localOffset = new Vector3(-0.30f,  0.05f,  0.30f), baseScale = new Vector3(0.48f, 0.24f, 0.55f), massFactor = 1.10f }, // Chassis FL
            new WreckChunkSpec { name = "BodyChunk_7", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.30f,  0.05f,  0.30f), baseScale = new Vector3(0.48f, 0.24f, 0.55f), massFactor = 1.10f }, // Chassis FR
            new WreckChunkSpec { name = "BodyChunk_8", primitive = PrimitiveType.Cube, localOffset = new Vector3(-0.30f,  0.05f, -0.40f), baseScale = new Vector3(0.45f, 0.22f, 0.60f), massFactor = 1.05f }, // Chassis RL
            new WreckChunkSpec { name = "BodyChunk_9", primitive = PrimitiveType.Cube, localOffset = new Vector3( 0.30f,  0.05f, -0.40f), baseScale = new Vector3(0.45f, 0.22f, 0.60f), massFactor = 1.05f }  // Chassis RR
        };

        private Transform[] _chunkTransforms;
        private Renderer[] _chunkRenderers;
        private Vector3[] _chunkWorldPositions;
        private Vector3[] _chunkVelocities;
        private Vector3[] _chunkAngularVelocities;

        private MaterialPropertyBlock _mpb;
        private Renderer _groundRingRenderer;
        private Renderer _tiltedRingRenderer;

        private Vector3 _activeCrashDir = Vector3.forward;
        private float _elapsed;
        private float _loopTimer;
        private bool _isPlayingEffect;

        public static TronExplosionVFX Spawn(Vector3 position, Color color, float scale = 1.0f, bool playAudio = true)
        {
            return SpawnCrash(position, Vector3.forward, color, scale, playAudio);
        }

        public static TronExplosionVFX SpawnCrash(Vector3 position, Vector3 crashDir, Color color, float scale = 1.0f, bool playAudio = true)
        {
            GameObject prefab = Resources.Load<GameObject>("TronExplosionVFX");
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                instance = new GameObject("TronExplosionVFX");
                instance.transform.position = position;
            }

            TronExplosionVFX vfx = instance.GetComponent<TronExplosionVFX>();
            if (vfx == null)
            {
                vfx = instance.AddComponent<TronExplosionVFX>();
            }

            vfx.playOnStart = false;
            vfx.loopInShowcase = false;
            vfx.autoDestroyWhenFinished = true;
            vfx.TriggerCrashExplosion(crashDir, color, scale, playAudio);
            return vfx;
        }

        private void Awake()
        {
            EnsureHierarchyBuilt();
            CacheRenderersAndChunks();
        }

        private void Start()
        {
            if (playOnStart)
            {
                TriggerCrashExplosion(crashDirection, neonColor, explosionScale, playSound);
            }
            else
            {
                SetDynamicVisualsVisible(false);
                if (explosionPointLight != null)
                {
                    explosionPointLight.intensity = 0f;
                }
            }
        }

        private void Update()
        {
            if (loopInShowcase)
            {
                _loopTimer += Time.deltaTime;
                if (_loopTimer >= loopInterval)
                {
                    _loopTimer = 0f;
                    TriggerCrashExplosion(crashDirection, neonColor, explosionScale, playSound);
                }
            }

            if (!_isPlayingEffect)
            {
                return;
            }

            float dt = Time.deltaTime;
            _elapsed += dt;

            UpdateShatteredChunks(dt, _elapsed);
            UpdateProceduralMeshesAndLight(_elapsed);

            if (_elapsed >= EffectLifetime)
            {
                SetDynamicVisualsVisible(false);
                if (explosionPointLight != null)
                {
                    explosionPointLight.intensity = 0f;
                }

                if (autoDestroyWhenFinished && _elapsed >= DestroyDelay)
                {
                    _isPlayingEffect = false;
                    Destroy(gameObject);
                }
                else if (!autoDestroyWhenFinished)
                {
                    _isPlayingEffect = false;
                }
            }
        }

        public void TriggerExplosion()
        {
            TriggerCrashExplosion(crashDirection, neonColor, explosionScale, playSound);
        }

        public void TriggerExplosion(Color color, float scale = 1.0f, bool triggerAudio = true)
        {
            TriggerCrashExplosion(crashDirection, color, scale, triggerAudio);
        }

        public void TriggerCrashExplosion(Vector3 crashDir, Color color, float scale = 1.0f, bool triggerAudio = true)
        {
            neonColor = color;
            explosionScale = Mathf.Max(0.1f, scale);
            crashDir.y = 0f;
            _activeCrashDir = crashDir.sqrMagnitude > 0.001f ? crashDir.normalized : Vector3.forward;
            crashDirection = _activeCrashDir;

            transform.localScale = Vector3.one * explosionScale;

            EnsureHierarchyBuilt();
            CacheRenderersAndChunks();
            SyncFloorCollisionPlane();
            OrientCrashEmissionTransforms(_activeCrashDir);
            ApplyParticleColors(neonColor);

            _elapsed = 0f;
            _loopTimer = 0f;
            _isPlayingEffect = true;

            InitializeWreckChunkPhysics(_activeCrashDir);

            RestartParticleSystem(coreFlashPS);
            RestartParticleSystem(derezVoxelsPS);
            RestartParticleSystem(neonSparksPS);
            RestartParticleSystem(electricArcsPS);

            SetDynamicVisualsVisible(true);
            UpdateShatteredChunks(0f, 0f);
            UpdateProceduralMeshesAndLight(0f);

            if (triggerAudio && Application.isPlaying)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayExplosion(transform.position);
                }
            }
        }

        public void EnsureHierarchyBuilt()
        {
            EnsureMaterials();

            // Clean up legacy vertical sky beam objects if upgrading an older prefab/scene
            RemoveLegacyChild("DerezVerticalBeam");
            RemoveLegacyChild("RisingDataBitsPS");

            if (floorCollisionPlane == null)
                floorCollisionPlane = FindOrCreateFloorCollisionPlane("FloorCollisionPlane");
            SyncFloorCollisionPlane();

            if (coreFlashPS == null)
                coreFlashPS = FindOrCreateChildParticleSystem("CoreFlashPS");
            ConfigureCoreFlashPS(coreFlashPS);

            if (derezVoxelsPS == null)
                derezVoxelsPS = FindOrCreateChildParticleSystem("DerezVoxelsPS");
            ConfigureDerezVoxelsPS(derezVoxelsPS);

            if (neonSparksPS == null)
                neonSparksPS = FindOrCreateChildParticleSystem("NeonSparksPS");
            ConfigureNeonSparksPS(neonSparksPS);

            if (electricArcsPS == null)
                electricArcsPS = FindOrCreateChildParticleSystem("ElectricArcsPS");
            ConfigureElectricArcsPS(electricArcsPS);

            if (shatteredCarPiecesRoot == null)
                shatteredCarPiecesRoot = FindOrCreateShatteredCarPiecesRoot("ShatteredCarPiecesRoot");
            EnsureShatteredCarPieceChildren();

            if (shockwaveRingGround == null)
                shockwaveRingGround = FindOrCreateQuadMesh("ShockwaveRingGround", new Vector3(0f, 0.05f, 0f), Quaternion.Euler(90f, 0f, 0f), ringMaterial);

            if (shockwaveRingTilted == null)
                shockwaveRingTilted = FindOrCreateQuadMesh("ShockwaveRingTilted", new Vector3(0f, 0.6f, 0.8f), Quaternion.identity, ringMaterial);

            if (explosionPointLight == null)
                explosionPointLight = FindOrCreatePointLight("ExplosionPointLight");

            ApplyParticleColors(neonColor);

            if (!Application.isPlaying)
            {
                SetDynamicVisualsVisible(false);
                if (explosionPointLight != null)
                {
                    explosionPointLight.intensity = 0f;
                }
            }
        }

        private void RemoveLegacyChild(string childName)
        {
            Transform t = transform.Find(childName);
            if (t != null)
            {
                if (Application.isPlaying)
                    Destroy(t.gameObject);
                else
                    DestroyImmediate(t.gameObject);
            }
        }

        private void EnsureMaterials()
        {
            Shader shader = Shader.Find("Custom/URP_TronExplosionVFX");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (sparkMaterial == null)
                sparkMaterial = CreateRuntimeMaterial(shader, 0f, 4.0f, 0.14f);

            if (ringMaterial == null)
                ringMaterial = CreateRuntimeMaterial(shader, 1f, 4.5f, 0.14f);

            if (voxelMaterial == null)
                voxelMaterial = CreateRuntimeMaterial(shader, 2f, 3.8f, 0.14f);

            if (beamMaterial == null)
                beamMaterial = CreateRuntimeMaterial(shader, 3f, 4.2f, 0.14f);
        }

        private static Material CreateRuntimeMaterial(Shader shader, float mode, float emissionStrength, float edgeWidth)
        {
            Material mat = new Material(shader);
            if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, Color.white);
            if (mat.HasProperty(BodyColorId)) mat.SetColor(BodyColorId, new Color(0.04f, 0.05f, 0.09f, 1f));
            if (mat.HasProperty(ModeId)) mat.SetFloat(ModeId, mode);
            if (mat.HasProperty(EmissionStrengthId)) mat.SetFloat(EmissionStrengthId, emissionStrength);
            if (mat.HasProperty(EdgeWidthId)) mat.SetFloat(EdgeWidthId, edgeWidth);
            return mat;
        }

        private Transform FindOrCreateFloorCollisionPlane(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            return child;
        }

        private void SyncFloorCollisionPlane()
        {
            if (floorCollisionPlane == null) return;
            floorCollisionPlane.position = new Vector3(transform.position.x, 0.02f, transform.position.z);
            floorCollisionPlane.rotation = Quaternion.identity;
        }

        private void OrientCrashEmissionTransforms(Vector3 crashDir)
        {
            Vector3 shardForward = (Vector3.up * 1.15f + crashDir * 0.65f).normalized;
            if (derezVoxelsPS != null)
            {
                derezVoxelsPS.transform.rotation = Quaternion.LookRotation(shardForward);
            }
            if (neonSparksPS != null)
            {
                Vector3 sparkDir = (Vector3.up * 0.85f + crashDir * 0.75f).normalized;
                neonSparksPS.transform.rotation = Quaternion.LookRotation(sparkDir);
            }
            if (shockwaveRingTilted != null)
            {
                shockwaveRingTilted.position = transform.position + crashDir * (0.75f * explosionScale) + Vector3.up * (0.35f * explosionScale);
                shockwaveRingTilted.rotation = Quaternion.LookRotation(crashDir);
            }
            if (shockwaveRingGround != null)
            {
                shockwaveRingGround.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                shockwaveRingGround.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private ParticleSystem FindOrCreateChildParticleSystem(string childName)
        {
            Transform child = transform.Find(childName);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }

            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = go.AddComponent<ParticleSystem>();
            }
            return ps;
        }

        private void ConfigureCoreFlashPS(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(3.2f, 5.0f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 10;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 4) });

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.65f),
                new Keyframe(0.15f, 1.15f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sharedMaterial = sparkMaterial;
        }

        private void ConfigureDerezVoxelsPS(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.localRotation = Quaternion.LookRotation((Vector3.up * 1.15f + Vector3.forward * 0.65f).normalized);

            var main = ps.main;
            main.duration = 1.6f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.38f);
            main.gravityModifier = 2.6f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 160;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 130) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58f;
            shape.radius = 0.45f;

            var rotOverLifetime = ps.rotationOverLifetime;
            rotOverLifetime.enabled = true;
            rotOverLifetime.separateAxes = true;
            float maxRad = 540f * Mathf.Deg2Rad;
            rotOverLifetime.x = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);
            rotOverLifetime.y = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);
            rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-maxRad, maxRad);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.75f, 0.85f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            if (floorCollisionPlane != null) col.SetPlane(0, floorCollisionPlane);
            col.bounce = new ParticleSystem.MinMaxCurve(0.45f);
            col.dampen = new ParticleSystem.MinMaxCurve(0.28f);
            col.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.04f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            psRenderer.mesh = GetPrimitiveMesh(PrimitiveType.Cube);
            psRenderer.sharedMaterial = voxelMaterial;
        }

        private void ConfigureNeonSparksPS(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.localRotation = Quaternion.LookRotation((Vector3.up * 0.85f + Vector3.forward * 0.75f).normalized);

            var main = ps.main;
            main.duration = 1.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 32f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
            main.gravityModifier = 2.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 65f;
            shape.radius = 0.35f;

            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = true;
            limitVel.separateAxes = false;
            limitVel.limit = 4.0f;
            limitVel.dampen = 0.14f;

            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            if (floorCollisionPlane != null) col.SetPlane(0, floorCollisionPlane);
            col.bounce = new ParticleSystem.MinMaxCurve(0.55f);
            col.dampen = new ParticleSystem.MinMaxCurve(0.20f);
            col.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.02f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            psRenderer.lengthScale = 3.8f;
            psRenderer.velocityScale = 0.08f;
            psRenderer.sharedMaterial = sparkMaterial;
        }

        private void ConfigureElectricArcsPS(ParticleSystem ps)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.70f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 48;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.45f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 6f;
            noise.frequency = 3.5f;
            noise.scrollSpeed = 1.5f;
            noise.octaveCount = 2;

            var trails = ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1.0f;
            trails.lifetime = 0.22f;
            trails.minVertexDistance = 0.12f;
            trails.worldSpace = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.14f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            ));

            var col = ps.collision;
            col.enabled = true;
            col.type = ParticleSystemCollisionType.Planes;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            if (floorCollisionPlane != null) col.SetPlane(0, floorCollisionPlane);
            col.bounce = new ParticleSystem.MinMaxCurve(0.45f);
            col.dampen = new ParticleSystem.MinMaxCurve(0.25f);
            col.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.02f);

            var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sharedMaterial = sparkMaterial;
            psRenderer.trailMaterial = sparkMaterial;
        }

        private Transform FindOrCreateShatteredCarPiecesRoot(string rootName)
        {
            Transform root = transform.Find(rootName);
            if (root == null)
            {
                GameObject go = new GameObject(rootName);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                root = go.transform;
            }
            return root;
        }

        private void EnsureShatteredCarPieceChildren()
        {
            if (shatteredCarPiecesRoot == null) return;

            for (int i = 0; i < ChunkSpecs.Length; i++)
            {
                WreckChunkSpec spec = ChunkSpecs[i];
                Transform child = shatteredCarPiecesRoot.Find(spec.name);
                GameObject chunkGo;
                if (child == null)
                {
                    chunkGo = new GameObject(spec.name);
                    chunkGo.transform.SetParent(shatteredCarPiecesRoot, false);
                }
                else
                {
                    chunkGo = child.gameObject;
                }

                chunkGo.transform.localPosition = spec.localOffset;
                chunkGo.transform.localRotation = spec.primitive == PrimitiveType.Cylinder
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;
                chunkGo.transform.localScale = spec.baseScale;

                MeshFilter mf = chunkGo.GetComponent<MeshFilter>();
                if (mf == null) mf = chunkGo.AddComponent<MeshFilter>();
                mf.sharedMesh = GetPrimitiveMesh(spec.primitive);

                MeshRenderer mr = chunkGo.GetComponent<MeshRenderer>();
                if (mr == null) mr = chunkGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = voxelMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        private Transform FindOrCreateQuadMesh(string childName, Vector3 localPos, Quaternion localRot, Material mat)
        {
            Transform child = transform.Find(childName);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(transform, false);
            }

            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetPrimitiveMesh(PrimitiveType.Quad);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            return go.transform;
        }

        private Light FindOrCreatePointLight(string childName)
        {
            Transform child = transform.Find(childName);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                go = new GameObject(childName);
                go.transform.SetParent(transform, false);
            }

            go.transform.localPosition = new Vector3(0f, 1.1f, 0f);

            Light l = go.GetComponent<Light>();
            if (l == null) l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = neonColor;
            l.intensity = 18f;
            l.range = 16f;
            l.shadows = LightShadows.None;
            return l;
        }

        public void ApplyParticleColors(Color color)
        {
            if (coreFlashPS != null)
            {
                var main = coreFlashPS.main;
                main.startColor = Color.white;

                var colOverLifetime = coreFlashPS.colorOverLifetime;
                colOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(Color.white, 0.0f),
                        new GradientColorKey(color, 0.35f),
                        new GradientColorKey(color, 1.0f)
                    },
                    new[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.9f, 0.35f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
            }

            if (derezVoxelsPS != null)
            {
                Color hotTint = Color.Lerp(color, Color.white, 0.25f);
                var main = derezVoxelsPS.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color, hotTint);

                var colOverLifetime = derezVoxelsPS.colorOverLifetime;
                colOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(Color.white, 0.0f),
                        new GradientColorKey(Color.white, 1.0f)
                    },
                    new[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(1.0f, 0.65f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
            }

            if (neonSparksPS != null)
            {
                var main = neonSparksPS.main;
                main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);

                var colOverLifetime = neonSparksPS.colorOverLifetime;
                colOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(Color.white, 0.0f),
                        new GradientColorKey(color, 0.25f),
                        new GradientColorKey(color, 1.0f)
                    },
                    new[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.85f, 0.55f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);
            }

            if (electricArcsPS != null)
            {
                var main = electricArcsPS.main;
                main.startColor = Color.Lerp(color, Color.white, 0.4f);

                var colOverLifetime = electricArcsPS.colorOverLifetime;
                colOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new[] {
                        new GradientColorKey(Color.white, 0.0f),
                        new GradientColorKey(color, 0.3f),
                        new GradientColorKey(color, 1.0f)
                    },
                    new[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                colOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

                var trails = electricArcsPS.trails;
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(grad);
            }

            if (explosionPointLight != null)
            {
                explosionPointLight.color = color;
            }
        }

        private void CacheRenderersAndChunks()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            if (shockwaveRingGround != null && _groundRingRenderer == null)
                _groundRingRenderer = shockwaveRingGround.GetComponent<Renderer>();

            if (shockwaveRingTilted != null && _tiltedRingRenderer == null)
                _tiltedRingRenderer = shockwaveRingTilted.GetComponent<Renderer>();

            if (shatteredCarPiecesRoot != null && (_chunkTransforms == null || _chunkTransforms.Length != TotalChunkCount))
            {
                _chunkTransforms = new Transform[TotalChunkCount];
                _chunkRenderers = new Renderer[TotalChunkCount];
                _chunkWorldPositions = new Vector3[TotalChunkCount];
                _chunkVelocities = new Vector3[TotalChunkCount];
                _chunkAngularVelocities = new Vector3[TotalChunkCount];

                for (int i = 0; i < TotalChunkCount; i++)
                {
                    Transform t = shatteredCarPiecesRoot.Find(ChunkSpecs[i].name);
                    _chunkTransforms[i] = t;
                    _chunkRenderers[i] = t != null ? t.GetComponent<Renderer>() : null;
                }
            }
        }

        private void InitializeWreckChunkPhysics(Vector3 crashDir)
        {
            if (_chunkTransforms == null) return;

            Vector3 fwd = crashDir.sqrMagnitude > 0.001f ? crashDir.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            float speedScale = Mathf.Lerp(1f, explosionScale, 0.55f);

            for (int i = 0; i < TotalChunkCount; i++)
            {
                Transform t = _chunkTransforms[i];
                if (t == null) continue;

                WreckChunkSpec spec = ChunkSpecs[i];
                Vector3 localOff = spec.localOffset;
                Vector3 worldSpawn = transform.position + (right * localOff.x + Vector3.up * localOff.y + fwd * localOff.z) * explosionScale;
                worldSpawn.y = Mathf.Max(worldSpawn.y, 0.2f * explosionScale);

                _chunkWorldPositions[i] = worldSpawn;
                t.position = worldSpawn;
                t.localScale = spec.baseScale;
                t.rotation = spec.primitive == PrimitiveType.Cylinder
                    ? Quaternion.LookRotation(fwd) * Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.LookRotation(fwd) * Quaternion.Euler(Random.Range(-18f, 18f), Random.Range(-25f, 25f), Random.Range(-18f, 18f));

                float forwardImpulse = Random.Range(4f, 14f) / spec.massFactor;
                float lateralScatter = Random.Range(-9f, 9f) + Mathf.Sign(localOff.x == 0f ? Random.Range(-1f, 1f) : localOff.x) * Random.Range(2f, 6f);
                float upwardPop = Random.Range(4.5f, 12.5f) / spec.massFactor;

                _chunkVelocities[i] = (fwd * forwardImpulse + right * lateralScatter + Vector3.up * upwardPop) * speedScale;
                _chunkAngularVelocities[i] = new Vector3(
                    Random.Range(-720f, 720f),
                    Random.Range(-720f, 720f),
                    Random.Range(-720f, 720f)
                );
            }
        }

        private void UpdateShatteredChunks(float dt, float time)
        {
            if (_chunkTransforms == null) return;

            float floorY = 0.12f * explosionScale;
            float shrinkFactor = 1f - Mathf.InverseLerp(0.75f, 1.45f, time);
            bool active = time <= 1.48f;

            for (int i = 0; i < TotalChunkCount; i++)
            {
                Transform t = _chunkTransforms[i];
                if (t == null) continue;

                t.gameObject.SetActive(active);
                if (!active) continue;

                if (dt > 0f)
                {
                    Vector3 vel = _chunkVelocities[i];
                    Vector3 pos = _chunkWorldPositions[i];

                    vel.y -= 25f * dt;
                    pos += vel * dt;

                    if (pos.y <= floorY)
                    {
                        pos.y = floorY;
                        if (vel.y < 0f)
                        {
                            vel.y = -vel.y * 0.45f;
                        }
                        vel.x *= 0.82f;
                        vel.z *= 0.82f;
                        _chunkAngularVelocities[i] *= 0.85f;
                    }

                    _chunkWorldPositions[i] = pos;
                    _chunkVelocities[i] = vel;

                    t.position = pos;
                    t.Rotate(_chunkAngularVelocities[i] * dt, Space.Self);
                }

                float flicker = (time > 0.95f && ((int)(time * 42f) + i) % 3 == 0) ? 0.35f : 1f;
                t.localScale = ChunkSpecs[i].baseScale * shrinkFactor;

                Color chunkCol = new Color(neonColor.r, neonColor.g, neonColor.b, shrinkFactor * flicker);
                SetRendererColor(_chunkRenderers[i], chunkCol);
            }
        }

        private void RestartParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            ps.Play(true);
        }

        private void SetDynamicVisualsVisible(bool visible)
        {
            if (shatteredCarPiecesRoot != null) shatteredCarPiecesRoot.gameObject.SetActive(visible);
            if (shockwaveRingGround != null) shockwaveRingGround.gameObject.SetActive(visible);
            if (shockwaveRingTilted != null) shockwaveRingTilted.gameObject.SetActive(visible);
        }

        private void UpdateProceduralMeshesAndLight(float time)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            // 1. Ground Blast Shockwave (0 -> 0.48s, 0.5m -> 12.5m at y = 0.05)
            if (shockwaveRingGround != null)
            {
                const float dur = 0.48f;
                float t = Mathf.Clamp01(time / dur);
                bool active = time <= dur;
                shockwaveRingGround.gameObject.SetActive(active);
                if (active)
                {
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    float size = Mathf.Lerp(0.5f, 12.5f, ease);
                    shockwaveRingGround.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                    shockwaveRingGround.rotation = Quaternion.Euler(90f, 0f, 0f);
                    shockwaveRingGround.localScale = new Vector3(size, size, 1f);

                    float alpha = 1f - t * t;
                    SetRendererColor(_groundRingRenderer, new Color(neonColor.r, neonColor.g, neonColor.b, alpha));
                }
            }

            // 2. Vertical Impact Crunch Ring facing crashDirection (0 -> 0.32s, 0.4m -> 7.5m)
            if (shockwaveRingTilted != null)
            {
                const float dur = 0.32f;
                float t = Mathf.Clamp01(time / dur);
                bool active = time <= dur;
                shockwaveRingTilted.gameObject.SetActive(active);
                if (active)
                {
                    float ease = 1f - Mathf.Pow(1f - t, 3f);
                    float size = Mathf.Lerp(0.4f, 7.5f, ease);
                    shockwaveRingTilted.position = transform.position + _activeCrashDir * (0.75f * explosionScale) + Vector3.up * (0.35f * explosionScale);
                    shockwaveRingTilted.rotation = Quaternion.LookRotation(_activeCrashDir);
                    shockwaveRingTilted.localScale = new Vector3(size, size, 1f);

                    float alpha = (1f - t * t) * 0.95f;
                    SetRendererColor(_tiltedRingRenderer, new Color(neonColor.r, neonColor.g, neonColor.b, alpha));
                }
            }

            // 3. Dynamic Point Light (0 -> 0.42s, 18 -> 0)
            if (explosionPointLight != null)
            {
                const float dur = 0.42f;
                float t = Mathf.Clamp01(time / dur);
                explosionPointLight.color = neonColor;
                explosionPointLight.range = 16f * explosionScale;
                explosionPointLight.intensity = 18f * Mathf.Pow(1f - t, 2f);
            }
        }

        private void SetRendererColor(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (CachedPrimitiveMeshes.TryGetValue(type, out Mesh existing) && existing != null)
            {
                return existing;
            }

            GameObject temp = GameObject.CreatePrimitive(type);
            MeshFilter mf = temp.GetComponent<MeshFilter>();
            Mesh mesh = mf != null ? mf.sharedMesh : null;
            if (Application.isPlaying)
            {
                Destroy(temp);
            }
            else
            {
                DestroyImmediate(temp);
            }

            if (mesh != null)
            {
                CachedPrimitiveMeshes[type] = mesh;
            }
            return mesh;
        }
    }
}
