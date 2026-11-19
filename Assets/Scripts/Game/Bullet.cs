using UnityEngine;

namespace DiepMultiplayer
{
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        public float speed = 45f;
        public int damage = 5;
        public float lifetime = 3.5f;

        [HideInInspector] public string shooterName = "";
        [HideInInspector] public int shooterActorNumber = -1;
        [HideInInspector] public bool shooterIsBot = false;

        private float spawnTime;

        private void Start()
        {
            spawnTime = Time.time;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += transform.forward * (speed * Time.deltaTime);
        }

        public void Initialize(string shooter, int actorNum, bool isBot, bool isBig)
        {
            shooterName = shooter;
            shooterActorNumber = actorNum;
            shooterIsBot = isBot;

            if (isBig)
            {
                damage = 10; // Double damage dari 5
                transform.localScale = Vector3.one * 1.3f;
            }
            else
            {
                damage = 5; // Damage standar 5
                transform.localScale = Vector3.one * 0.6f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if hit a tank
            TankBase hitTank = other.GetComponentInParent<TankBase>();
            if (hitTank != null)
            {
                // Prevent shooting self
                if (hitTank.actorNumber == shooterActorNumber && hitTank.isBot == shooterIsBot)
                {
                    return;
                }

                // Deal damage to tank
                hitTank.TakeDamage(damage, shooterName, shooterActorNumber, shooterIsBot);
                Destroy(gameObject);
                return;
            }

            // Wall, obstacle, or arena border
            string hitName = other.gameObject.name;
            if (hitName.Contains("Wall") || hitName.Contains("Boundary") || hitName.Contains("Obstacle"))
            {
                Destroy(gameObject);
            }
        }
    }
}
