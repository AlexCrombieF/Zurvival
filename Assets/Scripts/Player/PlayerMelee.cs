using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombieSurvival.Audio;
using ZombieSurvival.Core;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Left-click melee swing. Triggers the viewmodel animation, plays a whoosh,
    /// then lands the hit on the mid-swing impact frame. Weapon stats come from an
    /// equippable MeleeWeaponData, so picking up a bat/axe changes damage, reach,
    /// and swing speed. A connecting hit makes the zombie flinch and knocks it back.
    /// </summary>
    public class PlayerMelee : MonoBehaviour
    {
        [SerializeField] private MeleeWeaponData startingWeapon = new MeleeWeaponData();
        [Tooltip("Seconds into the swing when the weapon connects.")]
        [SerializeField] private float impactDelay = 0.13f;

        [Header("Noise (radius in metres)")]
        [SerializeField] private float swingNoiseRadius = 4f;
        [SerializeField] private float hitNoiseRadius = 7f;

        private Camera cam;
        private SurvivorState survivor;
        private PlayerViewModel viewModel;
        private AudioSource sfx;
        private MeleeWeaponData weapon;
        private float cooldownTimer;

        private void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            survivor = GetComponent<SurvivorState>();
            viewModel = GetComponent<PlayerViewModel>();
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.spatialBlend = 0f;
            sfx.playOnAwake = false;
            weapon = startingWeapon;
        }

        private void Start() => EquipWeapon(startingWeapon); // viewmodel is built by now

        /// <summary>Swap to a new weapon (called by weapon pickups).</summary>
        public void EquipWeapon(MeleeWeaponData newWeapon)
        {
            if (newWeapon == null) return;
            weapon = newWeapon;
            viewModel?.SetWeaponVisual(weapon.color, weapon.length);
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
            if (survivor != null && survivor.Stamina < weapon.staminaCost) return; // too exhausted

            cooldownTimer = weapon.cooldown;
            survivor?.DrainStamina(weapon.staminaCost);
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
            if (Physics.Raycast(origin, cam.transform.forward, out var hit, weapon.reach))
            {
                var zombie = hit.collider.GetComponentInParent<ZombieHealth>();
                if (zombie != null)
                {
                    bool headshot = hit.collider.name.Contains("Head");
                    Vector3 dir = cam.transform.forward; dir.y = 0f;
                    zombie.Hit(weapon.force, headshot, dir.normalized);
                    NoiseManager.Emit(hit.point, hitNoiseRadius);
                    sfx.pitch = headshot ? 1.2f : Random.Range(0.9f, 1.05f);
                    sfx.PlayOneShot(ProceduralSfx.Thud());
                }
            }
        }
    }
}
