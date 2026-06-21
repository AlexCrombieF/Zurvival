using UnityEngine;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Stats + look for a melee weapon. Plain serializable class (no asset files)
    /// so weapon pickups can carry their own values and hand them to PlayerMelee.
    /// </summary>
    [System.Serializable]
    public class MeleeWeaponData
    {
        public string name = "Pipe";
        public float force = 22f;        // headshots always kill; this is body damage / knockback
        public float reach = 2.2f;
        public float cooldown = 0.6f;    // seconds between swings (lower = faster)
        public float staminaCost = 12f;
        public Color color = new Color(0.3f, 0.3f, 0.33f);
        public float length = 0.3f;      // visual pipe length on the viewmodel

        public MeleeWeaponData() { }

        public MeleeWeaponData(string name, float force, float reach, float cooldown,
                               float staminaCost, Color color, float length)
        {
            this.name = name; this.force = force; this.reach = reach; this.cooldown = cooldown;
            this.staminaCost = staminaCost; this.color = color; this.length = length;
        }
    }
}
