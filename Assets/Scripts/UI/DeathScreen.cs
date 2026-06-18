using UnityEngine;
using ZombieSurvival.Core;
using ZombieSurvival.Player;

namespace ZombieSurvival.UI
{
    /// <summary>
    /// Permadeath screen. Listens for SurvivorState.OnDeath, freezes the game,
    /// frees the cursor, and shows the run summary — "This is how you died." —
    /// with a button to start a fresh run. Drawn with OnGUI (no Canvas wiring).
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        private SurvivorState survivor;
        private bool dead;
        private Texture2D black;

        private void Start()
        {
            survivor = FindFirstObjectByType<SurvivorState>();
            if (survivor != null) survivor.OnDeath += HandleDeath;

            black = new Texture2D(1, 1);
            black.SetPixel(0, 0, new Color(0.02f, 0.02f, 0.02f, 0.92f));
            black.Apply();
        }

        private void OnDestroy()
        {
            if (survivor != null) survivor.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            dead = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!dead) return;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;

            var title = new GUIStyle(GUI.skin.label)
            { fontSize = 34, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.8f, 0.1f, 0.1f) } };
            GUI.Label(new Rect(cx - 300, cy - 120, 600, 50), "THIS IS HOW YOU DIED", title);

            var info = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            float secs = GameManager.Instance != null ? GameManager.Instance.SurvivedSeconds : 0f;
            int kills = GameManager.Instance != null ? GameManager.Instance.Kills : 0;
            string summary = $"Survived {Mathf.FloorToInt(secs / 60f)}m {Mathf.FloorToInt(secs % 60f)}s   ·   {kills} zombies put down";
            GUI.Label(new Rect(cx - 300, cy - 50, 600, 30), summary, info);

            if (GUI.Button(new Rect(cx - 90, cy + 20, 180, 44), "Start a new life"))
                GameManager.Instance?.Restart();
        }
    }
}
