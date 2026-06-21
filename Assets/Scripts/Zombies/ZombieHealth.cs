using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ZombieSurvival.Core;

namespace ZombieSurvival.Zombies
{
    /// <summary>
    /// No health bar — kills are about hit location and weapon force. A headshot
    /// drops the zombie instantly; body hits accumulate past its toughness. Every
    /// hit triggers a visible reaction: a white flash and a stagger/knockback
    /// (handled by ZombieAI). On death it disables AI, falls over, and despawns.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        [SerializeField] private float bodyToughness = 40f;
        [SerializeField] private float corpseLifetime = 10f;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        private float damageTaken;
        private ZombieAI ai;
        private Renderer[] renderers;
        private Color[] baseColors;

        public bool IsDead { get; private set; }

        private void Awake()
        {
            ai = GetComponent<ZombieAI>();
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].material.color; // instances the materials
        }

        /// <param name="force">Weapon force of the blow.</param>
        /// <param name="headshot">True if the hit landed on the head.</param>
        /// <param name="fromDir">Horizontal direction the blow came from (for knockback).</param>
        public void Hit(float force, bool headshot, Vector3 fromDir)
        {
            if (IsDead) return;

            StartCoroutine(Flash());
            if (ai != null) ai.Stagger(fromDir, headshot ? 80f : force);

            if (headshot) { Die(); return; }

            damageTaken += force;
            if (damageTaken >= bodyToughness) Die();
        }

        private IEnumerator Flash()
        {
            SetColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) Apply(renderers[i], baseColors[i]);
        }

        private void SetColor(Color c)
        {
            foreach (var r in renderers) if (r != null) Apply(r, c);
        }

        private static void Apply(Renderer r, Color c)
        {
            r.material.color = c;
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
        }

        private void Die()
        {
            IsDead = true;
            GameManager.Instance?.RegisterKill();

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            if (ai != null) ai.enabled = false;
            var anim = GetComponent<ZombieAnimator>();
            if (anim != null) anim.enabled = false; // stop animating so the corpse stays toppled

            transform.Rotate(90f, 0f, 0f, Space.Self); // collapse
            Destroy(gameObject, corpseLifetime);
        }
    }
}
