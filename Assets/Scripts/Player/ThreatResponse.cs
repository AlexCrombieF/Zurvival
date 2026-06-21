using UnityEngine;
using ZombieSurvival.Audio;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Turns nearby danger into felt tension: a heartbeat that quickens and a red
    /// panic vignette that pulses in time with it as a hunting zombie closes in.
    /// Threat = how close the nearest zombie that's actively hunting you is.
    /// Put this on the Player. Drawn with OnGUI so no post-processing setup needed.
    /// </summary>
    public class ThreatResponse : MonoBehaviour
    {
        [SerializeField] private float threatRange = 16f;
        [SerializeField] private float minHeartHz = 1.0f;
        [SerializeField] private float maxHeartHz = 2.4f;
        [SerializeField] private float maxVignetteAlpha = 0.45f;

        private SurvivorState survivor;
        private AudioSource heart;
        private Texture2D vignette;

        private ZombieAI[] zombies = System.Array.Empty<ZombieAI>();
        private float rescanTimer;
        private float threat;        // 0..1
        private float beatTimer;
        private float pulsePhase;

        private void Awake()
        {
            survivor = GetComponent<SurvivorState>();

            heart = gameObject.AddComponent<AudioSource>();
            heart.spatialBlend = 0f;
            heart.playOnAwake = false;

            vignette = BuildVignette();
        }

        private void Update()
        {
            // Low health alone also raises threat a little (you feel fragile).
            float danger = NearestHuntingThreat();
            if (survivor != null && survivor.HealthPct < 0.35f)
                danger = Mathf.Max(danger, 0.4f * (1f - survivor.HealthPct / 0.35f));

            threat = Mathf.MoveTowards(threat, danger, Time.deltaTime * 1.5f);

            if (threat > 0.05f)
            {
                float hz = Mathf.Lerp(minHeartHz, maxHeartHz, threat);
                pulsePhase += hz * Time.deltaTime;

                beatTimer -= Time.deltaTime;
                if (beatTimer <= 0f)
                {
                    beatTimer = 1f / hz;
                    heart.clip = ProceduralSfx.Heartbeat();
                    heart.volume = Mathf.Clamp01(threat);
                    heart.Play();
                }
            }
        }

        private float NearestHuntingThreat()
        {
            rescanTimer -= Time.deltaTime;
            if (rescanTimer <= 0f)
            {
                rescanTimer = 1f;
                zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            }

            float best = 0f;
            foreach (var z in zombies)
            {
                if (z == null || !z.IsHunting) continue;
                float d = Vector3.Distance(transform.position, z.transform.position);
                if (d <= threatRange)
                    best = Mathf.Max(best, 1f - d / threatRange);
            }
            return best;
        }

        private void OnGUI()
        {
            if (threat <= 0.08f) return;
            // Gentle pulse (0.7..1.0) rather than a hard on/off flash.
            float pulse = 0.85f + 0.15f * Mathf.Sin(pulsePhase * 2f * Mathf.PI);
            float alpha = threat * maxVignetteAlpha * pulse;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignette);
            GUI.color = prev;
        }

        private static Texture2D BuildVignette()
        {
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x / (s - 1f) - 0.5f) * 2f;
                float dy = (y / (s - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421356f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
                tex.SetPixel(x, y, new Color(0.55f, 0.02f, 0.02f, a));
            }
            tex.Apply();
            return tex;
        }
    }
}
