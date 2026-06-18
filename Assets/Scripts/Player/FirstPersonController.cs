using UnityEngine;
using UnityEngine.InputSystem; // new Input System low-level API (Keyboard/Mouse .current)
using ZombieSurvival.Core;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// First-person controller for the MVP: walk, sprint, crouch, mouse look,
    /// gravity, jump. Also emits footstep noise into the NoiseManager so zombies
    /// can hear you — louder when sprinting, near-silent when crouched.
    ///
    /// Reads input directly from Keyboard/Mouse so no .inputactions asset wiring
    /// is needed. Auto-finds the SurvivorState and child Camera, so the editor
    /// scene builder doesn't have to wire references either.
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
        [SerializeField] private float gravity = -19.62f;
        [SerializeField] private float jumpHeight = 1.0f;

        [Header("Noise footprint (radius in metres)")]
        [SerializeField] private float walkNoiseRadius = 8f;
        [SerializeField] private float sprintNoiseRadius = 16f;
        [SerializeField] private float crouchNoiseRadius = 2f;

        [Header("Survival link (auto-found if left empty)")]
        [SerializeField] private SurvivorState survivor;
        [SerializeField] private float sprintStaminaPerSecond = 12f;

        private CharacterController controller;
        private Camera cam;
        private float verticalVelocity;
        private float cameraPitch;
        private bool isCrouching;
        private float noiseTimer;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>();
            if (survivor == null) survivor = GetComponent<SurvivorState>();
            if (cam == null)
                Debug.LogWarning("FirstPersonController: no child Camera found.");
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (survivor != null && survivor.IsDead) return;
            HandleLook();
            HandleMovement();
            HandleCrouch();
        }

        private void HandleLook()
        {
            if (Mouse.current == null || cam == null) return;

            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(Vector3.up, delta.x);
            cameraPitch = Mathf.Clamp(cameraPitch - delta.y, -maxLookAngle, maxLookAngle);
            cam.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
            Vector3 move = transform.right * input.x + transform.forward * input.z;

            bool wantsSprint = kb.leftShiftKey.isPressed && !isCrouching && z > 0.1f;
            bool canSprint = wantsSprint && (survivor == null || survivor.CanSprint);
            float speed = isCrouching ? crouchSpeed : (canSprint ? sprintSpeed : walkSpeed);

            if (canSprint && move.sqrMagnitude > 0.01f && survivor != null)
                survivor.DrainStamina(sprintStaminaPerSecond * Time.deltaTime);

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (kb.spaceKey.wasPressedThisFrame && !isCrouching)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            // Footstep noise.
            float planarSpeed = new Vector2(move.x, move.z).magnitude * speed;
            if (controller.isGrounded && planarSpeed > 0.1f)
            {
                noiseTimer -= Time.deltaTime;
                if (noiseTimer <= 0f)
                {
                    noiseTimer = canSprint ? 0.30f : (isCrouching ? 0.7f : 0.5f);
                    float radius = isCrouching ? crouchNoiseRadius
                                 : (canSprint ? sprintNoiseRadius : walkNoiseRadius);
                    NoiseManager.Emit(transform.position, radius);
                }
            }
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
    }
}
