using UnityEngine;
using UnityEngine.AI;
using ZombieSurvival.Core;

namespace ZombieSurvival.Zombies
{
    /// <summary>
    /// No health bar — kills are about hit location and weapon force, per the
    /// design pillars. A headshot drops the zombie instantly; body hits have to
    /// accumulate past its toughness. On death it disables AI, falls over, and
    /// is cleaned up after a delay.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        [SerializeField] private float bodyToughness = 40f;
        [SerializeField] private float corpseLifetime = 10f;

        private float damageTaken;
        public bool IsDead { get; private set; }

        /// <param name="force">Weapon force of the blow.</param>
        /// <param name="headshot">True if the hit landed on the head.</param>
        public void Hit(float force, bool headshot)
        {
            if (IsDead) return;

            if (headshot) { Die(); return; }

            damageTaken += force;
            if (damageTaken >= bodyToughness) Die();
        }

        private void Die()
        {
            IsDead = true;
            GameManager.Instance?.RegisterKill();

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            var ai = GetComponent<ZombieAI>();
            if (ai != null) ai.enabled = false;

            // Crude "collapse".
            transform.Rotate(90f, 0f, 0f, Space.Self);
            Destroy(gameObject, corpseLifetime);
        }
    }
}
