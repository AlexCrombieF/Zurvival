using System;
using UnityEngine;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Global, decoupled noise channel. Anything that makes a sound (footsteps,
    /// melee swings, gunshots) calls Emit(); zombies subscribe to OnNoise and
    /// react if they're within the radius. Keeps the hearing system simple and
    /// avoids zombies omnisciently knowing where the player is.
    ///
    /// It's a static class so nothing needs wiring in the Inspector. The event is
    /// cleared automatically on each Play (domain reload), and subscribers must
    /// still unsubscribe in OnDisable.
    /// </summary>
    public static class NoiseManager
    {
        /// <summary>(worldPosition, radius) of a noise that just happened.</summary>
        public static event Action<Vector3, float> OnNoise;

        public static void Emit(Vector3 position, float radius)
        {
            if (radius > 0f)
                OnNoise?.Invoke(position, radius);
        }
    }
}
