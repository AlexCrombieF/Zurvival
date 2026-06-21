using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieSurvival.Player
{
    /// <summary>
    /// First-person viewmodel built from primitives at runtime: two hands gripping
    /// a weapon (handle + head), with forearms leading down toward the body. The
    /// hands are children of the weapon so they always sit ON the handle, even as
    /// it's recoloured/resized for different weapons.
    ///
    /// Animated in code: idle bob, mouse sway, and a proper swing arc
    /// (wind-up -> fast chop -> ease-back recovery). Unlit materials + a small
    /// camera near-clip (set by the scene builder) keep it visible. Put on Player.
    /// </summary>
    public class PlayerViewModel : MonoBehaviour
    {
        [Header("Rest pose (camera-local space)")]
        [SerializeField] private Vector3 restPosition = new Vector3(0.13f, -0.20f, 0.5f);
        [SerializeField] private Vector3 restEuler = new Vector3(0f, -4f, 0f);

        [Header("Swing arc")]
        [SerializeField] private float swingDuration = 0.45f;
        [SerializeField] private float swingPitch = 95f;   // chop down angle
        [SerializeField] private float swingYaw = -32f;    // sweep across
        [SerializeField] private float swingRoll = 22f;
        [SerializeField] private float swingLunge = 0.14f; // forward thrust

        [Header("Feel")]
        [SerializeField] private float bobAmount = 0.012f;
        [SerializeField] private float swayAmount = 0.02f;

        private Transform root;
        private Transform weapon;
        private Transform handle;
        private Renderer handleRenderer;
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
            Debug.Log("PlayerViewModel built — two hands gripping a weapon, lower-centre/right.");
        }

        public void Swing() => swingTimer = swingDuration;

        /// <summary>Recolour/resize the held weapon when a new one is equipped.</summary>
        public void SetWeaponVisual(Color color, float length)
        {
            if (handle == null) return;
            handle.localScale = new Vector3(0.04f, Mathf.Max(0.12f, length), 0.04f);
            if (handleRenderer != null)
            {
                handleRenderer.material.color = color;
                if (handleRenderer.material.HasProperty("_BaseColor"))
                    handleRenderer.material.SetColor("_BaseColor", color);
            }
        }

        private void Update()
        {
            if (root == null) return;

            float speed = body != null ? new Vector2(body.velocity.x, body.velocity.z).magnitude : 0f;
            bobPhase += Time.deltaTime * (4f + speed * 1.6f);
            float intensity = Mathf.Clamp01(speed / 3f + 0.12f);
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * bobAmount,
                                      -Mathf.Abs(Mathf.Sin(bobPhase)) * bobAmount, 0f) * intensity;

            Vector2 m = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            var targetSway = Quaternion.Euler(m.y * swayAmount, -m.x * swayAmount, 0f);
            swayRot = Quaternion.Slerp(swayRot, targetSway, 12f * Time.deltaTime);

            Quaternion swingRot = Quaternion.identity;
            Vector3 swingPos = Vector3.zero;
            if (swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(1f - swingTimer / swingDuration);
                float arc = SwingArc(t); // -0.45 (raised) .. 1 (struck) .. 0 (rest)
                swingRot = Quaternion.Euler(arc * swingPitch, arc * swingYaw, arc * swingRoll);
                swingPos = new Vector3(arc * 0.03f,
                                       -Mathf.Max(0f, arc) * 0.03f,
                                       arc * swingLunge);
            }

            root.localPosition = restPosition + bob + swingPos;
            root.localRotation = restRot * swayRot * swingRot;
        }

        /// <summary>Wind-up (raise), fast chop, then ease back. Returns -0.45..1..0.</summary>
        private static float SwingArc(float t)
        {
            if (t < 0.30f) return Mathf.SmoothStep(0f, -0.45f, t / 0.30f);          // raise
            if (t < 0.52f) return Mathf.SmoothStep(-0.45f, 1f, (t - 0.30f) / 0.22f); // chop
            return Mathf.SmoothStep(1f, 0f, (t - 0.52f) / 0.48f);                    // recover
        }

        private void BuildGeometry(Transform cam)
        {
            root = new GameObject("ViewModel").transform;
            root.SetParent(cam, false);
            root.localPosition = restPosition;
            root.localRotation = restRot;

            var metal = UnlitMat(new Color(0.32f, 0.32f, 0.35f));
            var dark = UnlitMat(new Color(0.18f, 0.18f, 0.2f));
            var skin = UnlitMat(new Color(0.76f, 0.6f, 0.5f));

            // Weapon group, held at an angle. Hands attach to this so they stay gripped.
            weapon = new GameObject("Weapon").transform;
            weapon.SetParent(root, false);
            weapon.localPosition = new Vector3(0.03f, 0.04f, 0.05f);
            weapon.localRotation = Quaternion.Euler(42f, 8f, 18f);

            // Handle along the weapon's local Y axis.
            handle = MakeChild(weapon, PrimitiveType.Cylinder, "Handle", metal,
                Vector3.zero, Vector3.zero, new Vector3(0.04f, 0.26f, 0.04f));
            handleRenderer = handle.GetComponent<Renderer>();

            // Weapon head at the top of the handle.
            MakeChild(weapon, PrimitiveType.Cube, "Head", dark,
                new Vector3(0f, 0.26f, 0f), Vector3.zero, new Vector3(0.1f, 0.13f, 0.1f));

            // Two hands gripping the lower half of the handle.
            MakeChild(weapon, PrimitiveType.Cube, "HandUpper", skin,
                new Vector3(0f, -0.02f, 0f), Vector3.zero, new Vector3(0.09f, 0.07f, 0.11f));
            MakeChild(weapon, PrimitiveType.Cube, "HandLower", skin,
                new Vector3(0f, -0.16f, 0f), Vector3.zero, new Vector3(0.09f, 0.07f, 0.11f));

            // Forearms leading from the grip down toward the body.
            MakeChild(root, PrimitiveType.Capsule, "ForearmR", skin,
                new Vector3(0.07f, -0.17f, -0.02f), new Vector3(64f, 6f, 10f), new Vector3(0.065f, 0.18f, 0.065f));
            MakeChild(root, PrimitiveType.Capsule, "ForearmL", skin,
                new Vector3(-0.05f, -0.15f, 0.02f), new Vector3(66f, -14f, -6f), new Vector3(0.065f, 0.17f, 0.065f));
        }

        private Transform MakeChild(Transform parent, PrimitiveType type, string name, Material mat,
                                    Vector3 localPos, Vector3 localEuler, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // viewmodel must not block the melee raycast
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        private static Material UnlitMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }
    }
}
