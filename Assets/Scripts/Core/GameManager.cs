using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZombieSurvival.Core
{
    /// <summary>
    /// Tracks the current run (start time, kills) for the death summary, and
    /// handles restarting. Simple singleton so the death screen and zombies can
    /// reach it without wiring.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public float RunStartTime { get; private set; }
        public int Kills { get; private set; }
        public float SurvivedSeconds => Time.time - RunStartTime;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            RunStartTime = Time.time;
        }

        public void RegisterKill() => Kills++;

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
