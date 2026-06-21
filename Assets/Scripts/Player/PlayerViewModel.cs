using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// A first-person viewmodel built from primitives at runtime: two arms gripping
    /// a pipe, parented to the camera. Animated entirely in code — idle bob while
    /// moving, weapon sway from mouse movement, and a procedural swing arc
    /// (wind-up, chop, recover) triggered by Swing(). No rigged mesh or imported
    /// animation clips needed. Put this on the Player.
    ///
    /// Note: as a prototype it can clip into walls when you're right up against
    /// them. The proper fix later is a dedicated viewmodel camera/layer — overkill
    /// for now.
    /// </summary>
    public class PlayerViewModel : MonoBehaviour
    {
        [Header("Rest pose (camera-local space)")]
        [SerializeField] private Vector3 restPosition = new Vector3(0.18f, -0.20f, 0.45f);
        [SerializeField] private Vector3 restEuler = new Vector3(0f, -6f, 0f);

        [Header("Swing")]
        [SerializeField] private float swingDuration = 0.4f;
        [SerializeField] private float swingPitch = 75f;
        [SerializeField] private float swingYaw = -28f;
        [SerializeField] private float swingRoll = 16f;
        [SerializeField] private float swingLunge = 0.12f;

        [Header("Feel")]
        [SerializeField] private float bobAmount = 0.012f;
        [SerializeField] private float swayAmount = 0.02f;

        private Transform root;
        private Quaternion restRot;
        private CharacterController body;

        private float swingTimer;
        private float bobPhase;
        private Quaternion swayRot = Quaternion.identity;

        public bool IsSwinging => swingTimer > 0f;

        private void Awake()
        {
            var cam = GetComponentInChildren<Camera>();
            body = GetComponent<CharacterController>();
            if (cam == null) { Debug.LogWarning("PlayerViewModel: no camera found."); enabled = false; return; }

            restRot = Quaternion.Euler(restEuler);
            BuildGeometry(cam.transform);
        }

        /// <summary>Start a swing animation (called by PlayerMelee).</summary>
        public void Swing() => swingTimer = swingDuration;

        private void Update()
        {
            if (root == null) return;

            // Idle bob, scaled by how fast you're moving.
            float speed = body != null ? new Vector2(body.velocity.x, body.velocity.z).magnitude : 0f;
            bobPhase += Time.deltaTime * (4f + speed * 1.6f);
            float intensity = Mathf.Clamp01(speed / 3f + 0.12f);
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * bobAmount,
                                      -Mathf.Abs(Mathf.Sin(bobPhase)) * bobAmount, 0f) * intensity;

            // Weapon sway lags behind mouse movement.
            Vector2 m = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            var targetSway = Quaternion.Euler(m.y * swayAmount, -m.x * swayAmount, 0f);
            swayRot = Quaternion.Slerp(swayRot, targetSway, 12f * Time.deltaTime);

            // Swing arc.
            Quaternion swingRot = Quaternion.identity;
            Vector3 swingPos = Vector3.zero;
            if (swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                float p = Mathf.Clamp01(1f - swingTimer / swingDuration);
                float arc = Mathf.Sin(p * Mathf.PI);          // 0 -> 1 -> 0
                float windup = Mathf.Sin(p * Mathf.PI * 0.5f); // slight raise early
                swingRot = Quaternion.Euler(arc * swingPitch - (1f - windup) * 12f,
                                            arc * swingYaw, arc * swingRoll);
                swingPos = new Vector3(0f, 0f, arc * swingLunge);
            }

            root.localPosition = restPosition + bob + swingPos;
            root.localRotation = restRot * swayRot * swingRot;
        }

        private void BuildGeometry(Transform cam)
        {
            root = new GameObject("ViewModel").transform;
            root.SetParent(cam, false);
            root.localPosition = restPosition;
            root.localRotation = restRot;

            var metal = Mat(new Color(0.3f, 0.3f, 0.33f));
            var skin = Mat(new Color(0.76f, 0.6f, 0.5f));

            // Weapon: a pipe held diagonally in front.
            MakePart(PrimitiveType.Cylinder, "Pipe", metal,
                new Vector3(0f, 0.02f, 0.12f), new Vector3(72f, 0f, 6f), new Vector3(0.045f, 0.3f, 0.045f));

            // Right forearm gripping the pipe.
            MakePart(PrimitiveType.Capsule, "ArmR", skin,
                new Vector3(0.03f, -0.13f, -0.04f), new Vector3(62f, 0f, 8f), new Vector3(0.07f, 0.18f, 0.07f));

            // Left forearm bracing it.
            MakePart(PrimitiveType.Capsule, "ArmL", skin,
                new Vector3(-0.09f, -0.10f, 0.03f), new Vector3(58f, -22f, -4f), new Vector3(0.07f, 0.16f, 0.07f));
        }

        private void MakePart(PrimitiveType type, string name, Material mat,
                              Vector3 localPos, Vector3 localEuler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Viewmodel must not have colliders — they'd block the melee raycast.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material Mat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
