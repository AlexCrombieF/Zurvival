using UnityEngine;

namespace ZombieSurvival.Audio
{
    /// <summary>
    /// Plays footstep sounds timed to the player's movement speed, quieter when
    /// crouched. Reinforces the noise-footprint mechanic audibly. Put on Player.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootstepAudio : MonoBehaviour
    {
        [SerializeField] private float baseInterval = 0.5f;

        private CharacterController controller;
        private AudioSource source;
        private float timer;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f; // your own steps, non-positional
            source.playOnAwake = false;
        }

        private void Update()
        {
            Vector3 v = controller.velocity;
            float planarSpeed = new Vector2(v.x, v.z).magnitude;

            if (!controller.isGrounded || planarSpeed < 0.4f)
            {
                timer = 0f; // step immediately when you start moving again
                return;
            }

            bool crouched = controller.height < 1.4f;
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            // Faster speed -> quicker steps.
            timer = Mathf.Clamp(baseInterval * (3.5f / Mathf.Max(planarSpeed, 0.1f)), 0.28f, 0.7f);

            source.clip = ProceduralSfx.Footstep();
            source.pitch = Random.Range(0.95f, 1.05f);
            source.volume = crouched ? 0.18f : 0.5f;
            source.Play();
        }
    }
}
