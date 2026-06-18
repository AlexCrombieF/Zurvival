using UnityEngine;

namespace ZombieSurvival.World
{
    /// <summary>Anything the player can look at and press E on.</summary>
    public interface IInteractable
    {
        /// <summary>Text shown in the HUD when looked at, e.g. "Press E to take Water".</summary>
        string Prompt { get; }

        /// <param name="user">The player GameObject that triggered the interaction.</param>
        void Interact(GameObject user);
    }
}
