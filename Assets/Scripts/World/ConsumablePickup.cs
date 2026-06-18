using UnityEngine;
using ZombieSurvival.Player;

namespace ZombieSurvival.World
{
    /// <summary>
    /// A simple lootable item: walk up, press E, it restores some combination of
    /// hunger / thirst / health, then disappears. Stand-in for the full
    /// weight-based inventory + loot containers, which come after the core loop
    /// is proven fun.
    /// </summary>
    public class ConsumablePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private string displayName = "Canned Food";
        [SerializeField] private float nutrition = 40f;
        [SerializeField] private float hydration = 0f;
        [SerializeField] private float healing = 0f;
        [Tooltip("If true, taking this stops bleeding (e.g. a bandage).")]
        [SerializeField] private bool bandages = false;

        public string Prompt => $"Press E to use {displayName}";

        public void Interact(GameObject user)
        {
            var s = user.GetComponent<SurvivorState>();
            if (s != null)
            {
                if (nutrition > 0f) s.Eat(nutrition);
                if (hydration > 0f) s.Drink(hydration);
                if (healing > 0f) s.Heal(healing);
                if (bandages) s.Bandage();
            }
            Destroy(gameObject);
        }
    }
}
