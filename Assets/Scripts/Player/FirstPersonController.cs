using UnityEngine;
using UnityEngine.InputSystem; // new Input System low-level API (Keyboard/Mouse .current)

namespace ZombieSurvival.Player
{
    /// <summary>
    /// Minimal first-person controller for the MVP vertical slice.
    /// Handles walking, sprinting, crouching, mouse look, and gravity.
    /// Reads input directly from Keyboard/Mouse so no .inputactions asset
    /// wiring is required — attach it and it works.
    ///
    /// Setup:
    ///   1. Create an empty GameObject named "Player".
    ///   2. Add a CharacterController component (Unity adds one automatically
    ///      because of [RequireComponent] below).
    ///   3. Make the Main Camera a CHILD of Player, positioned at roughly
    ///      (0, 0.6, 0) so it sits at "eye height".
    ///   4. Drop this script on the Player object.
    ///   5. Press Play. WASD to move, mouse to look, Shift to sprint,
    ///      Ctrl/C to crouch, Space to jump. Esc frees the cursor.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement speeds (metres/second)")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6.0f;
        [SerializeField] private float crouchSpeed = 1.6f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 0.08f;
        [SerializeField] private float maxLookAngle = 85f;

        [Header("Crouch")]
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float crouchLerpSpeed = 10f;

        [Header("Physics")]
        [SerializeField] private float gravity = -19.62f; // 2x real gravity feels better in games
        [SerializeField] private float jumpHeight = 1.0f;

        [Header("Optional — survival link")]
        [Tooltip("If set, sprinting drains stamina and is blocked when exhausted.")]
        [SerializeField] private SurvivorState survivor;
        [SerializeField] private float sprintStaminaPerSecond = 12f;

        private CharacterController controller;
        private Camera cam;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isCrouching;

        /// <summary>How much noise the player is currently making (0 = silent).
        /// Other systems (zombies) can read this later for the hearing system.</summary>
        public float CurrentNoise { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>();
            if (cam == null)
                Debug.LogWarning("FirstPersonController: no child Camera found. " +
                                 "Make the Main Camera a child of the Player object.");
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
            HandleCrouch();
            HandleCursorUnlock();
        }

        private void HandleLook()
        {
            if (Mouse.current == null || cam == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            // Yaw rotates the whole body; pitch only the camera.
            transform.Rotate(Vector3.up, delta.x);

            cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -maxLookAngle, maxLookAngle);
            cam.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Read WASD as a direction.
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
            Vector3 move = transform.right * input.x + transform.forward * input.z;

            // Decide speed.
            bool wantsSprint = kb.leftShiftKey.isPressed && !isCrouching && z > 0.1f;
            bool canSprint = wantsSprint && (survivor == null || survivor.CanSprint);
            float speed = isCrouching ? crouchSpeed : (canSprint ? sprintSpeed : walkSpeed);

            if (canSprint && move.sqrMagnitude > 0.01f && survivor != null)
                survivor.DrainStamina(sprintStaminaPerSecond * Time.deltaTime);

            // Gravity + jump.
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f; // keep grounded
                if (kb.spaceKey.wasPressedThisFrame && !isCrouching)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            // Crude noise footprint — louder when faster, near-silent when crouched.
            float planarSpeed = new Vector2(move.x, move.z).magnitude * speed;
            CurrentNoise = isCrouching ? planarSpeed * 0.25f : planarSpeed;
        }

        private void HandleCrouch()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.leftCtrlKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame))
                isCrouching = !isCrouching;

            float targetHeight = isCrouching ? crouchHeight : standHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, crouchLerpSpeed * Time.deltaTime);
            controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        }

        private void HandleCursorUnlock()
        {
            // Esc frees the cursor so you can click out of Play mode.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
