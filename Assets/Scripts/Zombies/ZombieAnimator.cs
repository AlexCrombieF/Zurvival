using UnityEngine;
using UnityEngine.AI;

namespace ZombieSurvival.Zombies
{
    /// <summary>
    /// Procedural animation for the primitive zombie rig — no animation clips.
    /// Reads the NavMeshAgent's speed for a shamble (body bob/sway/lurch + arms
    /// swinging from the shoulders) and the AI's attack state for a forward lunge
    /// and grabbing motion. Expects children: Rig, Rig/ShoulderL, Rig/ShoulderR.
    /// </summary>
    public class ZombieAnimator : MonoBehaviour
    {
        [Header("Walk shamble")]
        [SerializeField] private float stride = 2.2f;     // cycle speed
        [SerializeField] private float armSwing = 30f;    // arm swing degrees
        [SerializeField] private float armHang = -35f;    // base forward reach (shoulder pitch)
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float sway = 0.04f;
        [SerializeField] private float lurch = 8f;        // side-to-side body roll
        [SerializeField] private float hunch = 12f;       // constant forward lean

        [Header("Attack")]
        [SerializeField] private float reachAngle = -100f; // arms thrust forward
        [SerializeField] private float lungeLean = 18f;
        [SerializeField] private float grabSpeed = 8f;
        [SerializeField] private float blendSpeed = 7f;

        private NavMeshAgent agent;
        private ZombieAI ai;
        private ZombieHealth health;
        private Transform rig, shoulderL, shoulderR;

        private float phase;
        private float attackBlend;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<ZombieAI>();
            health = GetComponent<ZombieHealth>();

            rig = transform.Find("Rig");
            if (rig != null)
            {
                shoulderL = rig.Find("ShoulderL");
                shoulderR = rig.Find("ShoulderR");
            }
        }

        private void Update()
        {
            if (rig == null) return;
            if (health != null && health.IsDead) return;

            float speed = (agent != null && agent.enabled) ? agent.velocity.magnitude : 0f;
            float move = Mathf.Clamp01(speed / 1.5f);

            phase += Time.deltaTime * (stride * Mathf.Max(speed, 0.3f) + 1f);
            float s = Mathf.Sin(phase);
            float s2 = Mathf.Sin(phase * 2f);

            // Attack blend (0 walking -> 1 lunging).
            bool attacking = ai != null && ai.IsAttacking;
            attackBlend = Mathf.MoveTowards(attackBlend, attacking ? 1f : 0f, blendSpeed * Time.deltaTime);

            // Body: bob + sway while walking, constant hunch, lean in to attack.
            rig.localPosition = new Vector3(s * sway * move, Mathf.Abs(s2) * bobHeight * move, 0f);
            rig.localRotation = Quaternion.Euler(hunch + attackBlend * lungeLean, 0f, s * lurch * move);

            // Arms: opposite walk swing, blended toward a forward grab when attacking.
            float grab = Mathf.Sin(Time.time * 9f) * 14f * attackBlend;
            float walkL = armHang + s * armSwing * move;
            float walkR = armHang - s * armSwing * move;
            float reach = reachAngle + grab;

            if (shoulderL != null)
                shoulderL.localRotation = Quaternion.Euler(Mathf.Lerp(walkL, reach, attackBlend), 0f, 0f);
            if (shoulderR != null)
                shoulderR.localRotation = Quaternion.Euler(Mathf.Lerp(walkR, reach, attackBlend), 0f, 0f);
        }
    }
}
