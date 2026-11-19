using UnityEngine;

namespace DiepMultiplayer
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 24f, -13f);
        public float smoothTime = 0.10f;
        private Vector3 currentVelocity = Vector3.zero;

        private void Start()
        {
            transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            // Jika target belum diset atau hilang, otomatis cari player lokal kita
            if (target == null)
            {
                PlayerTankController[] players = FindObjectsByType<PlayerTankController>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if ((p.photonView == null || !Photon.Pun.PhotonNetwork.IsConnected || p.photonView.IsMine) && p.gameObject.activeInHierarchy)
                    {
                        target = p.transform;
                        transform.position = target.position + offset;
                        break;
                    }
                }
            }

            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(62f, 0f, 0f);
        }
    }
}
