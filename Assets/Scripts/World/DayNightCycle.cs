using UnityEngine;

namespace ZombieSurvival.World
{
    /// <summary>
    /// Rotates the directional light to make a day/night cycle and tracks the
    /// in-game clock. Night gets dark and dangerous (the design intends you to
    /// hole up or use a flashlight). Put this on the Sun (directional light).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class DayNightCycle : MonoBehaviour
    {
        [Tooltip("Real seconds for one full 24h in-game day.")]
        [SerializeField] private float dayLengthSeconds = 300f;
        [Range(0f, 24f)][SerializeField] private float startHour = 8f;
        [SerializeField] private float maxSunIntensity = 1.2f;

        private Light sun;

        /// <summary>Current time of day, 0..24.</summary>
        public float TimeOfDay { get; private set; }

        public string Clock =>
            $"{Mathf.FloorToInt(TimeOfDay):00}:{Mathf.FloorToInt((TimeOfDay % 1f) * 60f):00}";

        private void Awake()
        {
            sun = GetComponent<Light>();
            TimeOfDay = startHour;
        }

        private void Update()
        {
            TimeOfDay += (24f / dayLengthSeconds) * Time.deltaTime;
            if (TimeOfDay >= 24f) TimeOfDay -= 24f;

            // Sun rises in the east at 6:00, sets at 18:00.
            float sunPitch = (TimeOfDay - 6f) / 12f * 180f;
            transform.rotation = Quaternion.Euler(sunPitch, 150f, 0f);

            // Brightest at noon, off at night.
            float daylight = Mathf.Clamp01(Mathf.Sin(TimeOfDay / 24f * Mathf.PI * 2f - Mathf.PI * 0.5f));
            sun.intensity = daylight * maxSunIntensity;
            RenderSettings.ambientIntensity = Mathf.Lerp(0.1f, 1f, daylight);
        }
    }
}
