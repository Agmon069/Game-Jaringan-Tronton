using UnityEngine;

namespace DiepMultiplayer
{
    public class AmmoOrb : MonoBehaviour
    {
        public AmmoType ammoType = AmmoType.Blue;
        public MeshRenderer meshRenderer;

        private Vector3 startPos;
        private float bobSpeed = 3f;
        private float bobHeight = 0.25f;

        private void Start()
        {
            startPos = transform.position;
            UpdateVisual();
        }

        private void Update()
        {
            // Bobbing animation and rotation
            transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        public void SetAmmoType(AmmoType type)
        {
            ammoType = type;
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) return;

            Color orbColor;
            switch (ammoType)
            {
                case AmmoType.Blue:
                    orbColor = new Color(0.2f, 0.6f, 1f); // Blue (+1)
                    break;
                case AmmoType.Green:
                    orbColor = new Color(0.2f, 0.9f, 0.3f); // Green (+3)
                    break;
                case AmmoType.Yellow:
                    orbColor = new Color(1f, 0.9f, 0.1f); // Yellow (+5)
                    break;
                case AmmoType.Orange:
                    orbColor = new Color(1f, 0.5f, 0.1f); // Orange (+8)
                    break;
                case AmmoType.Red:
                    orbColor = new Color(1f, 0.2f, 0.2f); // Red (+10)
                    break;
                default:
                    orbColor = Color.white;
                    break;
            }

            meshRenderer.material.color = orbColor;
        }

        private void OnTriggerEnter(Collider other)
        {
            TankBase tank = other.GetComponentInParent<TankBase>();
            if (tank != null)
            {
                bool isLocal = (tank is PlayerTankController pt && pt.photonView != null && pt.photonView.IsMine);
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayAmmoPickup(ammoType, transform.position, isLocal);
                }

                tank.AddAmmo((int)ammoType);

                // Notify spawner to respawn this orb at a new random position
                if (ArenaSpawner.Instance != null)
                {
                    ArenaSpawner.Instance.RespawnAmmoOrb(this);
                }
            }
        }

        public void Relocate(Vector3 newPosition, AmmoType newType)
        {
            transform.position = newPosition;
            startPos = newPosition;
            SetAmmoType(newType);
        }
    }
}
