using UnityEngine;
using TMPro;

namespace DiepMultiplayer
{
    public class PowerUpItem : MonoBehaviour
    {
        public PowerUpType powerUpType = PowerUpType.SpeedBoost;
        public float duration = 6f;
        public MeshRenderer meshRenderer;
        public TMP_Text labelText;

        private Vector3 startPos;
        private float bobSpeed = 3f;
        private float bobHeight = 0.35f;

        private void Start()
        {
            startPos = transform.position;
            UpdateVisual();
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, 75f * Time.deltaTime, Space.World);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            if (labelText != null && Camera.main != null)
            {
                labelText.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            }
        }

        public void SetPowerUpType(PowerUpType type)
        {
            powerUpType = type;
            switch (type)
            {
                case PowerUpType.SpeedBoost:
                    duration = 6f;
                    break;
                case PowerUpType.Shield:
                    duration = 4f;
                    break;
                case PowerUpType.TrailBoost:
                    duration = 7f;
                    break;
                case PowerUpType.EMP:
                    duration = 2f;
                    break;
                default:
                    duration = 6f;
                    break;
            }
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            Color boxColor;
            string text = "";

            switch (powerUpType)
            {
                case PowerUpType.SpeedBoost:
                    boxColor = new Color(0.1f, 1.0f, 0.35f);
                    text = "SPEED BOOST";
                    break;
                case PowerUpType.Shield:
                    boxColor = new Color(1.0f, 0.88f, 0.1f);
                    text = "SHIELD (TEMBUS)";
                    break;
                case PowerUpType.TrailBoost:
                    boxColor = new Color(0.0f, 0.88f, 1.0f);
                    text = "TRAIL BOOST";
                    break;
                case PowerUpType.EMP:
                    boxColor = new Color(0.95f, 0.18f, 0.9f);
                    text = "EMP (HAPUS JEJAK)";
                    break;
                default:
                    boxColor = Color.white;
                    break;
            }

            if (meshRenderer != null)
            {
                meshRenderer.material.color = boxColor;
                if (meshRenderer.material.HasProperty("_EmissionColor"))
                {
                    meshRenderer.material.EnableKeyword("_EMISSION");
                    meshRenderer.material.SetColor("_EmissionColor", boxColor * 1.4f);
                }
            }

            if (labelText != null)
            {
                labelText.text = text;
                labelText.color = boxColor;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TankBase tank = other.GetComponentInParent<TankBase>();
            if (tank != null && tank.currentHealth > 0)
            {
                bool isLocal = (tank is PlayerTankController pt && (pt.photonView == null || !Photon.Pun.PhotonNetwork.IsConnected || pt.photonView.IsMine));
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayPowerUp(powerUpType, transform.position, isLocal);
                }

                tank.ApplyPowerUp(powerUpType, duration);

                if (ArenaSpawner.Instance != null)
                {
                    ArenaSpawner.Instance.RespawnPowerUp(this);
                }
            }
        }

        public void Relocate(Vector3 newPosition, PowerUpType newType)
        {
            transform.position = newPosition;
            startPos = newPosition;
            SetPowerUpType(newType);
        }
    }
}
