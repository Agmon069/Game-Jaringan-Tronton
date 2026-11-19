using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace DiepMultiplayer
{
    public class LeaderboardEntry
    {
        public string name;
        public int kills;
        public bool isLocal;
    }

    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("UI Reference")]
        public TMP_Text leaderboardText;

        private float refreshTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ApplyLargerRankingLayout();
            RefreshLeaderboard();
        }

        private void ApplyLargerRankingLayout()
        {
            if (leaderboardText == null) return;

            leaderboardText.fontSize = 22f;
            leaderboardText.lineSpacing = 10f;

            RectTransform txtRT = leaderboardText.GetComponent<RectTransform>();
            if (txtRT != null)
            {
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.offsetMin = new Vector2(18f, 16f);
                txtRT.offsetMax = new Vector2(-18f, -16f);
            }

            if (leaderboardText.transform.parent != null)
            {
                RectTransform panelRT = leaderboardText.transform.parent.GetComponent<RectTransform>();
                if (panelRT != null)
                {
                    panelRT.anchorMin = new Vector2(1f, 1f);
                    panelRT.anchorMax = new Vector2(1f, 1f);
                    panelRT.pivot = new Vector2(1f, 1f);
                    panelRT.anchoredPosition = new Vector2(-24f, -24f);
                    panelRT.sizeDelta = new Vector2(390f, 300f);
                }
            }
        }

        private void Update()
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= 0.5f)
            {
                refreshTimer = 0f;
                RefreshLeaderboard();
            }
        }

        public void RefreshLeaderboard()
        {
            if (leaderboardText == null) return;

            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            // 1. Gather all real players in Photon room
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    int kills = 0;
                    if (player.CustomProperties.TryGetValue("Kills", out object kVal))
                    {
                        kills = (int)kVal;
                    }

                    entries.Add(new LeaderboardEntry
                    {
                        name = string.IsNullOrEmpty(player.NickName) ? "Player" : player.NickName,
                        kills = kills,
                        isLocal = (PhotonNetwork.LocalPlayer != null && player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                    });
                }
            }
            else if (GameManager.Instance != null && GameManager.Instance.localPlayerTank != null)
            {
                entries.Add(new LeaderboardEntry
                {
                    name = GameManager.Instance.localPlayerTank.tankName,
                    kills = GameManager.Instance.localPlayerTank.kills,
                    isLocal = true
                });
            }

            // 2. Gather all bots in arena
            BotController[] bots = FindObjectsByType<BotController>(FindObjectsSortMode.None);
            foreach (BotController bot in bots)
            {
                entries.Add(new LeaderboardEntry
                {
                    name = bot.tankName,
                    kills = bot.kills,
                    isLocal = false
                });
            }

            // Sort descending by kills
            var sortedEntries = entries.OrderByDescending(e => e.kills).Take(6).ToList();

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<b><color=#FFD700>--- RANKING (KILLS) ---</color></b>");

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];
                string colorTag = entry.isLocal ? "<color=#00FF66>" : "<color=#FFFFFF>";
                string rankNumber = (i + 1) + ". ";
                sb.AppendLine($"{colorTag}{rankNumber}{entry.name} : {entry.kills} Kills</color>");
            }

            leaderboardText.text = sb.ToString();
        }
    }
}
