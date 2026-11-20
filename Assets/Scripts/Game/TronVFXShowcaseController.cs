using UnityEngine;
using UnityEngine.InputSystem;

namespace DiepMultiplayer
{
    public class TronVFXShowcaseController : MonoBehaviour
    {
        [System.Serializable]
        public struct NeonPreset
        {
            public string label;
            public Color color;
        }

        [System.Serializable]
        public class CrashTestLane
        {
            public string laneName = "Lane";
            public GameObject carVisual;
            public TronExplosionVFX crashVfx;
            public Color neonColor = new Color(0.0f, 0.95f, 1.0f);
            public float explosionScale = 1.0f;
            public Vector3 startPos = new Vector3(0f, 0f, -9.5f);
            public Vector3 crashPos = new Vector3(0f, 0f, 1.6f);
            public float driveSpeed = 16f;
            public float respawnDelay = 1.6f;
            public float initialDelay = 0f;

            [System.NonSerialized] public bool isCrashed;
            [System.NonSerialized] public float timer;
            [System.NonSerialized] public bool hasStarted;
        }

        [Header("Crash Test Lanes")]
        public CrashTestLane[] crashLanes;
        public TronExplosionVFX[] stageExplosions;
        public bool autoLoop = true;

        [Header("Camera Shake")]
        public Camera targetCamera;
        public float shakeDecay = 2.6f;

        public static readonly NeonPreset[] Presets = new NeonPreset[]
        {
            new NeonPreset { label = "1: Tron Cyan",          color = new Color(0.0f, 0.95f, 1.0f) },
            new NeonPreset { label = "2: Rinzler Orange-Red", color = new Color(1.0f, 0.32f, 0.0f) },
            new NeonPreset { label = "3: Neon Magenta",       color = new Color(1.0f, 0.08f, 0.68f) },
            new NeonPreset { label = "4: Matrix Lime",        color = new Color(0.15f, 1.0f, 0.25f) },
            new NeonPreset { label = "5: Siren Gold",         color = new Color(1.0f, 0.85f, 0.05f) },
            new NeonPreset { label = "6: Electric Purple",    color = new Color(0.65f, 0.25f, 1.0f) }
        };

        private int _activePresetIndex = 0;
        private float _shakeAmount = 0f;
        private Vector3 _cameraOriginalPos;
        private bool _hasCameraBasePos;
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
            if (targetCamera != null)
            {
                _cameraOriginalPos = targetCamera.transform.position;
                _hasCameraBasePos = true;
            }

            if (crashLanes != null)
            {
                for (int i = 0; i < crashLanes.Length; i++)
                {
                    CrashTestLane lane = crashLanes[i];
                    if (lane == null) continue;

                    if (lane.crashVfx != null)
                    {
                        lane.crashVfx.loopInShowcase = false;
                        lane.crashVfx.playOnStart = false;
                        lane.crashVfx.playSound = false;
                    }

                    if (lane.carVisual != null)
                    {
                        lane.carVisual.transform.position = lane.startPos;
                        lane.carVisual.SetActive(lane.initialDelay <= 0.01f);
                    }

                    lane.isCrashed = false;
                    lane.hasStarted = lane.initialDelay <= 0.01f;
                    lane.timer = lane.initialDelay;
                }
            }

            if (stageExplosions != null)
            {
                for (int i = 0; i < stageExplosions.Length; i++)
                {
                    if (stageExplosions[i] != null)
                    {
                        stageExplosions[i].loopInShowcase = false;
                        stageExplosions[i].playOnStart = false;
                        stageExplosions[i].playSound = false;
                    }
                }
            }
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseInput();
            UpdateCrashTestLanes();
            UpdateCameraShake();
        }

