using UnityEngine;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.Audio
{
    /// <summary>
    /// Spatial (3D) groans so you can hear roughly where a zombie is — directional
    /// audio is a core stealth cue. Groans more often and louder while hunting,
    /// goes quiet once dead.
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class ZombieAudio : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private float idleMin = 4f, idleMax = 9f;
        [SerializeField] private float huntMin = 1.5f, huntMax = 3f;

        private AudioSource source;
        private ZombieAI ai;
        private ZombieHealth health;
        private float timer;

        private void Awake()
        {
            ai = GetComponent<ZombieAI>();
            health = GetComponent<ZombieHealth>();

            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;               // fully 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = maxDistance;
            source.playOnAwake = false;

            timer = Random.Range(idleMin, idleMax);
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            bool hunting = ai != null && ai.IsHunting;
            timer = hunting ? Random.Range(huntMin, huntMax) : Random.Range(idleMin, idleMax);

            source.clip = ProceduralSfx.Groan();
            source.pitch = Random.Range(0.85f, 1.1f);
            source.volume = hunting ? 1f : 0.6f;
            source.Play();
        }
    }
}
