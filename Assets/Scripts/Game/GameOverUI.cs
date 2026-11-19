using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiepMultiplayer
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject panel;
        public TMP_Text titleText;
        public TMP_Text killerText;
        public TMP_Text statsText;
        public Button respawnButton;
        public Button exitButton;

        private bool buttonsInitialized = false;

        private void Awake()
        {
            InitButtons();
            ApplyLargerModalAndButtonStyling();
        }

        private void InitButtons()
        {
            if (buttonsInitialized) return;

            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveAllListeners();
                respawnButton.onClick.AddListener(OnRestartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
            }

            buttonsInitialized = true;
        }

        private void ApplyLargerModalAndButtonStyling()
        {
            GameObject targetPanel = panel != null ? panel : gameObject;
            RectTransform panelRT = targetPanel.GetComponent<RectTransform>();
            if (panelRT != null)
            {
                panelRT.sizeDelta = new Vector2(580f, 440f);
                panelRT.anchoredPosition = Vector2.zero;
            }

            if (titleText != null)
            {
                titleText.fontSize = 48f;
                RectTransform rt = titleText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 145f);
                    rt.sizeDelta = new Vector2(520f, 65f);
                }
            }

            if (killerText != null)
            {
                killerText.fontSize = 26f;
                killerText.color = Color.white;
                RectTransform rt = killerText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 70f);
                    rt.sizeDelta = new Vector2(520f, 45f);
                }
            }

            if (statsText != null)
            {
                statsText.fontSize = 24f;
                RectTransform rt = statsText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(0f, 20f);
                    rt.sizeDelta = new Vector2(520f, 40f);
                }
            }

            ConfigureButtonAndWhiteText(respawnButton, new Vector2(0f, -55f), new Vector2(380f, 58f), "RESTART", 24f);
            ConfigureButtonAndWhiteText(exitButton, new Vector2(0f, -130f), new Vector2(380f, 58f), "KEMBALI KE MAIN MENU", 22f);
        }

        private void ConfigureButtonAndWhiteText(Button btn, Vector2 anchoredPos, Vector2 size, string label, float fontSize)
        {
            if (btn == null) return;

            RectTransform btnRT = btn.GetComponent<RectTransform>();
            if (btnRT != null)
            {
                btnRT.anchorMin = new Vector2(0.5f, 0.5f);
                btnRT.anchorMax = new Vector2(0.5f, 0.5f);
                btnRT.pivot = new Vector2(0.5f, 0.5f);
                btnRT.anchoredPosition = anchoredPos;
                btnRT.sizeDelta = size;
            }

            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>(true);
            if (btnText == null)
            {
                GameObject txtObj = new GameObject("ButtonLabel", typeof(RectTransform));
                txtObj.transform.SetParent(btn.transform, false);
                btnText = txtObj.AddComponent<TextMeshProUGUI>();
            }

            btnText.gameObject.SetActive(true);
            btnText.text = label;
            btnText.color = Color.white;
            btnText.fontSize = fontSize;
            btnText.fontStyle = FontStyles.Bold;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.textWrappingMode = TextWrappingModes.NoWrap;
            btnText.overflowMode = TextOverflowModes.Overflow;

            RectTransform txtRT = btnText.GetComponent<RectTransform>();
            if (txtRT != null)
            {
                txtRT.anchorMin = Vector2.zero;
                txtRT.anchorMax = Vector2.one;
                txtRT.pivot = new Vector2(0.5f, 0.5f);
                txtRT.localScale = Vector3.one;
                txtRT.localPosition = Vector3.zero;
                txtRT.anchoredPosition = Vector2.zero;
                txtRT.sizeDelta = Vector2.zero;
                txtRT.offsetMin = Vector2.zero;
                txtRT.offsetMax = Vector2.zero;
            }
        }

        public void Show(string killerName, int kills)
        {
            InitButtons();
            ApplyLargerModalAndButtonStyling();

            if (panel != null) panel.SetActive(true);
            else gameObject.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "GAME OVER";
            }

            if (killerText != null)
            {
                killerText.text = "Dikalahkan oleh: <color=#FF4444>" + killerName + "</color>";
            }

            if (statsText != null)
            {
                statsText.text = "Total Kills: <color=#FFD700>" + kills + "</color>";
            }
        }

        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
            else gameObject.SetActive(false);
        }

        private void OnRestartClicked()
        {
            Hide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RespawnLocalPlayer();
            }
        }

        private void OnExitClicked()
        {
            Hide();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LeaveGameToLobby();
            }
        }
    }
}
