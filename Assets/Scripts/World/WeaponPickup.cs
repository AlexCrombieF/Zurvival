using UnityEngine;
using ZombieSurvival.Player;

namespace ZombieSurvival.World
{
    /// <summary>
    /// A melee weapon lying in the world. Walk up, press E, and it becomes your
    /// equipped weapon (changing damage, reach, swing speed, and the viewmodel's
    /// look). Stand-in until there's a full inventory to carry multiple weapons.
    /// </summary>
    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private MeleeWeaponData weapon = new MeleeWeaponData();

        public string Prompt => $"Press E to pick up {weapon.name}";

        /// <summary>Set the carried weapon from code (used by the scene builder).</summary>
        public void Configure(MeleeWeaponData data) => weapon = data;

        public void Interact(GameObject user)
        {
            var melee = user.GetComponent<PlayerMelee>();
            if (melee != null) melee.EquipWeapon(weapon);
            Destroy(gameObject);
        }
    }
}
