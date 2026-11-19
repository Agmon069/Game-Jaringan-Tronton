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
                EditorUtility.DisplayDialog("Sukses!", "Scene 'GameScene' dan semua Prefab Tron Light Cycle berhasil diperbarui dan siap dimainkan!", "OK");
            }
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

            // Build Tron Light Cycle Visual
            BuildTronCycleVisualHierarchy(root, neonMat, chassisMat);

            // Shield Bubble
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldBubble";
            shield.transform.SetParent(root.transform);
            shield.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            shield.transform.localScale = new Vector3(2.1f, 2.0f, 3.0f);
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

            // Build Tron Light Cycle Visual
            BuildTronCycleVisualHierarchy(root, neonMat, chassisMat);

            // Shield Bubble
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = "ShieldBubble";
            shield.transform.SetParent(root.transform);
            shield.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            shield.transform.localScale = new Vector3(2.1f, 2.0f, 3.0f);
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
            canvasObj.AddComponent<CanvasScaler>();
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

        private static void SetupGameOverModal(GameObject canvasObj, GameManager gm)
        {
            GameObject goPanel = new GameObject("GameOverModal");
            goPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform gort = goPanel.AddComponent<RectTransform>();
            gort.anchorMin = new Vector2(0.5f, 0.5f);
            gort.anchorMax = new Vector2(0.5f, 0.5f);
            gort.pivot = new Vector2(0.5f, 0.5f);
            gort.anchoredPosition = Vector2.zero;
            gort.sizeDelta = new Vector2(580, 440);

            Image goBg = goPanel.AddComponent<Image>();
            goBg.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);

            GameOverUI goUI = goPanel.AddComponent<GameOverUI>();
            goUI.panel = goPanel;
            gm.gameOverUI = goUI;

            // Title
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "GAME OVER";
            titleTMP.fontSize = 48;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = new Color(1f, 0.25f, 0.2f);
            titleTMP.alignment = TextAlignmentOptions.Center;
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 145);
            trt.sizeDelta = new Vector2(520, 65);
            goUI.titleText = titleTMP;

            // Killer Text
            GameObject killerObj = new GameObject("KillerText");
            killerObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI killerTMP = killerObj.AddComponent<TextMeshProUGUI>();
            killerTMP.text = "Dikalahkan oleh: Tron Bot";
            killerTMP.fontSize = 26;
            killerTMP.color = Color.white;
            killerTMP.alignment = TextAlignmentOptions.Center;
            RectTransform krt = killerObj.GetComponent<RectTransform>();
            krt.anchoredPosition = new Vector2(0, 70);
            krt.sizeDelta = new Vector2(520, 45);
            goUI.killerText = killerTMP;

            // Stats Text
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(goPanel.transform, false);
            TextMeshProUGUI statsTMP = statsObj.AddComponent<TextMeshProUGUI>();
            statsTMP.text = "Total Kills: 0";
            statsTMP.fontSize = 24;
            statsTMP.color = new Color(0.0f, 0.95f, 1.0f);
            statsTMP.alignment = TextAlignmentOptions.Center;
            RectTransform srt = statsObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, 20);
            srt.sizeDelta = new Vector2(520, 40);
            goUI.statsText = statsTMP;

            // Respawn Button
            GameObject respawnBtnObj = new GameObject("RespawnButton");
            respawnBtnObj.transform.SetParent(goPanel.transform, false);
            Image rImg = respawnBtnObj.AddComponent<Image>();
            rImg.color = new Color(0.0f, 0.7f, 0.85f);
            Button rBtn = respawnBtnObj.AddComponent<Button>();
            RectTransform rrt = respawnBtnObj.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0, -55);
            rrt.sizeDelta = new Vector2(380, 58);
            goUI.respawnButton = rBtn;

            GameObject rTxtObj = new GameObject("RespawnBtnText");
            rTxtObj.transform.SetParent(respawnBtnObj.transform, false);
            TextMeshProUGUI rTMP = rTxtObj.AddComponent<TextMeshProUGUI>();
            rTMP.text = "RESTART";
            rTMP.fontSize = 24;
            rTMP.fontStyle = FontStyles.Bold;
            rTMP.color = Color.white;
            rTMP.alignment = TextAlignmentOptions.Center;
            rTMP.textWrappingMode = TextWrappingModes.NoWrap;
            rTMP.overflowMode = TextOverflowModes.Overflow;
            RectTransform rtxtrt = rTxtObj.GetComponent<RectTransform>();
            rtxtrt.anchorMin = Vector2.zero;
            rtxtrt.anchorMax = Vector2.one;
            rtxtrt.pivot = new Vector2(0.5f, 0.5f);
            rtxtrt.anchoredPosition = Vector2.zero;
            rtxtrt.sizeDelta = Vector2.zero;
            rtxtrt.offsetMin = Vector2.zero;
            rtxtrt.offsetMax = Vector2.zero;

            // Exit Button
            GameObject exitBtnObj = new GameObject("ExitButton");
            exitBtnObj.transform.SetParent(goPanel.transform, false);
            Image eImg = exitBtnObj.AddComponent<Image>();
            eImg.color = new Color(0.85f, 0.25f, 0.2f);
            Button eBtn = exitBtnObj.AddComponent<Button>();
            RectTransform ert = exitBtnObj.GetComponent<RectTransform>();
            ert.anchoredPosition = new Vector2(0, -130);
            ert.sizeDelta = new Vector2(380, 58);
            goUI.exitButton = eBtn;

            GameObject eTxtObj = new GameObject("ExitBtnText");
            eTxtObj.transform.SetParent(exitBtnObj.transform, false);
            TextMeshProUGUI eTMP = eTxtObj.AddComponent<TextMeshProUGUI>();
            eTMP.text = "KEMBALI KE MAIN MENU";
            eTMP.fontSize = 22;
            eTMP.fontStyle = FontStyles.Bold;
            eTMP.color = Color.white;
            eTMP.alignment = TextAlignmentOptions.Center;
            eTMP.textWrappingMode = TextWrappingModes.NoWrap;
            eTMP.overflowMode = TextOverflowModes.Overflow;
            RectTransform etxtrt = eTxtObj.GetComponent<RectTransform>();
            etxtrt.anchorMin = Vector2.zero;
            etxtrt.anchorMax = Vector2.one;
            etxtrt.pivot = new Vector2(0.5f, 0.5f);
            etxtrt.anchoredPosition = Vector2.zero;
            etxtrt.sizeDelta = Vector2.zero;
            etxtrt.offsetMin = Vector2.zero;
            etxtrt.offsetMax = Vector2.zero;

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
                if (!File.Exists("Assets/Resources/PlayerTank.prefab")
                    || !File.Exists("Assets/Scenes/GameScene.unity")
                    || !File.Exists("Assets/Materials/TronCycleChassis.mat"))
                {
                    Debug.Log("[Tron Game] Menyiapkan Scene dan Prefab Tron Light Cycle secara otomatis...");
                    DiepGameSetup.SetupAll();
                }
            };
        }
    }
}