        private void HandleKeyboardInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                Color c = Presets[_activePresetIndex].color;
                CrashCenterOrSpawn(c, 1.35f, true);
                AddCameraShake(0.45f);
            }

            if (kb.lKey.wasPressedThisFrame)
            {
                autoLoop = !autoLoop;
            }

            int selectedPreset = -1;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) selectedPreset = 0;
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) selectedPreset = 1;
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) selectedPreset = 2;
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) selectedPreset = 3;
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) selectedPreset = 4;
            else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) selectedPreset = 5;

            if (selectedPreset >= 0 && selectedPreset < Presets.Length)
            {
                _activePresetIndex = selectedPreset;
                Color c = Presets[_activePresetIndex].color;
                CrashCenterOrSpawn(c, 1.15f, true);
                AddCameraShake(0.30f);
            }
        }

        private void HandleMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || targetCamera == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Ray ray = targetCamera.ScreenPointToRay(screenPos);

                // First check if cursor clicked on one of the driving 3D cars
                CrashTestLane clickedLane = FindLaneCarHitByRay(ray);
                if (clickedLane != null)
                {
                    TriggerLaneCrash(clickedLane, Presets[_activePresetIndex].color, clickedLane.explosionScale, true);
                    AddCameraShake(0.35f);
                    return;
                }

                // Otherwise spawn crash-shatter explosion at cursor ground point
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter) && enter > 0f && enter < 120f)
                {
                    Vector3 hitPos = ray.GetPoint(enter);
                    hitPos.y = 0.45f;
                    Color c = Presets[_activePresetIndex].color;
                    TronExplosionVFX.SpawnCrash(hitPos, Vector3.forward, c, 1.05f, true);
                    AddCameraShake(0.25f);
                }
            }
        }

        private CrashTestLane FindLaneCarHitByRay(Ray ray)
        {
            if (crashLanes == null) return null;

            CrashTestLane bestLane = null;
            float bestDist = 1.9f;

            for (int i = 0; i < crashLanes.Length; i++)
            {
                CrashTestLane lane = crashLanes[i];
                if (lane == null || lane.carVisual == null || !lane.carVisual.activeInHierarchy) continue;

                Vector3 carCenter = lane.carVisual.transform.position + Vector3.up * 0.4f;
                float distToRay = Vector3.Cross(ray.direction, carCenter - ray.origin).magnitude;
                if (distToRay < bestDist)
                {
                    bestDist = distToRay;
                    bestLane = lane;
                }
            }

            return bestLane;
        }

        private void CrashCenterOrSpawn(Color color, float scale, bool playAudio)
        {
            if (crashLanes != null && crashLanes.Length > 0 && crashLanes[0] != null)
            {
                CrashTestLane center = crashLanes[0];
                if (center.carVisual != null && center.carVisual.activeInHierarchy)
                {
                    TriggerLaneCrash(center, color, scale, playAudio);
                    return;
                }
            }

            TronExplosionVFX.SpawnCrash(new Vector3(0f, 0.45f, 1.6f), Vector3.forward, color, scale, playAudio);
        }

        private void UpdateCrashTestLanes()
        {
            if (crashLanes == null) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < crashLanes.Length; i++)
            {
                CrashTestLane lane = crashLanes[i];
                if (lane == null || lane.carVisual == null) continue;

                if (!lane.hasStarted)
                {
                    if (!autoLoop) continue;
                    lane.timer -= dt;
                    if (lane.timer <= 0f)
                    {
                        lane.hasStarted = true;
                        lane.carVisual.transform.position = lane.startPos;
                        lane.carVisual.SetActive(true);
                    }
                    continue;
                }

                if (lane.isCrashed)
                {
                    if (!autoLoop) continue;
                    lane.timer -= dt;
                    if (lane.timer <= 0f)
                    {
                        lane.isCrashed = false;
                        lane.carVisual.transform.position = lane.startPos;
                        lane.carVisual.SetActive(true);
                    }
                    continue;
                }

                if (!autoLoop) continue;

                Vector3 pos = lane.carVisual.transform.position;
                Vector3 dir = (lane.crashPos - lane.startPos).normalized;
                pos += dir * (lane.driveSpeed * dt);
                lane.carVisual.transform.position = pos;

                if (Vector3.Dot(lane.crashPos - pos, dir) <= 0f)
                {
                    lane.carVisual.transform.position = lane.crashPos;
                    TriggerLaneCrash(lane, lane.neonColor, lane.explosionScale, false);
                    AddCameraShake(0.22f);
                }
            }
        }

        private void TriggerLaneCrash(CrashTestLane lane, Color color, float scale, bool playAudio)
        {
            Vector3 impactPos = lane.carVisual != null ? lane.carVisual.transform.position : lane.crashPos;
            impactPos.y = 0.45f;
            Vector3 crashDir = (lane.crashPos - lane.startPos).normalized;
            if (crashDir.sqrMagnitude < 0.001f) crashDir = Vector3.forward;

            if (lane.carVisual != null)
            {
                lane.carVisual.SetActive(false);
            }

            if (lane.crashVfx != null)
            {
                lane.crashVfx.transform.position = impactPos;
                lane.crashVfx.TriggerCrashExplosion(crashDir, color, scale, playAudio);
            }
            else
            {
                TronExplosionVFX.SpawnCrash(impactPos, crashDir, color, scale, playAudio);
            }

            lane.isCrashed = true;
            lane.timer = lane.respawnDelay;
        }

        private void UpdateCameraShake()
        {
            if (!_hasCameraBasePos || targetCamera == null) return;

            if (_shakeAmount > 0.001f)
            {
                Vector3 offset = Random.insideUnitSphere * _shakeAmount;
                offset.z *= 0.35f;
                targetCamera.transform.position = _cameraOriginalPos + offset;
                _shakeAmount = Mathf.Max(0f, _shakeAmount - shakeDecay * Time.deltaTime * _shakeAmount);
                if (_shakeAmount < 0.005f) _shakeAmount = 0f;
            }
            else
            {
                targetCamera.transform.position = _cameraOriginalPos;
            }
        }

        public void AddCameraShake(float amount)
        {
            _shakeAmount = Mathf.Clamp(_shakeAmount + amount, 0f, 0.75f);
        }

        private void OnGUI()
        {
            EnsureGUIStyles();

            NeonPreset active = Presets[_activePresetIndex];
            GUILayout.BeginArea(new Rect(18f, 18f, 360f, 195f), _boxStyle);
            GUILayout.Label("TRON CAR CRASH & SHATTER VFX", _titleStyle);
            GUILayout.Space(4f);

            Color prev = GUI.color;
            GUI.color = Color.Lerp(active.color, Color.white, 0.25f);
            GUILayout.Label($"Active Neon : {active.label}", _labelStyle);
            GUI.color = prev;

            GUILayout.Label($"Auto Crash-Test : {(autoLoop ? "ON" : "OFF")}", _labelStyle);
            GUILayout.Space(6f);
            GUILayout.Label("[SPACE]      Crash & Shatter Center Car", _labelStyle);
            GUILayout.Label("[LEFT CLICK] Smash Car / Spawn at Cursor", _labelStyle);
            GUILayout.Label("[1 - 6]      Switch Neon Color & Crash Test", _labelStyle);
            GUILayout.Label("[L]          Toggle Auto Crash-Test Loop", _labelStyle);
            GUILayout.EndArea();
        }

        private void EnsureGUIStyles()
        {
            if (_boxStyle == null)
            {
                Texture2D bg = new Texture2D(1, 1);
                bg.SetPixel(0, 0, new Color(0.02f, 0.05f, 0.11f, 0.82f));
                bg.Apply();
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = bg;
                _boxStyle.padding = new RectOffset(12, 12, 10, 10);
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                };
                _titleStyle.normal.textColor = new Color(0.0f, 0.95f, 1.0f);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12
                };
                _labelStyle.normal.textColor = new Color(0.86f, 0.94f, 1.0f);
            }
        }
    }
}
