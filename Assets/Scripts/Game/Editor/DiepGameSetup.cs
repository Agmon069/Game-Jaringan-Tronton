using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DiepMultiplayer.Editor
{
    public class DiepGameSetup : EditorWindow
    {
        [MenuItem("Tron Game/1. Setup Tron Game Scene & Prefabs", false, 1)]
        [MenuItem("Diep.io Game/1. Setup Game Scene & Prefabs", false, 1)]
        public static void SetupAllMenu()
        {
            SetupAll(true);
        }

        public static void SetupAll(bool showDialog = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string previousScenePath = SceneManager.GetActiveScene().path;

            EnsureDirectories();
            Material groundMat = CreateOrGetMaterial("Assets/Materials/ArenaGround.mat", new Color(0.03f, 0.04f, 0.08f));
            Material wallMat = CreateOrGetMaterial("Assets/Materials/ArenaWall.mat", new Color(0.05f, 0.45f, 0.75f), true);
            Material playerMat = CreateOrGetMaterial("Assets/Materials/TankPlayer.mat", new Color(0.0f, 0.95f, 1.0f), true);
            Material botMat = CreateOrGetMaterial("Assets/Materials/TankBot.mat", new Color(1.0f, 0.32f, 0.0f), true);
            Material chassisMat = CreateOrGetMaterial("Assets/Materials/TronCycleChassis.mat", new Color(0.07f, 0.08f, 0.12f));
            Material shieldMat = CreateOrGetMaterial("Assets/Materials/TankShield.mat", new Color(1.0f, 0.9f, 0.1f, 0.35f), true);
            Material bulletMat = CreateOrGetMaterial("Assets/Materials/BulletMat.mat", new Color(0.0f, 0.95f, 1.0f), true);

            GameObject bulletPrefab = CreateBulletPrefab(bulletMat);
            GameObject ammoOrbPrefab = CreateAmmoOrbPrefab();
            GameObject powerUpPrefab = CreatePowerUpPrefab();
            GameObject playerTankPrefab = CreatePlayerCyclePrefab(playerMat, chassisMat, shieldMat, bulletPrefab);
            GameObject botTankPrefab = CreateBotCyclePrefab(botMat, chassisMat, shieldMat, bulletPrefab);
            CreateOptionsOverlayPrefab();

            CreateGameScene(groundMat, wallMat, ammoOrbPrefab, powerUpPrefab);
            AddSceneToBuildSettings("Assets/Scenes/GameScene.unity");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previousScenePath) && previousScenePath != "Assets/Scenes/GameScene.unity" && File.Exists(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath);
            }

            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Sukses!", "Scene 'GameScene' dan semua Prefab Tron Car (Car Blue & Car Red) berhasil diperbarui dan siap dimainkan!", "OK");
            }
        }

        public static void SetupCarPrefabsOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureDirectories();
            Material playerMat = CreateOrGetMaterial("Assets/Materials/TankPlayer.mat", new Color(0.0f, 0.95f, 1.0f), true);
            Material botMat = CreateOrGetMaterial("Assets/Materials/TankBot.mat", new Color(1.0f, 0.32f, 0.0f), true);
            Material chassisMat = CreateOrGetMaterial("Assets/Materials/TronCycleChassis.mat", new Color(0.07f, 0.08f, 0.12f));
            Material shieldMat = CreateOrGetMaterial("Assets/Materials/TankShield.mat", new Color(1.0f, 0.9f, 0.1f, 0.35f), true);
            Material bulletMat = CreateOrGetMaterial("Assets/Materials/BulletMat.mat", new Color(0.0f, 0.95f, 1.0f), true);

            GameObject bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/BulletPrefab.prefab");
            if (bulletPrefab == null) bulletPrefab = CreateBulletPrefab(bulletMat);

            CreatePlayerCyclePrefab(playerMat, chassisMat, shieldMat, bulletPrefab);
            CreateBotCyclePrefab(botMat, chassisMat, shieldMat, bulletPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static Material CreateOrGetMaterial(string path, Color color, bool emissive = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                if (emissive && mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.5f);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                if (emissive && mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 1.5f);
                }
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject CreateBulletPrefab(Material mat)
        {
            string path = "Assets/Resources/BulletPrefab.prefab";
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "BulletPrefab";
            obj.transform.localScale = Vector3.one * 0.45f;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;

            SphereCollider col = obj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            Bullet bullet = obj.AddComponent<Bullet>();
            bullet.speed = 28f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateAmmoOrbPrefab()
        {
            string path = "Assets/Resources/AmmoOrbPrefab.prefab";
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = "AmmoOrbPrefab";
            obj.transform.localScale = Vector3.one * 0.7f;

            SphereCollider col = obj.GetComponent<SphereCollider>();
            col.isTrigger = true;

            AmmoOrb orb = obj.AddComponent<AmmoOrb>();
            orb.meshRenderer = obj.GetComponent<MeshRenderer>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreatePowerUpPrefab()
        {
            string path = "Assets/Resources/PowerUpPrefab.prefab";
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "PowerUpPrefab";
            obj.transform.localScale = Vector3.one * 1.1f;

            BoxCollider col = obj.GetComponent<BoxCollider>();
            col.isTrigger = true;

            PowerUpItem pItem = obj.AddComponent<PowerUpItem>();
            pItem.meshRenderer = obj.GetComponent<MeshRenderer>();

            // Floating text label
            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(obj.transform);
            textObj.transform.localPosition = new Vector3(0, 1.4f, 0);
            TextMeshPro tm = textObj.AddComponent<TextMeshPro>();
            tm.fontSize = 4;
            tm.alignment = TextAlignmentOptions.Center;
            pItem.labelText = tm;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            DestroyImmediate(obj);
            return prefab;
        }

        private static void BuildTronCarOrFallbackVisual(GameObject root, string carAssetPath, Material fallbackNeonMat, Material fallbackChassisMat)
        {
            GameObject carAsset = AssetDatabase.LoadAssetAtPath<GameObject>(carAssetPath);
            if (carAsset == null)
            {
                Debug.LogWarning($"[Tron Game] Asset 3D tidak ditemukan di '{carAssetPath}', menggunakan visual fallback.");
                BuildTronCycleVisualHierarchy(root, fallbackNeonMat, fallbackChassisMat);
                return;
            }

            GameObject visualRoot = new GameObject("TronCycleVisual");
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;
            visualRoot.transform.localScale = Vector3.one;

            GameObject carObj = (GameObject)PrefabUtility.InstantiatePrefab(carAsset);
            if (carObj == null)
            {
                carObj = Object.Instantiate(carAsset);
            }
            carObj.name = "CarModel";
            carObj.transform.SetParent(visualRoot.transform, false);
            carObj.transform.localPosition = Vector3.zero;
            carObj.transform.localRotation = Quaternion.identity;
            carObj.transform.localScale = Vector3.one;

            foreach (var col in carObj.GetComponentsInChildren<Collider>(true))
            {
                DestroyImmediate(col);
            }
            foreach (var anim in carObj.GetComponentsInChildren<Animator>(true))
            {
                if (anim.runtimeAnimatorController == null)
                {
                    DestroyImmediate(anim);
                }
            }

            AlignAndNormalizeCarModel(carObj, 2.8f);
        }

        private static void AlignAndNormalizeCarModel(GameObject carObj, float targetLength)
        {
            // 1. Align forward direction (+Z) using "Ban Depan" and "Ban Belakang" meshes if present
            MeshFilter[] meshFilters = carObj.GetComponentsInChildren<MeshFilter>(true);
            Vector3 frontSum = Vector3.zero;
            int frontCount = 0;
            Vector3 rearSum = Vector3.zero;
            int rearCount = 0;

            foreach (var mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                string lowerName = mf.gameObject.name.ToLowerInvariant();
                Vector3 worldCenter = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);

                if (lowerName.Contains("depan"))
                {
                    frontSum += worldCenter;
                    frontCount++;
                }
                else if (lowerName.Contains("belakang"))
                {
                    rearSum += worldCenter;
                    rearCount++;
                }
            }

            if (frontCount > 0 && rearCount > 0)
            {
                Vector3 frontCenter = frontSum / frontCount;
                Vector3 rearCenter = rearSum / rearCount;
                Vector3 forwardVec = frontCenter - rearCenter;

                // Ensure the forward axis lies in the XZ plane first if model came in vertically
                if (Mathf.Abs(forwardVec.y) > Mathf.Max(Mathf.Abs(forwardVec.x), Mathf.Abs(forwardVec.z)))
                {
                    Quaternion uprightRot = Quaternion.FromToRotation(forwardVec.normalized, Vector3.forward);
                    carObj.transform.rotation = uprightRot * carObj.transform.rotation;

                    // Re-evaluate wheel positions after upright rotation
                    frontSum = Vector3.zero;
                    rearSum = Vector3.zero;
                    foreach (var mf in meshFilters)
                    {
                        if (mf == null || mf.sharedMesh == null) continue;
                        string lowerName = mf.gameObject.name.ToLowerInvariant();
                        Vector3 worldCenter = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
                        if (lowerName.Contains("depan")) frontSum += worldCenter;
                        else if (lowerName.Contains("belakang")) rearSum += worldCenter;
                    }
                    forwardVec = (frontSum / frontCount) - (rearSum / rearCount);
                }

                forwardVec.y = 0f;
                if (forwardVec.sqrMagnitude > 0.0001f)
                {
                    Quaternion yawRot = Quaternion.FromToRotation(forwardVec.normalized, Vector3.forward);
                    carObj.transform.rotation = yawRot * carObj.transform.rotation;
                }
            }

            // 2. Ensure wheels sit below the body (upright +Y)
            if (TryCalculateCarMeshBounds(carObj, out Bounds preScaleBounds))
            {
                float wheelYSum = 0f;
                int wheelCount = 0;
                foreach (var mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null) continue;
                    if (mf.gameObject.name.ToLowerInvariant().Contains("ban"))
                    {
                        wheelYSum += mf.transform.TransformPoint(mf.sharedMesh.bounds.center).y;
                        wheelCount++;
                    }
                }
                if (wheelCount > 0 && (wheelYSum / wheelCount) > preScaleBounds.center.y)
                {
                    carObj.transform.rotation = Quaternion.Euler(0f, 0f, 180f) * carObj.transform.rotation;
                    TryCalculateCarMeshBounds(carObj, out preScaleBounds);
                }

                // 3. Uniformly scale so car length along +Z matches targetLength
                float currentLength = preScaleBounds.size.z;
                if (currentLength < 0.01f) currentLength = Mathf.Max(preScaleBounds.size.x, preScaleBounds.size.y, 0.01f);
                float uniformScale = targetLength / currentLength;
                carObj.transform.localScale *= uniformScale;

                // 4. Center horizontally and rest bottom of tires on ground (root spawns at y = 0.5)
                if (TryCalculateCarMeshBounds(carObj, out Bounds finalBounds))
                {
                    Vector3 offset = new Vector3(
                        -finalBounds.center.x,
                        -0.48f - finalBounds.min.y,
                        -finalBounds.center.z
                    );
                    carObj.transform.position += offset;
                }
            }
        }

        private static bool TryCalculateCarMeshBounds(GameObject rootObj, out Bounds combinedBounds)
        {
            combinedBounds = new Bounds();
            bool hasBounds = false;

            MeshFilter[] filters = rootObj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds localB = mf.sharedMesh.bounds;
                Vector3 c = localB.center;
                Vector3 e = localB.extents;
                Matrix4x4 m = mf.transform.localToWorldMatrix;

                for (int sx = -1; sx <= 1; sx += 2)
                {
                    for (int sy = -1; sy <= 1; sy += 2)
                    {
                        for (int sz = -1; sz <= 1; sz += 2)
                        {
                            Vector3 worldCorner = m.MultiplyPoint3x4(c + new Vector3(e.x * sx, e.y * sy, e.z * sz));
                            if (!hasBounds)
                            {
                                combinedBounds = new Bounds(worldCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                combinedBounds.Encapsulate(worldCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static void BuildTronCycleVisualHierarchy(GameObject root, Material neonMat, Material chassisMat)
        {
            GameObject visualRoot = new GameObject("TronCycleVisual");
            visualRoot.transform.SetParent(root.transform);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            // 1. Dark Cyber Chassis Core
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "ChassisCore";
            chassis.transform.SetParent(visualRoot.transform);
            chassis.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            chassis.transform.localScale = new Vector3(0.72f, 0.48f, 2.0f);
            chassis.GetComponent<MeshRenderer>().sharedMaterial = chassisMat;
            DestroyImmediate(chassis.GetComponent<Collider>());

            // 2. Glowing Neon Canopy Spine
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopy.name = "NeonCanopy";
            canopy.transform.SetParent(visualRoot.transform);
            canopy.transform.localPosition = new Vector3(0f, 0.52f, -0.05f);
            canopy.transform.localScale = new Vector3(0.46f, 0.28f, 1.45f);
            canopy.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
            DestroyImmediate(canopy.GetComponent<Collider>());

            // 3. Glowing Side Neon Rails
            GameObject sideRails = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideRails.name = "NeonRails";
            sideRails.transform.SetParent(visualRoot.transform);
            sideRails.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            sideRails.transform.localScale = new Vector3(0.86f, 0.14f, 1.75f);
            sideRails.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
            DestroyImmediate(sideRails.GetComponent<Collider>());

            // 4. Front Light Wheel
            GameObject frontWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            frontWheel.name = "FrontWheelNeon";
            frontWheel.transform.SetParent(visualRoot.transform);
            frontWheel.transform.localPosition = new Vector3(0f, 0.24f, 0.92f);
            frontWheel.transform.localScale = new Vector3(0.45f, 0.72f, 0.78f);
            frontWheel.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
            DestroyImmediate(frontWheel.GetComponent<Collider>());

            // 5. Rear Light Wheel
            GameObject rearWheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rearWheel.name = "RearWheelNeon";
            rearWheel.transform.SetParent(visualRoot.transform);
            rearWheel.transform.localPosition = new Vector3(0f, 0.24f, -0.92f);
            rearWheel.transform.localScale = new Vector3(0.48f, 0.72f, 0.78f);
            rearWheel.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
            DestroyImmediate(rearWheel.GetComponent<Collider>());
        }

        private static GameObject CreatePlayerCyclePrefab(Material neonMat, Material chassisMat, Material shieldMat, GameObject bulletPrefab)
        {
            string path = "Assets/Resources/PlayerTank.prefab";
            GameObject root = new GameObject("PlayerTank");

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 4f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            SphereCollider sphereCol = root.AddComponent<SphereCollider>();
            sphereCol.radius = 0.65f;

            PhotonView pv = root.AddComponent<PhotonView>();
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            PhotonTransformView ptv = root.AddComponent<PhotonTransformView>();

            PlayerTankController ctrl = root.AddComponent<PlayerTankController>();
            ctrl.rb = rb;
            ctrl.bulletPrefab = bulletPrefab;

            pv.ObservedComponents = new List<Component> { ptv, ctrl };

            // Build Tron 3D Car Visual (Assets/3D/Car Blue.fbx)
            BuildTronCarOrFallbackVisual(root, "Assets/3D/Car Blue.fbx", neonMat, chassisMat);

            // Shield Bubble
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldBubble";
            shield.transform.SetParent(root.transform);
            shield.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            shield.transform.localScale = new Vector3(2.2f, 1.8f, 3.4f);
            shield.GetComponent<MeshRenderer>().sharedMaterial = shieldMat;
            DestroyImmediate(shield.GetComponent<Collider>());
            ctrl.shieldBubble = shield;
            shield.SetActive(false);

            SetupOverheadUI(root, ctrl);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBotCyclePrefab(Material neonMat, Material chassisMat, Material shieldMat, GameObject bulletPrefab)
        {
            string path = "Assets/Resources/BotTank.prefab";
            GameObject root = new GameObject("BotTank");

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 4f;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            SphereCollider sphereCol = root.AddComponent<SphereCollider>();
            sphereCol.radius = 0.65f;

            PhotonView pv = root.AddComponent<PhotonView>();
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;
            PhotonTransformView ptv = root.AddComponent<PhotonTransformView>();

            BotController ctrl = root.AddComponent<BotController>();
            ctrl.rb = rb;
            ctrl.bulletPrefab = bulletPrefab;

            pv.ObservedComponents = new List<Component> { ptv, ctrl };

            // Build Tron 3D Car Visual (Assets/3D/Car Red.fbx)
            BuildTronCarOrFallbackVisual(root, "Assets/3D/Car Red.fbx", neonMat, chassisMat);

            // Shield Bubble
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldBubble";
            shield.transform.SetParent(root.transform);
            shield.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            shield.transform.localScale = new Vector3(2.2f, 1.8f, 3.4f);
            shield.GetComponent<MeshRenderer>().sharedMaterial = shieldMat;
            DestroyImmediate(shield.GetComponent<Collider>());
            ctrl.shieldBubble = shield;
            shield.SetActive(false);

            SetupOverheadUI(root, ctrl);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            DestroyImmediate(root);
            return prefab;
        }

        private static void SetupOverheadUI(GameObject root, TankBase ctrl)
        {
            GameObject canvasObj = new GameObject("OverheadCanvas");
            canvasObj.transform.SetParent(root.transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.4f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform crt = canvas.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(200, 70);
            crt.localScale = Vector3.one * 0.015f;

            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(canvasObj.transform);
            TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
            nameTMP.text = "LightCycle";
            nameTMP.fontSize = 20;
            nameTMP.alignment = TextAlignmentOptions.Center;
            nameTMP.color = Color.white;
            RectTransform nrt = nameTMP.GetComponent<RectTransform>();
            nrt.localPosition = new Vector3(0, 20, 0);
            nrt.sizeDelta = new Vector2(200, 30);
            nameObj.SetActive(false);

            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(canvasObj.transform);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.maxValue = 100;
            slider.value = 100;

            GameObject ammoObj = new GameObject("AmmoText");
            ammoObj.transform.SetParent(canvasObj.transform);
            TextMeshProUGUI ammoTMP = ammoObj.AddComponent<TextMeshProUGUI>();
            ammoObj.SetActive(false);

            canvasObj.SetActive(false);

            if (ctrl is PlayerTankController pt)
            {
                pt.overheadCanvas = canvas;
                pt.nameText = nameTMP;
                pt.healthSlider = slider;
                pt.ammoText = ammoTMP;
            }
            else if (ctrl is BotController bt)
            {
                bt.overheadCanvas = canvas;
                bt.nameText = nameTMP;
                bt.healthSlider = slider;
                bt.ammoText = ammoTMP;
            }
        }

        private static void CreateGameScene(Material groundMat, Material wallMat, GameObject ammoOrbPrefab, GameObject powerUpPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string scenePath = "Assets/Scenes/GameScene.unity";

            // 1. Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Arena Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(7.5f, 1f, 7.5f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 3. Boundary Walls
            CreateArenaWalls(wallMat);

            // 4. Main Camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.02f, 0.04f);
            camObj.AddComponent<AudioListener>();
            CameraFollow follow = camObj.AddComponent<CameraFollow>();
            follow.offset = new Vector3(0f, 26f, -14f);
            camObj.transform.position = new Vector3(0f, 26f, -14f);
            camObj.transform.rotation = Quaternion.Euler(62f, 0f, 0f);

            // 5. Spawner
            GameObject spawnerObj = new GameObject("ArenaSpawner");
            ArenaSpawner spawner = spawnerObj.AddComponent<ArenaSpawner>();
            spawner.ammoOrbPrefab = ammoOrbPrefab;
            spawner.powerUpPrefab = powerUpPrefab;
            spawner.initialAmmoOrbCount = 0;
            spawner.initialPowerUpCount = 10;
            spawner.minX = -30f;
            spawner.maxX = 30f;
            spawner.minZ = -30f;
            spawner.maxZ = 30f;

            // 6. UI Canvas & EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasObj = new GameObject("HUDCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler hudScaler = canvasObj.AddComponent<CanvasScaler>();
            hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution = new Vector2(1920f, 1080f);
            hudScaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            SetupSceneUI(canvasObj, out GameManager gm);

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateArenaWalls(Material wallMat)
        {
            GameObject wallsRoot = new GameObject("ArenaWalls");
            float size = 70f;
            float thickness = 2f;
            float height = 5f;

            CreateWall(wallsRoot.transform, new Vector3(0, height / 2, size / 2), new Vector3(size + thickness, height, thickness), wallMat);
            CreateWall(wallsRoot.transform, new Vector3(0, height / 2, -size / 2), new Vector3(size + thickness, height, thickness), wallMat);
            CreateWall(wallsRoot.transform, new Vector3(size / 2, height / 2, 0), new Vector3(thickness, height, size + thickness), wallMat);
            CreateWall(wallsRoot.transform, new Vector3(-size / 2, height / 2, 0), new Vector3(thickness, height, size + thickness), wallMat);
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.tag = "Boundary";
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void SetupSceneUI(GameObject canvasObj, out GameManager gm)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();

            GameObject lbObj = new GameObject("LeaderboardManager");
            LeaderboardManager lb = lbObj.AddComponent<LeaderboardManager>();

            // Top-Right Leaderboard Panel
            GameObject lbPanel = new GameObject("LeaderboardPanel");
            lbPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform lbrt = lbPanel.AddComponent<RectTransform>();
            lbrt.anchorMin = new Vector2(1, 1);
            lbrt.anchorMax = new Vector2(1, 1);
            lbrt.pivot = new Vector2(1, 1);
            lbrt.anchoredPosition = new Vector2(-24, -24);
            lbrt.sizeDelta = new Vector2(390, 300);

            Image lbBg = lbPanel.AddComponent<Image>();
            lbBg.color = new Color(0f, 0.03f, 0.08f, 0.65f);

            GameObject lbTextObj = new GameObject("LeaderboardText");
            lbTextObj.transform.SetParent(lbPanel.transform, false);
            TextMeshProUGUI lbTMP = lbTextObj.AddComponent<TextMeshProUGUI>();
            lbTMP.fontSize = 22;
            lbTMP.lineSpacing = 10;
            RectTransform lbtxtrt = lbTextObj.GetComponent<RectTransform>();
            lbtxtrt.anchorMin = Vector2.zero;
            lbtxtrt.anchorMax = Vector2.one;
            lbtxtrt.offsetMin = new Vector2(18, 16);
            lbtxtrt.offsetMax = new Vector2(-18, -16);
            lb.leaderboardText = lbTMP;

            // Bottom-Left Player HUD Panel
            GameObject hudPanel = new GameObject("PlayerHUDPanel");
            hudPanel.transform.SetParent(canvasObj.transform);
            RectTransform hudrt = hudPanel.AddComponent<RectTransform>();
            hudrt.anchorMin = new Vector2(0, 0);
            hudrt.anchorMax = new Vector2(0, 0);
            hudrt.pivot = new Vector2(0, 0);
            hudrt.anchoredPosition = new Vector2(25, 25);
            hudrt.sizeDelta = new Vector2(330, 140);

            Image hudBg = hudPanel.AddComponent<Image>();
            hudBg.color = new Color(0f, 0.03f, 0.08f, 0.7f);

            // Status Bar
            GameObject hpSliderObj = new GameObject("HUDHealthSlider");
            hpSliderObj.transform.SetParent(hudPanel.transform);
            Slider hpSlider = hpSliderObj.AddComponent<Slider>();
            hpSlider.maxValue = 100;
            hpSlider.value = 100;
            RectTransform hpsrt = hpSliderObj.GetComponent<RectTransform>();
            hpsrt.anchorMin = new Vector2(0, 1);
            hpsrt.anchorMax = new Vector2(1, 1);
            hpsrt.pivot = new Vector2(0.5f, 1);
            hpsrt.anchoredPosition = new Vector2(0, -20);
            hpsrt.sizeDelta = new Vector2(-30, 24);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(hpSliderObj.transform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.0f, 0.9f, 1.0f);
            RectTransform fillrt = fill.GetComponent<RectTransform>();
            fillrt.anchorMin = Vector2.zero;
            fillrt.anchorMax = Vector2.one;
            fillrt.sizeDelta = Vector2.zero;
            hpSlider.fillRect = fillrt;
            gm.hudHealthSlider = hpSlider;

            // Status Text
            GameObject hpTextObj = new GameObject("HUDHealthText");
            hpTextObj.transform.SetParent(hudPanel.transform);
            TextMeshProUGUI hpTMP = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpTMP.text = "STATUS: LIGHT CYCLE AKTIF";
            hpTMP.fontSize = 15;
            hpTMP.alignment = TextAlignmentOptions.Center;
            RectTransform hptxtrt = hpTextObj.GetComponent<RectTransform>();
            hptxtrt.anchoredPosition = new Vector2(0, 35);
            hptxtrt.sizeDelta = new Vector2(290, 25);
            gm.hudHealthText = hpTMP;

            // Kills & Trail Text
            GameObject ammoTextObj = new GameObject("HUDAmmoText");
            ammoTextObj.transform.SetParent(hudPanel.transform);
            TextMeshProUGUI ammoTMP = ammoTextObj.AddComponent<TextMeshProUGUI>();
            ammoTMP.text = "KILLS: 0  |  JEJAK: 4.5s";
            ammoTMP.fontSize = 18;
            ammoTMP.color = new Color(0.0f, 0.95f, 1.0f);
            ammoTMP.fontStyle = FontStyles.Bold;
            ammoTMP.alignment = TextAlignmentOptions.Center;
            RectTransform ammort = ammoTextObj.GetComponent<RectTransform>();
            ammort.anchoredPosition = new Vector2(0, 0);
            ammort.sizeDelta = new Vector2(290, 30);
            gm.hudAmmoText = ammoTMP;

            // Power-Up HUD Indicator
            GameObject puPanel = new GameObject("PowerUpHUDPanel");
            puPanel.transform.SetParent(hudPanel.transform);
            RectTransform purt = puPanel.AddComponent<RectTransform>();
            purt.anchoredPosition = new Vector2(0, -40);
            purt.sizeDelta = new Vector2(280, 30);
            gm.hudPowerUpPanel = puPanel;

            GameObject puNameObj = new GameObject("PowerUpName");
            puNameObj.transform.SetParent(puPanel.transform);
            TextMeshProUGUI puNameTMP = puNameObj.AddComponent<TextMeshProUGUI>();
            puNameTMP.text = "POWER UP";
            puNameTMP.fontSize = 14;
            puNameTMP.color = new Color(1f, 0.9f, 0.1f);
            puNameTMP.alignment = TextAlignmentOptions.Center;
            RectTransform punamert = puNameObj.GetComponent<RectTransform>();
            punamert.anchoredPosition = new Vector2(0, 10);
            punamert.sizeDelta = new Vector2(280, 20);
            gm.hudPowerUpNameText = puNameTMP;

            GameObject puSliderObj = new GameObject("PowerUpSlider");
            puSliderObj.transform.SetParent(puPanel.transform);
            Slider puSlider = puSliderObj.AddComponent<Slider>();
            puSlider.maxValue = 7;
            puSlider.value = 7;
            RectTransform pusrt = puSliderObj.GetComponent<RectTransform>();
            pusrt.anchoredPosition = new Vector2(0, -6);
            pusrt.sizeDelta = new Vector2(250, 10);
            GameObject puFill = new GameObject("PUFill");
            puFill.transform.SetParent(puSliderObj.transform);
            Image puFillImg = puFill.AddComponent<Image>();
            puFillImg.color = new Color(1f, 0.9f, 0.1f);
            RectTransform pufrt = puFill.GetComponent<RectTransform>();
            pufrt.anchorMin = Vector2.zero;
            pufrt.anchorMax = Vector2.one;
            pufrt.sizeDelta = Vector2.zero;
            puSlider.fillRect = pufrt;
            gm.hudPowerUpTimerSlider = puSlider;
            puPanel.SetActive(false);

            SetupGameOverModal(canvasObj, gm);
        }

        private static Sprite LoadUISprite(string path)
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (subAssets != null)
            {
                foreach (var obj in subAssets)
                {
                    if (obj is Sprite s) return s;
                }
            }
            return null;
        }

        private static GameObject CreateOptionsOverlayPrefab()
        {
            string path = "Assets/Resources/OptionsOverlayPrefab.prefab";

            Sprite panelFrameSprite = LoadUISprite("Assets/UI/UI-cyber- gede(ga make glowing.png");
            Sprite settingSprite = LoadUISprite("Assets/UI/UI-cyber-setting.png");
            Sprite exitSprite = LoadUISprite("Assets/UI/UI-cyber-exit.png");
            Sprite longBtnSprite = LoadUISprite("Assets/UI/UI-cyber-panjang.png");
            Sprite squareSprite = LoadUISprite("Assets/UI/UI-cyber-kotak.png");

            GameObject canvasObj = new GameObject("OptionsOverlayCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            OptionsPanelUI optUI = canvasObj.AddComponent<OptionsPanelUI>();

            // 1. Lobby Top-Right Option Button (hidden automatically in GameScene)
            GameObject lobbyBtnObj = new GameObject("LobbyOptionButton", typeof(RectTransform));
            lobbyBtnObj.transform.SetParent(canvasObj.transform, false);
            RectTransform lbrt = lobbyBtnObj.GetComponent<RectTransform>();
            lbrt.anchorMin = new Vector2(1f, 1f);
            lbrt.anchorMax = new Vector2(1f, 1f);
            lbrt.pivot = new Vector2(1f, 1f);
            lbrt.anchoredPosition = new Vector2(-36f, -32f);
            lbrt.sizeDelta = new Vector2(96f, 96f);

            Image lobbyBtnImg = lobbyBtnObj.AddComponent<Image>();
            if (settingSprite != null)
            {
                lobbyBtnImg.sprite = settingSprite;
                lobbyBtnImg.preserveAspect = true;
            }
            else
            {
                lobbyBtnImg.color = new Color(0.08f, 0.25f, 0.45f, 0.95f);
            }
            Button lobbyBtn = lobbyBtnObj.AddComponent<Button>();
            optUI.lobbyOptionButton = lobbyBtn;

            GameObject lobbyLblObj = new GameObject("Label", typeof(RectTransform));
            lobbyLblObj.transform.SetParent(lobbyBtnObj.transform, false);
            TextMeshProUGUI lobbyLbl = lobbyLblObj.AddComponent<TextMeshProUGUI>();
            lobbyLbl.text = "OPTION";
            lobbyLbl.fontSize = 18f;
            lobbyLbl.fontStyle = FontStyles.Bold;
            lobbyLbl.color = Color.white;
            lobbyLbl.alignment = TextAlignmentOptions.Center;
            RectTransform llrt = lobbyLblObj.GetComponent<RectTransform>();
            llrt.anchorMin = new Vector2(0.5f, 0f);
            llrt.anchorMax = new Vector2(0.5f, 0f);
            llrt.pivot = new Vector2(0.5f, 1f);
            llrt.anchoredPosition = new Vector2(0f, -4f);
            llrt.sizeDelta = new Vector2(130f, 28f);

            // 2. Backdrop
            GameObject backdropObj = new GameObject("OptionsBackdrop", typeof(RectTransform));
            backdropObj.transform.SetParent(canvasObj.transform, false);
            RectTransform bdrt = backdropObj.GetComponent<RectTransform>();
            bdrt.anchorMin = Vector2.zero;
            bdrt.anchorMax = Vector2.one;
            bdrt.sizeDelta = Vector2.zero;
            Image bdImg = backdropObj.AddComponent<Image>();
            bdImg.color = new Color(0.01f, 0.02f, 0.05f, 0.72f);
            optUI.optionsBackdrop = backdropObj;

            // 3. Main Options Modal Frame
            GameObject modalObj = new GameObject("OptionsModal", typeof(RectTransform));
            modalObj.transform.SetParent(backdropObj.transform, false);
            RectTransform mrt = modalObj.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0.5f, 0.5f);
            mrt.anchorMax = new Vector2(0.5f, 0.5f);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.anchoredPosition = Vector2.zero;
            mrt.sizeDelta = new Vector2(900f, 636f);

            Image modalImg = modalObj.AddComponent<Image>();
            if (panelFrameSprite != null)
            {
                modalImg.sprite = panelFrameSprite;
                modalImg.preserveAspect = true;
            }
            else
            {
                modalImg.color = new Color(0.05f, 0.07f, 0.14f, 0.96f);
            }
            optUI.optionsPanel = modalObj;

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.transform.SetParent(modalObj.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "OPTIONS";
            titleTMP.fontSize = 44f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(0f, 0.95f, 1f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 152f);
            trt.sizeDelta = new Vector2(520f, 60f);

            // Top-Right Close Button (UI-cyber-exit)
            GameObject closeObj = new GameObject("CloseButton", typeof(RectTransform));
            closeObj.transform.SetParent(modalObj.transform, false);
            RectTransform clrt = closeObj.GetComponent<RectTransform>();
            clrt.anchoredPosition = new Vector2(315f, 152f);
            clrt.sizeDelta = new Vector2(68f, 68f);
            Image closeImg = closeObj.AddComponent<Image>();
            if (exitSprite != null)
            {
                closeImg.sprite = exitSprite;
                closeImg.preserveAspect = true;
            }
            else
            {
                closeImg.color = new Color(0.85f, 0.25f, 0.2f);
            }
            optUI.closeButton = closeObj.AddComponent<Button>();

            // Row 1: BGM Slider
            CreateOptionSliderRow(modalObj.transform, "BGMRow", new Vector2(0f, 74f), "MUSIK (BGM)", squareSprite, out Slider bgmSlider, out TMP_Text bgmValText);
            optUI.bgmSlider = bgmSlider;
            optUI.bgmValueText = bgmValText;

            // Row 2: SFX Slider
            CreateOptionSliderRow(modalObj.transform, "SFXRow", new Vector2(0f, 12f), "EFEK SUARA", squareSprite, out Slider sfxSlider, out TMP_Text sfxValText);
            optUI.sfxSlider = sfxSlider;
            optUI.sfxValueText = sfxValText;

            // Row 3: Mute Toggle Button
            GameObject muteLabelObj = new GameObject("MuteLabel", typeof(RectTransform));
            muteLabelObj.transform.SetParent(modalObj.transform, false);
            TextMeshProUGUI muteLbl = muteLabelObj.AddComponent<TextMeshProUGUI>();
            muteLbl.text = "SUARA GAME";
            muteLbl.fontSize = 23f;
            muteLbl.fontStyle = FontStyles.Bold;
            muteLbl.color = Color.white;
            muteLbl.alignment = TextAlignmentOptions.Left;
            RectTransform mlrt = muteLabelObj.GetComponent<RectTransform>();
            mlrt.anchoredPosition = new Vector2(-190f, -52f);
            mlrt.sizeDelta = new Vector2(240f, 40f);

            Button muteBtn = CreateCyberPillButton(modalObj.transform, "MuteButton", new Vector2(105f, -52f), new Vector2(290f, 80f), longBtnSprite, null, "AUDIO: AKTIF", 21f, out TMP_Text muteBtnTxt);
            optUI.muteButton = muteBtn;
            optUI.muteButtonText = muteBtnTxt;

            // Row 4: Fullscreen + Save & Close Buttons
            Button fsBtn = CreateCyberPillButton(modalObj.transform, "FullscreenButton", new Vector2(-155f, -135f), new Vector2(280f, 84f), longBtnSprite, null, "LAYAR: FULLSCREEN", 20f, out TMP_Text fsBtnTxt);
            optUI.fullscreenButton = fsBtn;
            optUI.fullscreenButtonText = fsBtnTxt;

            Button saveBtn = CreateCyberPillButton(modalObj.transform, "SaveCloseButton", new Vector2(155f, -135f), new Vector2(280f, 84f), longBtnSprite, null, "SIMPAN & TUTUP", 20f, out _);
            optUI.saveCloseButton = saveBtn;

            backdropObj.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObj, path);
            DestroyImmediate(canvasObj);
            return prefab;
        }

        private static void CreateOptionSliderRow(Transform parent, string name, Vector2 pos, string label, Sprite handleSprite, out Slider slider, out TMP_Text valueTMP)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rrt = row.GetComponent<RectTransform>();
            rrt.anchoredPosition = pos;
            rrt.sizeDelta = new Vector2(660f, 48f);

            GameObject lblObj = new GameObject("Label", typeof(RectTransform));
            lblObj.transform.SetParent(row.transform, false);
            TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
            lbl.text = label;
            lbl.fontSize = 23f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Left;
            RectTransform lrt = lblObj.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(-200f, 0f);
            lrt.sizeDelta = new Vector2(230f, 40f);

            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
            sliderObj.transform.SetParent(row.transform, false);
            RectTransform srt = sliderObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(65f, 0f);
            srt.sizeDelta = new Vector2(270f, 24f);

            slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;

            // Track Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.1f, 0.2f, 0.95f);
            RectTransform bgrt = bgObj.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.sizeDelta = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fart = fillArea.GetComponent<RectTransform>();
            fart.anchorMin = Vector2.zero;
            fart.anchorMax = Vector2.one;
            fart.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0f, 0.88f, 1f);
            RectTransform frt = fillObj.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.sizeDelta = Vector2.zero;
            slider.fillRect = frt;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform hart = handleArea.GetComponent<RectTransform>();
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.sizeDelta = Vector2.zero;

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform));
            handleObj.transform.SetParent(handleArea.transform, false);
            Image handleImg = handleObj.AddComponent<Image>();
            if (handleSprite != null)
            {
                handleImg.sprite = handleSprite;
                handleImg.preserveAspect = true;
            }
            else
            {
                handleImg.color = Color.white;
            }
            RectTransform hrt = handleObj.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(34f, 34f);
            slider.handleRect = hrt;
            slider.targetGraphic = handleImg;

            // Value Percentage Text
            GameObject valObj = new GameObject("ValueText", typeof(RectTransform));
            valObj.transform.SetParent(row.transform, false);
            valueTMP = valObj.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "50%";
            valueTMP.fontSize = 22f;
            valueTMP.fontStyle = FontStyles.Bold;
            valueTMP.color = new Color(0f, 0.95f, 1f);
            valueTMP.alignment = TextAlignmentOptions.Right;
            RectTransform vrt = valObj.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(255f, 0f);
            vrt.sizeDelta = new Vector2(85f, 40f);
        }

        private static Button CreateCyberPillButton(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Sprite bgSprite, Sprite iconSprite, string label, float fontSize, out TMP_Text labelTMP)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            RectTransform brt = btnObj.GetComponent<RectTransform>();
            brt.anchoredPosition = anchoredPos;
            brt.sizeDelta = size;

            Image bgImg = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                bgImg.sprite = bgSprite;
                bgImg.preserveAspect = true;
            }
            else
            {
                bgImg.color = new Color(0.08f, 0.25f, 0.45f);
            }

            Button btn = btnObj.AddComponent<Button>();

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(btnObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                RectTransform irt = iconObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = new Vector2(size.y * 0.55f, 0f);
                irt.sizeDelta = new Vector2(size.y * 0.52f, size.y * 0.52f);
            }

            GameObject txtObj = new GameObject("LabelText", typeof(RectTransform));
            txtObj.transform.SetParent(btnObj.transform, false);
            labelTMP = txtObj.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = fontSize;
            labelTMP.fontStyle = FontStyles.Bold;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.textWrappingMode = TextWrappingModes.NoWrap;
            labelTMP.overflowMode = TextOverflowModes.Overflow;
            labelTMP.raycastTarget = false;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = new Vector2(iconSprite != null ? size.y * 0.72f : 12f, 4f);
            trt.offsetMax = new Vector2(-12f, -4f);

            return btn;
        }

        private static void SetupGameOverModal(GameObject canvasObj, GameManager gm)
        {
            Sprite panelFrameSprite = LoadUISprite("Assets/UI/UI-cyber- gede(ga make glowing.png");
            Sprite longBtnSprite = LoadUISprite("Assets/UI/UI-cyber-panjang.png");
            Sprite restartSprite = LoadUISprite("Assets/UI/UI-cyber-restart.png");
            Sprite exitSprite = LoadUISprite("Assets/UI/UI-cyber-exit.png");
            Sprite settingSprite = LoadUISprite("Assets/UI/UI-cyber-setting.png");

            GameObject goPanel = new GameObject("GameOverModal");
            goPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform gort = goPanel.AddComponent<RectTransform>();
            gort.anchorMin = new Vector2(0.5f, 0.5f);
            gort.anchorMax = new Vector2(0.5f, 0.5f);
            gort.pivot = new Vector2(0.5f, 0.5f);
            gort.anchoredPosition = Vector2.zero;
            gort.sizeDelta = new Vector2(900f, 636f);

            Image goBg = goPanel.AddComponent<Image>();
            if (panelFrameSprite != null)
            {
                goBg.sprite = panelFrameSprite;
                goBg.preserveAspect = true;
            }
            else
            {
                goBg.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);
            }

            GameOverUI goUI = goPanel.AddComponent<GameOverUI>();
            goUI.panel = goPanel;
            gm.gameOverUI = goUI;

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "GAME OVER";
            titleTMP.fontSize = 48f;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.25f, 0.2f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 148f);
            trt.sizeDelta = new Vector2(580f, 60f);
            goUI.titleText = titleTMP;

            // Killer Text
            GameObject killerObj = new GameObject("KillerText");
            killerObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI killerTMP = killerObj.AddComponent<TextMeshProUGUI>();
            killerTMP.text = "Dikalahkan oleh: Tron Bot";
            killerTMP.fontSize = 25f;
            killerTMP.color = Color.white;
            killerTMP.alignment = TextAlignmentOptions.Center;
            RectTransform krt = killerObj.GetComponent<RectTransform>();
            krt.anchoredPosition = new Vector2(0f, 84f);
            krt.sizeDelta = new Vector2(600f, 42f);
            goUI.killerText = killerTMP;

            // Stats Text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
            statsTMP.text = "Total Kills: 0";
            statsTMP.fontSize = 23f;
            statsTMP.color = new Color(0.0f, 0.95f, 1.0f);
            statsTMP.alignment = TextAlignmentOptions.Center;
            RectTransform srt = statsObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0f, 40f);
            srt.sizeDelta = new Vector2(580f, 38f);
            goUI.statsText = statsTMP;

            // Row 1 Buttons: RESTART (Left) + MAIN MENU (Right) using UI-cyber-panjang & icons
            Button rBtn = CreateCyberPillButton(goPanel.transform, "RespawnButton", new Vector2(-158f, -34f), new Vector2(290f, 94f), longBtnSprite, restartSprite, "RESTART", 22f, out _);
            goUI.respawnButton = rBtn;

            Button eBtn = CreateCyberPillButton(goPanel.transform, "ExitButton", new Vector2(158f, -34f), new Vector2(290f, 94f), longBtnSprite, exitSprite, "MAIN MENU", 22f, out _);
            goUI.exitButton = eBtn;

            // Row 2 Button: OPTION (Only exists when Game Over panel is active in GameScene!)
            Button optBtn = CreateCyberPillButton(goPanel.transform, "OptionButton", new Vector2(0f, -132f), new Vector2(290f, 90f), longBtnSprite, settingSprite, "OPTION", 22f, out _);
            goUI.optionButton = optBtn;

            goPanel.SetActive(false);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool alreadyExists = false;
            foreach (var s in scenes)
            {
                if (s.path == scenePath)
                {
                    alreadyExists = true;
                    s.enabled = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    [InitializeOnLoad]
    public static class DiepGameAutoInit
    {
        static DiepGameAutoInit()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (!File.Exists("Assets/Resources/PlayerTank.prefab")
                    || !File.Exists("Assets/Resources/OptionsOverlayPrefab.prefab")
                    || !File.Exists("Assets/Scenes/GameScene.unity")
                    || !File.Exists("Assets/Materials/TronCycleChassis.mat"))
                {
                    Debug.Log("[Tron Game] Menyiapkan Scene, Prefab UI Cyber, dan Tron Light Cycle secara otomatis...");
                    DiepGameSetup.SetupAll();
                    return;
                }

                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/PlayerTank.prefab");
                GameObject botPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/BotTank.prefab");
                bool missingCarModel = (playerPrefab == null || playerPrefab.transform.Find("TronCycleVisual/CarModel") == null)
                                    || (botPrefab == null || botPrefab.transform.Find("TronCycleVisual/CarModel") == null);

                if (missingCarModel && (File.Exists("Assets/3D/Car Blue.fbx") || File.Exists("Assets/3D/Car Red.fbx")))
                {
                    Debug.Log("[Tron Game] Memperbarui prefab PlayerTank & BotTank menggunakan model 3D Car Blue & Car Red...");
                    DiepGameSetup.SetupCarPrefabsOnly();
                }
            };
        }
    }
}
