using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace DiepMultiplayer
{
    public class OptionsPanelUI : MonoBehaviour
    {
        private static OptionsPanelUI _instance;
        public static OptionsPanelUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateOrFindInstance();
                }
                return _instance;
            }
        }

        [Header("Overlay References")]
        public Button lobbyOptionButton;
        public GameObject optionsBackdrop;
        public GameObject optionsPanel;
        public Button closeButton;
        public Button saveCloseButton;

        [Header("Audio Controls")]
        public Slider bgmSlider;
        public TMP_Text bgmValueText;
        public Slider sfxSlider;
        public TMP_Text sfxValueText;
        public Button muteButton;
        public TMP_Text muteButtonText;

        [Header("Display Controls")]
        public Button fullscreenButton;
        public TMP_Text fullscreenButtonText;

        private bool listenersBound = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            CreateOrFindInstance();
        }

        private static void CreateOrFindInstance()
        {
            if (_instance != null) return;

            OptionsPanelUI existing = FindFirstObjectByType<OptionsPanelUI>(FindObjectsInactive.Include);
            if (existing != null)
            {
                _instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            GameObject prefab = Resources.Load<GameObject>("OptionsOverlayPrefab");
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab);
                go.name = "OptionsOverlayCanvas";
                _instance = go.GetComponent<OptionsPanelUI>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            BindListeners();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshSceneMode(SceneManager.GetActiveScene().name);
            SyncUIFromSettings();
            CloseOptions();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CloseOptions();
            RefreshSceneMode(scene.name);
            SyncUIFromSettings();
        }

        private void RefreshSceneMode(string sceneName)
        {
            bool isGameScene = !string.IsNullOrEmpty(sceneName) && sceneName.Contains("Game");
            if (lobbyOptionButton != null)
            {
                // Di GameScene, tombol Option hanya muncul di panel Game Over saat mati!
                lobbyOptionButton.gameObject.SetActive(!isGameScene);
            }
        }

        private void BindListeners()
        {
            if (listenersBound) return;

            if (lobbyOptionButton != null)
            {
                lobbyOptionButton.onClick.RemoveAllListeners();
                lobbyOptionButton.onClick.AddListener(ToggleOptions);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (saveCloseButton != null)
            {
                saveCloseButton.onClick.RemoveAllListeners();
                saveCloseButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.RemoveAllListeners();
                bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            }

            if (muteButton != null)
            {
                muteButton.onClick.RemoveAllListeners();
                muteButton.onClick.AddListener(OnMuteToggled);
            }

            if (fullscreenButton != null)
            {
                fullscreenButton.onClick.RemoveAllListeners();
                fullscreenButton.onClick.AddListener(OnFullscreenToggled);
            }

            listenersBound = true;
        }

        public void OpenOptions()
        {
            BindListeners();
            SyncUIFromSettings();

            if (optionsBackdrop != null) optionsBackdrop.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(true);

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonClick();
            }
        }

        public void CloseOptions()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (optionsBackdrop != null) optionsBackdrop.SetActive(false);
        }

        private void OnCloseButtonClicked()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonClick();
            }
            CloseOptions();
        }

        public void ToggleOptions()
        {
            bool currentlyOpen = (optionsBackdrop != null && optionsBackdrop.activeSelf)
                              || (optionsPanel != null && optionsPanel.activeSelf);
            if (currentlyOpen)
            {
                CloseOptions();
            }
            else
            {
                OpenOptions();
            }
        }

        private void SyncUIFromSettings()
        {
            float bgm = SoundManager.Instance != null ? SoundManager.Instance.bgmVolume : PlayerPrefs.GetFloat("Tron_BGM_Volume", 0.45f);
            float sfx = SoundManager.Instance != null ? SoundManager.Instance.sfxVolume : PlayerPrefs.GetFloat("Tron_SFX_Volume", 0.8f);
            bool muted = SoundManager.Instance != null ? SoundManager.Instance.isMuted : (PlayerPrefs.GetInt("Tron_Audio_Muted", 0) == 1);

            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(bgm);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

            UpdateValueLabels(bgm, sfx, muted);
        }

        private void OnBgmChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetBgmVolume(value);
            }
            float sfx = sfxSlider != null ? sfxSlider.value : 0.8f;
            bool muted = SoundManager.Instance != null && SoundManager.Instance.isMuted;
            UpdateValueLabels(value, sfx, muted);
        }

        private void OnSfxChanged(float value)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSfxVolume(value);
            }
            float bgm = bgmSlider != null ? bgmSlider.value : 0.45f;
            bool muted = SoundManager.Instance != null && SoundManager.Instance.isMuted;
            UpdateValueLabels(bgm, value, muted);
        }

        private void OnMuteToggled()
        {
            bool nextMuted = SoundManager.Instance != null ? !SoundManager.Instance.isMuted : !(PlayerPrefs.GetInt("Tron_Audio_Muted", 0) == 1);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMuted(nextMuted);
                if (!nextMuted) SoundManager.Instance.PlayButtonClick();
            }
            float bgm = bgmSlider != null ? bgmSlider.value : 0.45f;
            float sfx = sfxSlider != null ? sfxSlider.value : 0.8f;
            UpdateValueLabels(bgm, sfx, nextMuted);
        }

        private void OnFullscreenToggled()
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonClick();
            }
            float bgm = bgmSlider != null ? bgmSlider.value : 0.45f;
            float sfx = sfxSlider != null ? sfxSlider.value : 0.8f;
            bool muted = SoundManager.Instance != null && SoundManager.Instance.isMuted;
            UpdateValueLabels(bgm, sfx, muted);
        }

        private void UpdateValueLabels(float bgm, float sfx, bool muted)
        {
            if (bgmValueText != null)
            {
                bgmValueText.text = Mathf.RoundToInt(bgm * 100f) + "%";
            }

            if (sfxValueText != null)
            {
                sfxValueText.text = Mathf.RoundToInt(sfx * 100f) + "%";
            }

            if (muteButtonText != null)
            {
                muteButtonText.text = muted ? "AUDIO: MUTE" : "AUDIO: AKTIF";
                muteButtonText.color = muted ? new Color(1f, 0.45f, 0.45f) : Color.white;
            }

            if (fullscreenButtonText != null)
            {
                fullscreenButtonText.text = Screen.fullScreen ? "LAYAR: FULLSCREEN" : "LAYAR: WINDOWED";
                fullscreenButtonText.color = Color.white;
            }
        }
    }
}
