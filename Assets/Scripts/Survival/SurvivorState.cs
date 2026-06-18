using System;
using UnityEngine;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Central survival model for the MVP. Hunger, thirst, fatigue and stamina
    /// deplete over time; when hunger or thirst bottoms out, health bleeds away.
    /// Bleeding adds an extra health drain until stopped.
    ///
    /// This is deliberately ONE component so the vertical slice is simple. As the
    /// design doc suggests, you can later split each stat into its own tickable
    /// system that reads/writes this state — but don't do that until you need to.
    ///
    /// Setup: drop this on the Player object. Hook up the FirstPersonController's
    /// "Survivor" field to this component so sprinting drains stamina.
    /// </summary>
    public class SurvivorState : MonoBehaviour
    {
        [Header("Maximums")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float maxThirst = 100f;
        [SerializeField] private float maxFatigue = 100f;
        [SerializeField] private float maxStamina = 100f;

        [Header("Depletion per minute (real time)")]
        [SerializeField] private float hungerLossPerMin = 1.2f;
        [SerializeField] private float thirstLossPerMin = 1.8f;
        [SerializeField] private float fatigueLossPerMin = 0.8f;

        [Header("Health")]
        [Tooltip("Health lost per second while starving OR dehydrated.")]
        [SerializeField] private float starvationDamagePerSec = 0.5f;
        [Tooltip("Health lost per second while bleeding.")]
        [SerializeField] private float bleedDamagePerSec = 1.5f;

        [Header("Stamina")]
        [SerializeField] private float staminaRegenPerSec = 15f;
        [Tooltip("Below this stamina, the player can't start sprinting.")]
        [SerializeField] private float sprintThreshold = 10f;

        // Current values.
        public float Health { get; private set; }
        public float Hunger { get; private set; }
        public float Thirst { get; private set; }
        public float Fatigue { get; private set; }
        public float Stamina { get; private set; }
        public bool IsBleeding { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>True when the player has enough stamina to begin a sprint.</summary>
        public bool CanSprint => Stamina > sprintThreshold;

        /// <summary>Fired once when health reaches zero. Hook your death screen here.</summary>
        public event Action OnDeath;

        // Normalized 0..1 accessors, handy for UI bars.
        public float HealthPct  => Health  / maxHealth;
        public float HungerPct  => Hunger  / maxHunger;
        public float ThirstPct  => Thirst  / maxThirst;
        public float FatiguePct => Fatigue / maxFatigue;
        public float StaminaPct => Stamina / maxStamina;

        private void Awake()
        {
            Health  = maxHealth;
            Hunger  = maxHunger;
            Thirst  = maxThirst;
            Fatigue = maxFatigue;
            Stamina = maxStamina;
        }

        private void Update()
        {
            if (IsDead) return;

            float dt = Time.deltaTime;
            float perSec = dt / 60f; // convert "per minute" rates to this frame

            Hunger  = Mathf.Max(0f, Hunger  - hungerLossPerMin  * perSec);
            Thirst  = Mathf.Max(0f, Thirst  - thirstLossPerMin  * perSec);
            Fatigue = Mathf.Max(0f, Fatigue - fatigueLossPerMin * perSec);

            // Passive stamina regen (you'd block this while sprinting via the controller).
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSec * dt);

            // Health consequences.
            float healthLoss = 0f;
            if (Hunger <= 0f) healthLoss += starvationDamagePerSec * dt;
            if (Thirst <= 0f) healthLoss += starvationDamagePerSec * dt;
            if (IsBleeding)   healthLoss += bleedDamagePerSec      * dt;
            if (healthLoss > 0f) ApplyDamage(healthLoss);
        }

        // --- Public API for other systems (food, water, zombies, bandages) ---

        public void Eat(float nutrition)  => Hunger  = Mathf.Min(maxHunger,  Hunger  + nutrition);
        public void Drink(float hydration) => Thirst = Mathf.Min(maxThirst,  Thirst  + hydration);
        public void Rest(float amount)    => Fatigue = Mathf.Min(maxFatigue, Fatigue + amount);
        public void DrainStamina(float amount) => Stamina = Mathf.Max(0f, Stamina - amount);

        public void StartBleeding() => IsBleeding = true;
        public void Bandage()       => IsBleeding = false;

        public void ApplyDamage(float amount)
        {
            if (IsDead) return;
            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f) Die();
        }

        public void Heal(float amount) => Health = Mathf.Min(maxHealth, Health + amount);

        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log("This is how you died.");
        }
    }
}
