using UnityEngine;
using UnityEngine.InputSystem;
using ZombieSurvival.World;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Raycasts forward from the camera; if it's looking at an IInteractable it
    /// exposes a prompt (the HUD reads this) and runs Interact() on E.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float range = 2.5f;

        private Camera cam;

        /// <summary>Current interaction prompt, or "" if nothing in range.</summary>
        public string Prompt { get; private set; } = "";

        private void Awake() => cam = GetComponentInChildren<Camera>();

        private void Update()
        {
            Prompt = "";
            if (cam == null) return;

            Vector3 origin = cam.transform.position + cam.transform.forward * 0.2f;
            if (Physics.Raycast(origin, cam.transform.forward, out var hit, range))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    Prompt = interactable.Prompt;
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                        interactable.Interact(gameObject);
                }
            }
        }
    }
}
