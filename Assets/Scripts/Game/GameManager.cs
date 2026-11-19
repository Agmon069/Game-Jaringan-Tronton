using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace DiepMultiplayer
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        public static GameManager Instance { get; private set; }

        [Header("Spawn Settings")]
        public float spawnAreaRadius = 21f;
        public int maxCombatants = 6;

        [Header("HUD References")]
        public Slider hudHealthSlider;
        public TMP_Text hudHealthText;
        public TMP_Text hudAmmoText;
        public GameObject hudPowerUpPanel;
        public TMP_Text hudPowerUpNameText;
        public Slider hudPowerUpTimerSlider;

        [Header("Game Over UI")]
        public GameOverUI gameOverUI;

        [Header("Local Player")]
        public PlayerTankController localPlayerTank;

        private List<string> botNamesPool = new List<string>
        {
            "Tron Alpha", "Tron Bravo", "Tron Charlie", "Tron Delta", "Tron Echo", "Tron Foxtrot"
        };
        private int botSpawnCounter = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Spawn local player Light Cycle
            SpawnLocalPlayer();

            // If Master Client (or offline testing), fill remaining slots with Bots
            if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
            {
                SpawnBotsToFillArena();
            }
        }

        public void SpawnLocalPlayer()
        {
            Vector3 spawnPos = GetRandomSpawnPosition();

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                GameObject playerObj = PhotonNetwork.Instantiate("PlayerTank", spawnPos, Quaternion.identity);
                localPlayerTank = playerObj.GetComponent<PlayerTankController>();
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>("PlayerTank");
                if (prefab != null)
                {
                    GameObject p = Instantiate(prefab, spawnPos, Quaternion.identity);
                    localPlayerTank = p.GetComponent<PlayerTankController>();
                }
            }
        }

        public void SpawnBotsToFillArena()
        {
            int currentPlayers = (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null)
                ? PhotonNetwork.CurrentRoom.PlayerCount
                : 1;
            int botsNeeded = maxCombatants - currentPlayers;

            if (botsNeeded < 0) botsNeeded = 0;

            for (int i = 0; i < botsNeeded; i++)
            {
                SpawnSingleBot();
            }
        }

        public void SpawnSingleBot()
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            string bName = botSpawnCounter < botNamesPool.Count ? botNamesPool[botSpawnCounter] : ("Tron Bot #" + (botSpawnCounter + 1));
            int botId = 1000 + botSpawnCounter;
            botSpawnCounter++;

            GameObject botObj = null;
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                botObj = PhotonNetwork.Instantiate("BotTank", spawnPos, Quaternion.identity);
            }
            else
            {
                GameObject prefab = Resources.Load<GameObject>("BotTank");
                if (prefab != null)
                {
                    botObj = Instantiate(prefab, spawnPos, Quaternion.identity);
                }
            }

            if (botObj != null)
            {
                BotController botCtrl = botObj.GetComponent<BotController>();
                if (botCtrl != null)
                {
                    botCtrl.InitializeBot(bName, botId);
                }
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Player {otherPlayer.NickName} keluar room. Master Client menggantikannya dengan bot baru.");

            if (PhotonNetwork.IsMasterClient)
            {
                SpawnSingleBot();
            }

            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.RefreshLeaderboard();
            }
        }

        public void AddBotKill(int botActorNumber)
        {
            BotController[] bots = FindObjectsByType<BotController>(FindObjectsSortMode.None);
            foreach (var b in bots)
            {
                if (b.actorNumber == botActorNumber)
                {
                    b.kills++;
                    break;
                }
            }
        }

        public void RespawnBotAfterDelay(BotController bot, float delay = 3.0f)
        {
            StartCoroutine(RespawnBotRoutine(bot, delay));
        }

        private IEnumerator RespawnBotRoutine(BotController bot, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (bot != null)
            {
                if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && bot.photonView != null)
                {
                    bot.photonView.RPC(nameof(BotController.RPC_RespawnBot), RpcTarget.All);
                }
                else
                {
                    bot.RPC_RespawnBot();
                }
            }
        }

        public void UpdateHUD(int health, int maxHealth, int currentKills, PowerUpType activePowerUp, float powerUpTimer)
        {
            if (hudHealthSlider != null)
            {
                hudHealthSlider.maxValue = maxHealth;
                hudHealthSlider.value = health > 0 ? maxHealth : 0;
            }

            if (hudHealthText != null)
            {
                if (health <= 0)
                {
                    hudHealthText.text = "STATUS: HANCUR";
                }
                else
                {
                    switch (activePowerUp)
                    {
                        case PowerUpType.Shield:
                            hudHealthText.text = "STATUS: SHIELD (TEMBUS JEJAK)";
                            break;
                        case PowerUpType.SpeedBoost:
                            hudHealthText.text = "STATUS: TURBO SPEED";
                            break;
                        case PowerUpType.TrailBoost:
                            hudHealthText.text = "STATUS: TRAIL BOOST";
                            break;
                        case PowerUpType.EMP:
                            hudHealthText.text = "STATUS: EMP BLAST!";
                            break;
                        default:
                            hudHealthText.text = "STATUS: LIGHT CYCLE AKTIF";
                            break;
                    }
                }
            }

            if (hudAmmoText != null)
            {
                string trailDur = (activePowerUp == PowerUpType.TrailBoost) ? "9.0s (TEBAL)" : "4.5s";
                hudAmmoText.text = $"KILLS: {currentKills}  |  JEJAK: {trailDur}";
            }

            if (hudPowerUpPanel != null)
            {
                if (activePowerUp != PowerUpType.None)
                {
                    hudPowerUpPanel.SetActive(true);
                    if (hudPowerUpNameText != null)
                    {
                        hudPowerUpNameText.text = GetPowerUpDisplayName(activePowerUp);
                    }
                    if (hudPowerUpTimerSlider != null)
                    {
                        float maxDur = 6f;
                        if (activePowerUp == PowerUpType.Shield) maxDur = 4f;
                        else if (activePowerUp == PowerUpType.TrailBoost) maxDur = 7f;
                        else if (activePowerUp == PowerUpType.EMP) maxDur = 2f;

                        hudPowerUpTimerSlider.maxValue = maxDur;
                        hudPowerUpTimerSlider.value = powerUpTimer;
                    }
                }
                else
                {
                    hudPowerUpPanel.SetActive(false);
                }
            }
        }

        private string GetPowerUpDisplayName(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.SpeedBoost: return "SPEED BOOST (TURBO)";
                case PowerUpType.Shield: return "SHIELD (TEMBUS JEJAK)";
                case PowerUpType.TrailBoost: return "TRAIL BOOST (JEJAK PANJANG)";
                case PowerUpType.EMP: return "EMP BLAST (HAPUS JEJAK)";
                default: return type.ToString();
            }
        }

        public void OnLocalPlayerDied(string killerName)
        {
            int myKills = localPlayerTank != null ? localPlayerTank.kills : 0;
            if (gameOverUI != null)
            {
                gameOverUI.Show(killerName, myKills);
            }
        }

        public void RespawnLocalPlayer()
        {
            if (localPlayerTank != null)
            {
                localPlayerTank.Respawn();
            }
        }

        public void LeaveGameToLobby()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                SceneManager.LoadScene("Lobby Scene");
            }
        }

        public override void OnLeftRoom()
        {
            SceneManager.LoadScene("Lobby Scene");
        }

        public Vector3 GetRandomSpawnPosition()
        {
            float safeRadius = Mathf.Min(spawnAreaRadius, 22f);
            TronTrailSegment[] segs = FindObjectsByType<TronTrailSegment>(FindObjectsSortMode.None);

            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(-safeRadius, safeRadius),
                    0.5f,
                    Random.Range(-safeRadius, safeRadius)
                );

                bool tooCloseToTrail = false;
                foreach (var s in segs)
                {
                    if (s != null && Vector3.Distance(candidate, s.transform.position) < 4.5f)
                    {
                        tooCloseToTrail = true;
                        break;
                    }
                }

                if (!tooCloseToTrail)
                {
                    return candidate;
                }
            }

            return new Vector3(
                Random.Range(-safeRadius, safeRadius),
                0.5f,
                Random.Range(-safeRadius, safeRadius)
            );
        }
    }
}
