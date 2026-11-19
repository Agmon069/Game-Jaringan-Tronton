using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DiepMultiplayer
{
    public class ArenaSpawner : MonoBehaviour
    {
        public static ArenaSpawner Instance { get; private set; }

        [Header("Arena Bounds")]
        public float minX = -30f;
        public float maxX = 30f;
        public float minZ = -30f;
        public float maxZ = 30f;

        [Header("Prefabs")]
        public GameObject ammoOrbPrefab;
        public GameObject powerUpPrefab;

        [Header("Spawn Counts")]
        public int initialAmmoOrbCount = 0;
        public int initialPowerUpCount = 10;

        private List<PowerUpItem> activePowerUps = new List<PowerUpItem>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ApplyTronArenaTheme();
            SpawnInitialPowerUps();
        }

        private void ApplyTronArenaTheme()
        {
            // Apply glowing Tron Cyber Grid texture to ArenaFloor
            GameObject floor = GameObject.Find("ArenaFloor");
            if (floor != null)
            {
                MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
                if (floorRenderer != null)
                {
                    Texture2D gridTex = GenerateTronGridTexture(128, new Color(0.02f, 0.03f, 0.06f), new Color(0.0f, 0.45f, 0.65f));
                    Material floorMat = new Material(floorRenderer.sharedMaterial != null ? floorRenderer.sharedMaterial : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));
                    floorMat.color = Color.white;
                    floorMat.mainTexture = gridTex;
                    floorMat.mainTextureScale = new Vector2(24f, 24f);
                    if (floorMat.HasProperty("_BaseMap"))
                    {
                        floorMat.SetTexture("_BaseMap", gridTex);
                        floorMat.SetTextureScale("_BaseMap", new Vector2(24f, 24f));
                    }
                    floorRenderer.material = floorMat;
                }
            }

            // Apply neon cyber border to ArenaWalls
            GameObject wallsRoot = GameObject.Find("ArenaWalls");
            if (wallsRoot != null)
            {
                MeshRenderer[] wallRenderers = wallsRoot.GetComponentsInChildren<MeshRenderer>();
                foreach (var wr in wallRenderers)
                {
                    if (wr != null)
                    {
                        Color wallNeon = new Color(0.05f, 0.35f, 0.65f);
                        wr.material.color = wallNeon;
                        if (wr.material.HasProperty("_EmissionColor"))
                        {
                            wr.material.EnableKeyword("_EMISSION");
                            wr.material.SetColor("_EmissionColor", wallNeon * 0.9f);
                        }
                    }
                }
            }
        }

        private Texture2D GenerateTronGridTexture(int size, Color bgColor, Color lineColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < 3 || x >= size - 3 || y < 3 || y >= size - 3);
                    bool isSubGrid = (Mathf.Abs(x - size / 2) < 1 || Mathf.Abs(y - size / 2) < 1);
                    if (isBorder)
                    {
                        pixels[y * size + x] = lineColor;
                    }
                    else if (isSubGrid)
                    {
                        pixels[y * size + x] = Color.Lerp(bgColor, lineColor, 0.3f);
                    }
                    else
                    {
                        pixels[y * size + x] = bgColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void SpawnInitialPowerUps()
        {
            int count = Mathf.Max(8, initialPowerUpCount);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomArenaPosition();
                PowerUpType type = GetRandomPowerUpType();

                GameObject obj = null;
                if (powerUpPrefab != null)
                {
                    obj = Instantiate(powerUpPrefab, pos, Quaternion.identity, transform);
                }
                else
                {
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = "PowerUp_" + i;
                    obj.transform.position = pos;
                    obj.transform.localScale = Vector3.one * 1.05f;
                    obj.transform.SetParent(transform);

                    BoxCollider col = obj.GetComponent<BoxCollider>();
                    if (col != null) col.isTrigger = true;

                    obj.AddComponent<PowerUpItem>();
                }

                PowerUpItem pItem = obj.GetComponent<PowerUpItem>();
                if (pItem != null)
                {
                    pItem.SetPowerUpType(type);
                    activePowerUps.Add(pItem);
                }
            }
        }

        public void RespawnAmmoOrb(AmmoOrb orb)
        {
            if (orb != null)
            {
                orb.gameObject.SetActive(false);
            }
        }

        public void RespawnPowerUp(PowerUpItem item)
        {
            StartCoroutine(RespawnPowerUpRoutine(item));
        }

        private IEnumerator RespawnPowerUpRoutine(PowerUpItem item)
        {
            item.gameObject.SetActive(false);
            yield return new WaitForSeconds(Random.Range(4f, 7f));

            Vector3 newPos = GetRandomArenaPosition();
            PowerUpType newType = GetRandomPowerUpType();
            item.Relocate(newPos, newType);
            item.gameObject.SetActive(true);
        }

        public Vector3 GetRandomArenaPosition()
        {
            return new Vector3(
                Random.Range(minX, maxX),
                0.65f,
                Random.Range(minZ, maxZ)
            );
        }

        private PowerUpType GetRandomPowerUpType()
        {
            // 1 = SpeedBoost, 2 = Shield, 3 = TrailBoost, 4 = EMP
            int p = Random.Range(1, 5);
            return (PowerUpType)p;
        }
    }
}
