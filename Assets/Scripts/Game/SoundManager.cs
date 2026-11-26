using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace DiepMultiplayer
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        public AudioSource shootSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float bgmVolume = 0.45f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool isMuted = false;

        private const string PrefBgmKey = "Tron_BGM_Volume";
        private const string PrefSfxKey = "Tron_SFX_Volume";
        private const string PrefMuteKey = "Tron_Audio_Muted";

        [Header("Audio Clips")]
        public AudioClip bgmLobbyClip;
        public AudioClip bgmGameClip;
        public AudioClip buttonClickClip;
        public AudioClip joinRoomClip;
        public AudioClip shootClip;
        public AudioClip shootBigClip;
        public AudioClip ammoPickupClip;
        public AudioClip powerUpClip;
        public AudioClip healClip;
        public AudioClip shieldClip;
        public AudioClip hitClip;
        public AudioClip explosionClip;
        public AudioClip gameOverClip;

        private HashSet<int> hookedButtons = new HashSet<int>();
        private float lastShootSoundTime = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance == null)
            {
                CreateInstance();
            }
        }

        private static void CreateInstance()
        {
            SoundManager existing = FindFirstObjectByType<SoundManager>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }

            GameObject go = new GameObject("SoundManager");
            _instance = go.AddComponent<SoundManager>();
            DontDestroyOnLoad(go);
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

            LoadAudioSettings();
            SetupAudioSources();
            LoadOrGenerateClips();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void LoadAudioSettings()
        {
            bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefBgmKey, 0.45f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxKey, 0.8f));
            isMuted = PlayerPrefs.GetInt(PrefMuteKey, 0) == 1;
        }

        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefBgmKey, bgmVolume);
            PlayerPrefs.Save();
            ApplyAudioVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PrefSfxKey, sfxVolume);
            PlayerPrefs.Save();
            ApplyAudioVolumes();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            PlayerPrefs.SetInt(PrefMuteKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioVolumes();
        }

        public void ApplyAudioVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.mute = isMuted;
                bgmSource.volume = isMuted ? 0f : bgmVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.mute = isMuted;
                sfxSource.volume = isMuted ? 0f : 1f;
            }
            if (shootSource != null)
            {
                shootSource.mute = isMuted;
                shootSource.volume = isMuted ? 0f : 0.75f;
            }
        }

        private void Start()
        {
            UpdateSceneAudio(SceneManager.GetActiveScene().name);
            HookAllSceneButtons();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = bgmVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume;
            }

            if (shootSource == null)
            {
                shootSource = gameObject.AddComponent<AudioSource>();
                shootSource.loop = false;
                shootSource.playOnAwake = false;
                shootSource.volume = sfxVolume * 0.7f;
            }

            ApplyAudioVolumes();
        }

        private void LoadOrGenerateClips()
        {
            bgmLobbyClip = LoadClipOrFallback("Audio/BGM_Lobby", GenerateLobbyBgmFallback);
            bgmGameClip = LoadClipOrFallback("Audio/BGM_Game", GenerateGameBgmFallback);
            buttonClickClip = LoadClipOrFallback("Audio/SFX_ButtonClick", () => GenerateToneClip("Click", 950f, 500f, 0.07f, 0.6f));
            joinRoomClip = LoadClipOrFallback("Audio/SFX_JoinRoom", () => GenerateToneClip("JoinRoom", 523f, 659f, 0.3f, 0.5f));
            shootClip = LoadClipOrFallback("Audio/SFX_Shoot", () => GenerateToneClip("Shoot", 420f, 95f, 0.13f, 0.65f, true));
            shootBigClip = LoadClipOrFallback("Audio/SFX_ShootBig", () => GenerateToneClip("ShootBig", 220f, 55f, 0.26f, 0.85f, true));
            ammoPickupClip = LoadClipOrFallback("Audio/SFX_AmmoPickup", () => GenerateToneClip("AmmoPickup", 880f, 1175f, 0.14f, 0.55f));
            powerUpClip = LoadClipOrFallback("Audio/SFX_PowerUp", () => GenerateArpeggioClip("PowerUp", new float[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.4f));
            healClip = LoadClipOrFallback("Audio/SFX_Heal", () => GenerateArpeggioClip("Heal", new float[] { 587.33f, 739.99f, 880f, 1174.66f }, 0.42f));
            shieldClip = LoadClipOrFallback("Audio/SFX_Shield", () => GenerateToneClip("Shield", 620f, 740f, 0.3f, 0.55f));
            hitClip = LoadClipOrFallback("Audio/SFX_Hit", () => GenerateToneClip("Hit", 180f, 70f, 0.09f, 0.65f, true));
            explosionClip = LoadClipOrFallback("Audio/SFX_Explosion", () => GenerateToneClip("Explosion", 110f, 30f, 0.5f, 0.85f, true));
            gameOverClip = LoadClipOrFallback("Audio/SFX_GameOver", () => GenerateArpeggioClip("GameOver", new float[] { 392f, 370f, 349.2f, 329.6f }, 0.8f));
        }

        private AudioClip LoadClipOrFallback(string resourcePath, System.Func<AudioClip> fallbackGenerator)
        {
            AudioClip clip = Resources.Load<AudioClip>(resourcePath);
            if (clip != null) return clip;
            return fallbackGenerator != null ? fallbackGenerator() : null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            hookedButtons.Clear();
            UpdateSceneAudio(scene.name);
            HookAllSceneButtons();
        }

        public void HookAllSceneButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn == null) continue;
                int id = btn.GetInstanceID();
                if (!hookedButtons.Contains(id))
                {
                    hookedButtons.Add(id);
                    btn.onClick.AddListener(PlayButtonClick);
                }
            }
        }

        public void UpdateSceneAudio(string sceneName)
        {
            if (bgmSource == null) return;

            AudioClip targetBgm = sceneName.Contains("Game") ? bgmGameClip : bgmLobbyClip;
            if (targetBgm != null && bgmSource.clip != targetBgm)
            {
                bgmSource.clip = targetBgm;
                ApplyAudioVolumes();
                bgmSource.Play();
            }
            else if (targetBgm != null && !bgmSource.isPlaying)
            {
                ApplyAudioVolumes();
                bgmSource.Play();
            }
        }

        // ==================== PUBLIC SFX API ====================

        public void PlayButtonClick()
        {
            if (sfxSource != null && buttonClickClip != null)
            {
                sfxSource.PlayOneShot(buttonClickClip, sfxVolume * 0.85f);
            }
        }

        public void PlayJoinRoom()
        {
            if (sfxSource != null && joinRoomClip != null)
            {
                sfxSource.PlayOneShot(joinRoomClip, sfxVolume * 0.9f);
            }
        }

        public void PlayShoot(bool isBig, Vector3 worldPos, bool isLocalPlayer = false)
        {
            if (shootSource == null) return;

            // Prevent overlapping audio clipping when multiple tanks fire on the exact same millisecond
            if (!isLocalPlayer && Time.unscaledTime - lastShootSoundTime < 0.04f)
                return;

            float atten = isLocalPlayer ? 1.0f : GetDistanceAttenuation(worldPos, 35f) * 0.45f;
            if (atten <= 0.02f) return;

            lastShootSoundTime = Time.unscaledTime;
            shootSource.pitch = Random.Range(0.92f, 1.08f);

            AudioClip clip = isBig ? shootBigClip : shootClip;
            if (clip != null)
            {
                shootSource.PlayOneShot(clip, sfxVolume * atten * (isBig ? 0.95f : 0.65f));
            }
        }

        public void PlayAmmoPickup(AmmoType type, Vector3 worldPos, bool isLocalPlayer = true)
        {
            if (sfxSource == null || ammoPickupClip == null) return;

            float atten = isLocalPlayer ? 1.0f : GetDistanceAttenuation(worldPos, 25f) * 0.35f;
            if (atten <= 0.02f) return;

            float pitch = 1.0f;
            switch (type)
            {
                case AmmoType.Blue: pitch = 0.95f; break;
                case AmmoType.Green: pitch = 1.06f; break;
                case AmmoType.Yellow: pitch = 1.18f; break;
                case AmmoType.Orange: pitch = 1.30f; break;
                case AmmoType.Red: pitch = 1.44f; break;
            }

            PlayPitchShiftedOneShot(ammoPickupClip, sfxVolume * 0.75f * atten, pitch);
        }

        public void PlayPowerUp(PowerUpType type, Vector3 worldPos, bool isLocalPlayer = true)
        {
            if (sfxSource == null) return;

            float atten = isLocalPlayer ? 1.0f : GetDistanceAttenuation(worldPos, 30f) * 0.5f;
            if (atten <= 0.02f) return;

            AudioClip clip = powerUpClip;
            if (type == PowerUpType.EMP) clip = shootBigClip != null ? shootBigClip : healClip;
            else if (type == PowerUpType.Shield) clip = shieldClip;
            else if (type == PowerUpType.TrailBoost) clip = healClip;

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * 0.95f * atten);
            }
        }

        public void PlayHit(Vector3 worldPos)
        {
            if (sfxSource == null || hitClip == null) return;

            float atten = GetDistanceAttenuation(worldPos, 32f);
            if (atten <= 0.02f) return;

            sfxSource.PlayOneShot(hitClip, sfxVolume * 0.75f * atten);
        }

        public void PlayShieldBlock(Vector3 worldPos)
        {
            if (sfxSource == null || shieldClip == null) return;

            float atten = GetDistanceAttenuation(worldPos, 32f);
            if (atten <= 0.02f) return;

            PlayPitchShiftedOneShot(shieldClip, sfxVolume * 0.75f * atten, 1.35f);
        }

        public void PlayExplosion(Vector3 worldPos)
        {
            if (sfxSource == null || explosionClip == null) return;

            float atten = Mathf.Max(0.35f, GetDistanceAttenuation(worldPos, 45f));
            sfxSource.PlayOneShot(explosionClip, sfxVolume * atten);
        }

        public void PlayGameOver()
        {
            if (sfxSource != null && gameOverClip != null)
            {
                sfxSource.PlayOneShot(gameOverClip, sfxVolume);
            }
        }

        private float GetDistanceAttenuation(Vector3 worldPos, float maxDist)
        {
            if (GameManager.Instance != null && GameManager.Instance.localPlayerTank != null)
            {
                float d = Vector3.Distance(GameManager.Instance.localPlayerTank.transform.position, worldPos);
                return Mathf.Clamp01(1f - (d / maxDist));
            }
            if (Camera.main != null)
            {
                Vector3 camGround = Camera.main.transform.position;
                camGround.y = worldPos.y;
                float d = Vector3.Distance(camGround, worldPos);
                return Mathf.Clamp01(1f - (d / maxDist));
            }
            return 1f;
        }

        private void PlayPitchShiftedOneShot(AudioClip clip, float volume, float pitch)
        {
            if (clip == null) return;
            GameObject tempGO = new GameObject("TempSFX");
            AudioSource aSource = tempGO.AddComponent<AudioSource>();
            aSource.clip = clip;
            aSource.volume = volume;
            aSource.pitch = pitch;
            aSource.Play();
            Destroy(tempGO, (clip.length / Mathf.Max(0.1f, pitch)) + 0.05f);
        }

        // ==================== PROCEDURAL FALLBACK GENERATORS ====================

        private AudioClip GenerateToneClip(string name, float startFreq, float endFreq, float duration, float gain, bool addNoise = false)
        {
            int sampleRate = 44100;
            int len = Mathf.Max(1, (int)(sampleRate * duration));
            float[] data = new float[len];
            float phase = 0f;
            System.Random rand = new System.Random(name.GetHashCode());

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float env = Mathf.Exp(-t * 5f);
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / sampleRate;
                float s = Mathf.Sin(2f * Mathf.PI * phase);
                if (addNoise)
                {
                    float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.35f;
                    s = s * 0.7f + noise;
                }
                data[i] = Mathf.Clamp(s * env * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateArpeggioClip(string name, float[] notes, float duration)
        {
            int sampleRate = 44100;
            int len = Mathf.Max(1, (int)(sampleRate * duration));
            float[] data = new float[len];
            float stepDur = duration / notes.Length;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                int idx = Mathf.Min(notes.Length - 1, (int)(t / stepDur));
                float localT = t - idx * stepDur;
                float env = Mathf.Exp(-localT * 8f);
                float freq = notes[idx];
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * freq * t);
                data[i] = Mathf.Clamp(s * env * 0.45f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateLobbyBgmFallback()
        {
            int sampleRate = 44100;
            float duration = 8.0f;
            int len = (int)(sampleRate * duration);
            float[] data = new float[len];
            float[] arpNotes = {
                220.00f, 261.63f, 329.63f, 392.00f, 329.63f, 261.63f, 329.63f, 392.00f,
                174.61f, 220.00f, 261.63f, 329.63f, 261.63f, 220.00f, 261.63f, 329.63f,
                261.63f, 329.63f, 392.00f, 493.88f, 392.00f, 329.63f, 392.00f, 493.88f,
                196.00f, 246.94f, 293.66f, 392.00f, 293.66f, 246.94f, 293.66f, 329.63f
            };
            float[] bassNotes = { 110.00f, 87.31f, 130.81f, 98.00f };
            float stepDur = duration / arpNotes.Length;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                int step = Mathf.Min(arpNotes.Length - 1, (int)(t / stepDur));
                float localT = t - step * stepDur;
                float arpFreq = arpNotes[step];
                float arpEnv = Mathf.Exp(-localT * 7f);

                int bassStep = Mathf.Min(3, (int)(t / 2.0f));
                float bassFreq = bassNotes[bassStep];

                float arp = (Mathf.Sin(2f * Mathf.PI * arpFreq * t) + 0.25f * Mathf.Sin(4f * Mathf.PI * arpFreq * t)) * arpEnv * 0.22f;
                float pad = Mathf.Sin(2f * Mathf.PI * bassFreq * 2f * t) * 0.08f;
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.16f;
                data[i] = Mathf.Clamp(arp + pad + bass, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("BGM_Lobby", len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateGameBgmFallback()
        {
            int sampleRate = 44100;
            float duration = 6.4f;
            int len = (int)(sampleRate * duration);
            float[] data = new float[len];
            float[] leadNotes = {
                329.63f, 329.63f, 392.00f, 440.00f, 493.88f, 440.00f, 392.00f, 329.63f,
                293.66f, 293.66f, 329.63f, 392.00f, 440.00f, 392.00f, 329.63f, 293.66f,
                261.63f, 261.63f, 329.63f, 392.00f, 440.00f, 523.25f, 493.88f, 392.00f,
                293.66f, 329.63f, 392.00f, 440.00f, 493.88f, 440.00f, 392.00f, 293.66f
            };
            float[] bassNotes = { 82.41f, 73.42f, 65.41f, 73.42f };
            float stepDur = duration / leadNotes.Length;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                int step = Mathf.Min(leadNotes.Length - 1, (int)(t / stepDur));
                float localT = t - step * stepDur;
                float leadFreq = leadNotes[step];
                float leadEnv = Mathf.Exp(-localT * 9f);

                int bassStep = Mathf.Min(3, (int)(t / (duration / 4f)));
                float bassFreq = bassNotes[bassStep];
                float bassEnv = Mathf.Exp(-(localT % 0.2f) * 11f);

                float kickT = t % 0.4f;
                float kick = Mathf.Sin(2f * Mathf.PI * (120f * Mathf.Exp(-kickT * 30f) + 40f) * kickT) * Mathf.Exp(-kickT * 18f) * 0.22f;

                float lead = (Mathf.Sin(2f * Mathf.PI * leadFreq * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * leadFreq * t)) * leadEnv * 0.18f;
                float bass = (Mathf.Sin(2f * Mathf.PI * bassFreq * t) + 0.4f * Mathf.Sin(4f * Mathf.PI * bassFreq * t)) * bassEnv * 0.22f;

                data[i] = Mathf.Clamp(lead + bass + kick, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("BGM_Game", len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
