using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DiepMultiplayer.Editor
{
    public static class TronVFXSceneSetup
    {
        private const string ShaderAssetPath = "Assets/Shaders/TronExplosionVFX.shader";
        private const string ShaderName = "Custom/URP_TronExplosionVFX";
        private const string SkyboxShaderPath = "Assets/Shaders/TronGridSkybox.shader";
        private const string SkyboxShaderName = "Custom/URP_TronGridSkybox";
        private const string SkyboxMaterialPath = "Assets/Materials/TronGridSkybox.mat";
        private const string PrefabPath = "Assets/Resources/TronExplosionVFX.prefab";
        private const string VfxScenePath = "Assets/Scenes/VFX.unity";
        private const string VolumeProfilePath = "Assets/Settings/TronVFXVolumeProfile.asset";
        private const string CarRedPath = "Assets/3D/Car Red.fbx";
        private const string CarBluePath = "Assets/3D/Car Blue.fbx";

        private static bool _autoSetupQueued = false;

        [InitializeOnLoadMethod]
        private static void OnEditorLoadCheck()
        {
            if (_autoSetupQueued) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;

            _autoSetupQueued = true;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;

                Scene activeScene = SceneManager.GetActiveScene();
                bool missingPrefab = !File.Exists(PrefabPath);
                bool emptyVfxScene = activeScene.name == "VFX" && activeScene.rootCount < 4;

                if (missingPrefab || emptyVfxScene)
                {
                    SetupVFXSceneAndPrefab(false);
                }
            };
        }

        [MenuItem("Tron Game/2. Setup VFX Explosion Scene & Prefab", false, 2)]
        public static void SetupVFXMenu()
        {
            SetupVFXSceneAndPrefab(true);
        }

        public static void SetupVFXSceneAndPrefab(bool showDialog = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureDirectories();

            Shader vfxShader = Shader.Find(ShaderName);
            if (vfxShader == null && File.Exists(ShaderAssetPath))
            {
                AssetDatabase.ImportAsset(ShaderAssetPath, ImportAssetOptions.ForceUpdate);
                vfxShader = Shader.Find(ShaderName);
            }

            if (vfxShader == null)
            {
                Debug.LogError($"[TronVFXSceneSetup] Shader '{ShaderName}' not found at '{ShaderAssetPath}'.");
                return;
            }

            Material sparkMat = CreateOrUpdateVfxMaterial("Assets/Materials/VFX_TronSpark.mat", vfxShader, 0f, 4.0f, 0.14f);
            Material ringMat  = CreateOrUpdateVfxMaterial("Assets/Materials/VFX_TronRing.mat",  vfxShader, 1f, 4.5f, 0.14f);
            Material voxelMat = CreateOrUpdateVfxMaterial("Assets/Materials/VFX_TronVoxel.mat", vfxShader, 2f, 3.8f, 0.14f);
            Material beamMat  = CreateOrUpdateVfxMaterial("Assets/Materials/VFX_TronBeam.mat",  vfxShader, 3f, 4.2f, 0.14f);

            VolumeProfile volumeProfile = CreateOrUpdateVolumeProfile(VolumeProfilePath);
            Material skyboxMat = CreateOrUpdateSkyboxMaterial();
            GameObject prefabAsset = CreateOrUpdateExplosionPrefab(sparkMat, ringMat, voxelMat, beamMat);

            BuildVfxScene(prefabAsset, volumeProfile, skyboxMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TronVFXSceneSetup] Crash-Shatter VFX scene 'VFX.unity' and 'Assets/Resources/TronExplosionVFX.prefab' built successfully!");

            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Tron Crash VFX Siap!",
                    "Efek ledakan tabrakan & pecahan mobil Tron berhasil dibuat di 'VFX.unity'!\nTekan Play untuk melihat mobil menabrak dinding & hancur berkeping-keping (SPACE, KLIK KIRI, 1-6, L).",
                    "OK"
                );
            }
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets", "Shaders");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string full = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Material CreateOrUpdateVfxMaterial(string assetPath, Shader shader, float mode, float emissionStrength, float edgeWidth)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_BodyColor", new Color(0.04f, 0.05f, 0.09f, 1f));
            mat.SetFloat("_Mode", mode);
            mat.SetFloat("_EmissionStrength", emissionStrength);
            mat.SetFloat("_EdgeWidth", edgeWidth);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static VolumeProfile CreateOrUpdateVolumeProfile(string profilePath)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);
            bloom.name = "Bloom";
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.8f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.4f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.68f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;
            if (!AssetDatabase.Contains(bloom)) AssetDatabase.AddObjectToAsset(bloom, profile);

            if (!profile.TryGet(out Tonemapping tonemapping))
                tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.name = "Tonemapping";
            tonemapping.active = true;
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;
            if (!AssetDatabase.Contains(tonemapping)) AssetDatabase.AddObjectToAsset(tonemapping, profile);

            if (!profile.TryGet(out Vignette vignette))
                vignette = profile.Add<Vignette>(true);
            vignette.name = "Vignette";
            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.28f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;
            if (!AssetDatabase.Contains(vignette)) AssetDatabase.AddObjectToAsset(vignette, profile);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Material CreateOrUpdateSkyboxMaterial()
        {
            Shader skyShader = Shader.Find(SkyboxShaderName);
            if (skyShader == null && File.Exists(SkyboxShaderPath))
            {
                AssetDatabase.ImportAsset(SkyboxShaderPath, ImportAssetOptions.ForceUpdate);
                skyShader = Shader.Find(SkyboxShaderName);
            }

            if (skyShader == null)
            {
                Debug.LogWarning($"[TronVFXSceneSetup] Skybox shader '{SkyboxShaderName}' not found at '{SkyboxShaderPath}'.");
                return null;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            if (mat == null)
            {
                mat = new Material(skyShader);
                AssetDatabase.CreateAsset(mat, SkyboxMaterialPath);
            }
            else if (mat.shader != skyShader)
            {
                mat.shader = skyShader;
            }

            mat.SetColor("_ZenithColor", new Color(0.006f, 0.012f, 0.032f, 1f));
            mat.SetColor("_AbyssColor", new Color(0.004f, 0.008f, 0.022f, 1f));
            mat.SetColor("_HorizonColor", new Color(0.0f, 0.65f, 1.35f, 1f));
            mat.SetColor("_GridColor", new Color(0.0f, 0.75f, 1.4f, 1f));
            mat.SetColor("_AccentColor", new Color(1.6f, 0.45f, 0.05f, 1f));
            mat.SetFloat("_GridScale", 3.5f);
            mat.SetFloat("_CityHeight", 0.16f);
            mat.SetFloat("_BeamIntensity", 1.2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateOrUpdateExplosionPrefab(Material sparkMat, Material ringMat, Material voxelMat, Material beamMat)
        {
            GameObject root = new GameObject("TronExplosionVFX");
            TronExplosionVFX vfx = root.AddComponent<TronExplosionVFX>();
            vfx.neonColor = new Color(0.0f, 0.95f, 1.0f);
            vfx.explosionScale = 1.0f;
            vfx.crashDirection = Vector3.forward;
            vfx.playOnStart = true;
            vfx.loopInShowcase = false;
            vfx.autoDestroyWhenFinished = false;
            vfx.playSound = true;

            vfx.sparkMaterial = sparkMat;
            vfx.ringMaterial = ringMat;
            vfx.voxelMaterial = voxelMat;
            vfx.beamMaterial = beamMat;

            vfx.EnsureHierarchyBuilt();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildVfxScene(GameObject explosionPrefab, VolumeProfile volumeProfile, Material skyboxMat)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path != VfxScenePath && active.isDirty && !string.IsNullOrEmpty(active.path))
            {
                EditorSceneManager.SaveScene(active);
            }

            Scene vfxScene;
            if (File.Exists(VfxScenePath))
            {
                vfxScene = EditorSceneManager.OpenScene(VfxScenePath, OpenSceneMode.Single);
            }
            else
            {
                vfxScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            if (skyboxMat != null)
            {
                RenderSettings.skybox = skyboxMat;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.10f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.012f, 0.016f, 0.035f, 1f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.011f;

            // Clean up any prior generated objects for idempotency
            DestroyIfExists("Global Volume");
            DestroyIfExists("TronGridStage");
            DestroyIfExists("CrashBarrierWall");
            DestroyIfExists("TronVFXShowcaseManager");
            DestroyIfExists("TronExplosion_Cyan");
            DestroyIfExists("TronExplosion_Orange");
            DestroyIfExists("TronExplosion_Magenta");
            DestroyIfExists("CrashTestCar_Center");
            DestroyIfExists("CrashTestCar_Left");
            DestroyIfExists("CrashTestCar_Right");

            Camera mainCam = SetupMainCamera();
            SetupDirectionalLight();

            GameObject volGo = new GameObject("Global Volume");
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = volumeProfile;

            BuildTronGridStage();
            BuildCrashBarrierWall();

            // Color definitions
            Color colorOrangeRed = new Color(1.0f, 0.32f, 0.0f);
            Color colorCyan = new Color(0.0f, 0.95f, 1.0f);
            Color colorMagenta = new Color(1.0f, 0.08f, 0.68f);

            // Spawn 3 explosion anchors at the wall crash line (z = 2.1f)
            TronExplosionVFX centerVfx = SpawnShowcaseExplosion(explosionPrefab, "TronExplosion_Orange", new Vector3(0.0f, 0.45f, 2.1f), colorOrangeRed, 1.15f);
            TronExplosionVFX leftVfx   = SpawnShowcaseExplosion(explosionPrefab, "TronExplosion_Cyan",   new Vector3(-4.5f, 0.45f, 2.1f), colorCyan, 0.95f);
            TronExplosionVFX rightVfx  = SpawnShowcaseExplosion(explosionPrefab, "TronExplosion_Magenta", new Vector3(4.5f, 0.45f, 2.1f), colorMagenta, 0.95f);

            // Instantiate actual 3D cars facing +Z towards the crash barrier wall
            GameObject centerCar = SpawnCrashTestCar(CarRedPath,  "CrashTestCar_Center", new Vector3(0.0f, 0f, -7.2f));
            GameObject leftCar   = SpawnCrashTestCar(CarBluePath, "CrashTestCar_Left",   new Vector3(-4.5f, 0f, -7.2f));
            GameObject rightCar  = SpawnCrashTestCar(CarRedPath,  "CrashTestCar_Right",  new Vector3(4.5f, 0f, -7.2f));

            // Wire Interactive Showcase Manager
            GameObject managerGo = new GameObject("TronVFXShowcaseManager");
            TronVFXShowcaseController controller = managerGo.AddComponent<TronVFXShowcaseController>();
            controller.targetCamera = mainCam;
            controller.autoLoop = true;
            controller.stageExplosions = new[] { centerVfx, leftVfx, rightVfx };
            controller.crashLanes = new[]
            {
                new TronVFXShowcaseController.CrashTestLane
                {
                    laneName = "Center Lane (Car Red)",
                    carVisual = centerCar,
                    crashVfx = centerVfx,
                    neonColor = colorOrangeRed,
                    explosionScale = 1.15f,
                    startPos = new Vector3(0.0f, 0f, -7.2f),
                    crashPos = new Vector3(0.0f, 0f, 2.1f),
                    driveSpeed = 15f,
                    respawnDelay = 1.6f,
                    initialDelay = 0.0f
                },
                new TronVFXShowcaseController.CrashTestLane
                {
                    laneName = "Left Lane (Car Blue)",
                    carVisual = leftCar,
                    crashVfx = leftVfx,
                    neonColor = colorCyan,
                    explosionScale = 0.95f,
                    startPos = new Vector3(-4.5f, 0f, -7.2f),
                    crashPos = new Vector3(-4.5f, 0f, 2.1f),
                    driveSpeed = 15f,
                    respawnDelay = 1.6f,
                    initialDelay = 0.7f
                },
                new TronVFXShowcaseController.CrashTestLane
                {
                    laneName = "Right Lane (Car Red Magenta)",
                    carVisual = rightCar,
                    crashVfx = rightVfx,
                    neonColor = colorMagenta,
                    explosionScale = 0.95f,
                    startPos = new Vector3(4.5f, 0f, -7.2f),
                    crashPos = new Vector3(4.5f, 0f, 2.1f),
                    driveSpeed = 15f,
                    respawnDelay = 1.6f,
                    initialDelay = 1.4f
                }
            };

            EditorSceneManager.MarkSceneDirty(vfxScene);
            EditorSceneManager.SaveScene(vfxScene, VfxScenePath);
        }

        private static Camera SetupMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.transform.position = new Vector3(0f, 5.4f, -13.8f);
            cam.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.012f, 0.016f, 0.035f, 1f);
            cam.allowHDR = true;
            cam.fieldOfView = 60f;

            UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            return cam;
        }

        private static void SetupDirectionalLight()
        {
            Light dirLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    dirLight = lights[i];
                    break;
                }
            }

            if (dirLight == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.intensity = 0.45f;
            dirLight.color = new Color(0.65f, 0.80f, 1.0f);
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void BuildTronGridStage()
        {
            GameObject stage = new GameObject("TronGridStage");

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material floorMat = CreateOrUpdateLitMaterial(
                "Assets/Materials/VFX_TronFloor.mat",
                litShader,
                new Color(0.015f, 0.02f, 0.04f, 1f),
                0.85f,
                0.88f,
                Color.black
            );

            Material gridLineMat = CreateOrUpdateLitMaterial(
                "Assets/Materials/VFX_TronGridLine.mat",
                litShader,
                new Color(0.0f, 0.45f, 0.75f, 1f),
                0.2f,
                0.5f,
                new Color(0.0f, 0.55f, 1.05f)
            );

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "TronFloor";
            floor.transform.SetParent(stage.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(40f, 0.2f, 40f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            const float halfExtent = 18f;
            const float step = 2f;
            const float lineWidth = 0.045f;

            for (float pos = -halfExtent; pos <= halfExtent + 0.01f; pos += step)
            {
                CreateGridLine(stage.transform, $"GridLineX_{pos:0}", new Vector3(0f, 0.004f, pos), new Vector3(halfExtent * 2f, 0.008f, lineWidth), gridLineMat);
                CreateGridLine(stage.transform, $"GridLineZ_{pos:0}", new Vector3(pos, 0.004f, 0f), new Vector3(lineWidth, 0.008f, halfExtent * 2f), gridLineMat);
            }
        }

        private static void BuildCrashBarrierWall()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material barrierPanelMat = CreateOrUpdateLitMaterial(
                "Assets/Materials/VFX_CrashBarrier.mat",
                litShader,
                new Color(0.02f, 0.20f, 0.36f, 1f),
                0.6f,
                0.9f,
                new Color(0.0f, 0.65f, 1.25f)
            );

            Material barrierTrimMat = CreateOrUpdateLitMaterial(
                "Assets/Materials/VFX_CrashBarrierTrim.mat",
                litShader,
                new Color(0.1f, 0.9f, 1.0f, 1f),
                0.2f,
                0.9f,
                new Color(0.15f, 1.6f, 2.4f)
            );

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "CrashBarrierWall";
            wall.transform.position = new Vector3(0f, 1.1f, 3.4f);
            wall.transform.localScale = new Vector3(15f, 2.2f, 0.18f);
            wall.GetComponent<MeshRenderer>().sharedMaterial = barrierPanelMat;

            GameObject topRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topRail.name = "CrashBarrierTopRail";
            topRail.transform.SetParent(wall.transform, false);
            topRail.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            topRail.transform.localScale = new Vector3(1.01f, 0.04f, 1.35f);
            topRail.GetComponent<MeshRenderer>().sharedMaterial = barrierTrimMat;
            Object.DestroyImmediate(topRail.GetComponent<Collider>());

            GameObject baseRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseRail.name = "CrashBarrierBaseRail";
            baseRail.transform.SetParent(wall.transform, false);
            baseRail.transform.localPosition = new Vector3(0f, -0.49f, 0f);
            baseRail.transform.localScale = new Vector3(1.01f, 0.04f, 1.45f);
            baseRail.GetComponent<MeshRenderer>().sharedMaterial = barrierTrimMat;
            Object.DestroyImmediate(baseRail.GetComponent<Collider>());
        }

        private static GameObject SpawnCrashTestCar(string assetPath, string instanceName, Vector3 startPos)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject car;
            if (asset != null)
            {
                car = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            }
            else
            {
                car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            car.name = instanceName;
            car.transform.position = startPos;
            if (asset == null)
            {
                car.transform.rotation = Quaternion.identity;
                car.transform.localScale = new Vector3(1.5f, 0.75f, 3.0f);
            }
            else
            {
                car.transform.localScale = Vector3.one * 32f;
            }
            return car;
        }

        private static void CreateGridLine(Transform parent, string lineName, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = lineName;
            line.transform.SetParent(parent, false);
            line.transform.localPosition = pos;
            line.transform.localScale = scale;
            MeshRenderer mr = line.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        private static Material CreateOrUpdateLitMaterial(string assetPath, Shader shader, Color baseColor, float metallic, float smoothness, Color emissionColor)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor != Color.black && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static TronExplosionVFX SpawnShowcaseExplosion(GameObject prefab, string instanceName, Vector3 position, Color neonColor, float scale)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = instanceName;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            TronExplosionVFX vfx = go.GetComponent<TronExplosionVFX>();
            vfx.neonColor = neonColor;
            vfx.explosionScale = scale;
            vfx.crashDirection = Vector3.forward;
            vfx.loopInShowcase = false;
            vfx.playOnStart = false;
            vfx.loopInterval = 2.4f;
            vfx.playSound = false;
            vfx.ApplyParticleColors(neonColor);

            return vfx;
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}
