using UnityEngine;
using ZombieSurvival.Player;
using ZombieSurvival.World;

namespace ZombieSurvival.UI
{
    /// <summary>
    /// Minimal on-screen HUD drawn with OnGUI so nothing needs Canvas wiring:
    /// survival stat bars, the clock, a crosshair, and the interaction prompt.
    /// Diegetic UI is the long-term goal — this is a functional placeholder.
    /// Put it on the Player object.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        private SurvivorState survivor;
        private PlayerInteractor interactor;
        private DayNightCycle clock;
        private Texture2D bg, fill;

        private void Awake()
        {
            survivor = GetComponent<SurvivorState>();
            interactor = GetComponent<PlayerInteractor>();
            clock = FindFirstObjectByType<DayNightCycle>();
            bg = SolidTexture(new Color(0f, 0f, 0f, 0.5f));
            fill = SolidTexture(Color.white);
        }

        private static Texture2D SolidTexture(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void OnGUI()
        {
            if (survivor == null) return;

            // Crosshair.
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            GUI.DrawTexture(new Rect(cx - 1, cy - 6, 2, 12), fill);
            GUI.DrawTexture(new Rect(cx - 6, cy - 1, 12, 2), fill);

            // Stat bars.
            float x = 20, y = Screen.height - 130, w = 220, h = 16, gap = 22;
            Bar(x, y + gap * 0, w, h, survivor.HealthPct,  new Color(0.8f, 0.2f, 0.2f), "Health");
            Bar(x, y + gap * 1, w, h, survivor.HungerPct,  new Color(0.8f, 0.6f, 0.2f), "Hunger");
            Bar(x, y + gap * 2, w, h, survivor.ThirstPct,  new Color(0.3f, 0.5f, 0.9f), "Thirst");
            Bar(x, y + gap * 3, w, h, survivor.FatiguePct, new Color(0.6f, 0.4f, 0.8f), "Fatigue");
            Bar(x, y + gap * 4, w, h, survivor.StaminaPct, new Color(0.4f, 0.8f, 0.4f), "Stamina");

            if (survivor.IsBleeding)
                GUI.Label(new Rect(x + w + 12, y, 120, 20), "BLEEDING");

            if (clock != null)
                GUI.Label(new Rect(Screen.width - 90, 16, 80, 22), clock.Clock);

            if (interactor != null && !string.IsNullOrEmpty(interactor.Prompt))
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(cx - 150, cy + 24, 300, 22), interactor.Prompt, style);
            }
        }

        private void Bar(float x, float y, float w, float h, float pct, Color color, string label)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), bg);
            var prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(pct), h), fill);
            GUI.color = prev;
            GUI.Label(new Rect(x + 4, y - 1, w, h), label);
        }
    }
}
