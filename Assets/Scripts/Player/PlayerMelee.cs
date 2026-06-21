using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieSurvival.Audio;
using ZombieSurvival.Core;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Left-click melee swing. Triggers the viewmodel swing animation, plays a
    /// whoosh, then lands the actual hit on the mid-swing impact frame so the
    /// damage feels tied to the animation. Costs stamina, has a cooldown, makes
    /// noise, and a headshot is an instant kill.
    /// </summary>
    public class PlayerMelee : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private float reach = 2.2f;
        [SerializeField] private float force = 22f;
        [SerializeField] private float cooldown = 0.6f;
        [SerializeField] private float staminaCost = 12f;
        [Tooltip("Seconds into the swing when the pipe connects.")]
        [SerializeField] private float impactDelay = 0.13f;

        [Header("Noise (radius in metres)")]
        [SerializeField] private float swingNoiseRadius = 4f;
        [SerializeField] private float hitNoiseRadius = 7f;

        private Camera cam;
        private SurvivorState survivor;
        private PlayerViewModel viewModel;
        private AudioSource sfx;
        private float cooldownTimer;

        private void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            survivor = GetComponent<SurvivorState>();
            viewModel = GetComponent<PlayerViewModel>();
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            sfx.playOnAwake = false;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            if (survivor != null && survivor.IsDead) return;
            if (Mouse.current == null || cam == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame && cooldownTimer <= 0f)
                StartSwing();
        }

        private void StartSwing()
        {
            if (survivor != null && survivor.Stamina < staminaCost) return; // too exhausted

            cooldownTimer = cooldown;
            survivor?.DrainStamina(staminaCost);
            viewModel?.Swing();

            sfx.pitch = Random.Range(0.95f, 1.1f);
            sfx.PlayOneShot(ProceduralSfx.Whoosh());
            NoiseManager.Emit(transform.position, swingNoiseRadius);

            StartCoroutine(LandHit());
        }

        private IEnumerator LandHit()
        {
            yield return new WaitForSeconds(impactDelay);
            if (cam == null) yield break;

            Vector3 origin = cam.transform.position + cam.transform.forward * 0.2f;
            if (Physics.Raycast(origin, cam.transform.forward, out var hit, reach))
            {
                var zombie = hit.collider.GetComponentInParent<ZombieHealth>();
                if (zombie != null)
                {
                    bool headshot = hit.collider.name.Contains("Head");
                    zombie.Hit(force, headshot);
                    NoiseManager.Emit(hit.point, hitNoiseRadius);
                    sfx.pitch = headshot ? 1.2f : Random.Range(0.9f, 1.05f);
                    sfx.PlayOneShot(ProceduralSfx.Thud());
                }
            }
        }
    }
}
