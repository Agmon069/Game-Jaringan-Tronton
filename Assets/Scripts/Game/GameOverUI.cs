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
        public Button optionButton;

        private bool buttonsInitialized = false;

        private void Awake()
        {
            InitButtons();
        }

        private void InitButtons()
        {
            if (buttonsInitialized) return;

            if (respawnButton != null)
            {
                respawnButton.onClick.RemoveAllListeners();
                respawnButton.onClick.AddListener(OnRestartClicked);
                EnsureWhiteLabel(respawnButton, "RESTART");
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveAllListeners();
                exitButton.onClick.AddListener(OnExitClicked);
                EnsureWhiteLabel(exitButton, "MAIN MENU");
            }

            if (optionButton != null)
            {
                optionButton.onClick.RemoveAllListeners();
                optionButton.onClick.AddListener(OnOptionClicked);
                EnsureWhiteLabel(optionButton, "OPTION");
            }

            buttonsInitialized = true;
        }

        private void EnsureWhiteLabel(Button btn, string defaultText)
        {
            if (btn == null) return;
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>(true);
            if (btnText != null)
            {
                if (string.IsNullOrEmpty(btnText.text))
                {
                    btnText.text = defaultText;
                }
                btnText.color = Color.white;
            }
        }

        public void Show(string killerName, int kills)
        {
            InitButtons();

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
            if (OptionsPanelUI.Instance != null)
            {
                OptionsPanelUI.Instance.CloseOptions();
            }

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

        private void OnOptionClicked()
        {
            if (OptionsPanelUI.Instance != null)
            {
                OptionsPanelUI.Instance.OpenOptions();
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
