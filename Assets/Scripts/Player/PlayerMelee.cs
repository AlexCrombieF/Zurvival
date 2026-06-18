using UnityEngine;
using UnityEngine.InputSystem;
using ZombieSurvival.Core;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Left-click melee swing. Costs stamina, has a cooldown, makes noise (a miss
    /// is quieter than a connecting hit), and does a forward raycast from the
    /// camera. Hitting a zombie's head is an instant kill; body hits chip its
    /// toughness. You will get tired mid-fight — that's intentional.
    /// </summary>
    public class PlayerMelee : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private float reach = 2.2f;
        [SerializeField] private float force = 22f;
        [SerializeField] private float cooldown = 0.6f;
        [SerializeField] private float staminaCost = 12f;

        [Header("Noise (radius in metres)")]
        [SerializeField] private float swingNoiseRadius = 4f;
        [SerializeField] private float hitNoiseRadius = 7f;

        private Camera cam;
        private SurvivorState survivor;
        private float cooldownTimer;

        private void Awake()
        {
            cam = GetComponentInChildren<Camera>();
            survivor = GetComponent<SurvivorState>();
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;

            if (survivor != null && survivor.IsDead) return;
            if (Mouse.current == null || cam == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame && cooldownTimer <= 0f)
                Swing();
        }

        private void Swing()
        {
            if (survivor != null && survivor.Stamina < staminaCost) return; // too exhausted

            cooldownTimer = cooldown;
            survivor?.DrainStamina(staminaCost);
            NoiseManager.Emit(transform.position, swingNoiseRadius);

            Vector3 origin = cam.transform.position + cam.transform.forward * 0.2f;
            if (Physics.Raycast(origin, cam.transform.forward, out var hit, reach))
            {
                var zombie = hit.collider.GetComponentInParent<ZombieHealth>();
                if (zombie != null)
                {
                    bool headshot = hit.collider.name.Contains("Head");
                    zombie.Hit(force, headshot);
                    NoiseManager.Emit(hit.point, hitNoiseRadius);
                }
            }
        }
    }
}
