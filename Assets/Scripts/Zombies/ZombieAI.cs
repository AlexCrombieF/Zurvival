using UnityEngine;
using UnityEngine.AI;
using ZombieSurvival.Core;
using ZombieSurvival.Player;

namespace ZombieSurvival.Zombies
{
    /// <summary>
    /// Shambler AI: a NavMeshAgent driven by a small state machine.
    ///   Wander      - drift to random nearby points.
    ///   Investigate - walk toward the last noise it heard.
    ///   Chase       - it can see the player; pursue.
    ///   Attack      - close enough; swing on an interval.
    ///
    /// Perception is sight (range + field-of-view + line-of-sight raycast) and
    /// hearing (subscribes to NoiseManager). It never reads the player's position
    /// directly unless it has actually seen or heard them.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
        private enum State { Wander, Investigate, Chase, Attack }

        [Header("Sight")]
        [SerializeField] private float sightRange = 14f;
        [SerializeField] private float fieldOfView = 110f;
        [SerializeField] private float eyeHeight = 1.6f;

        [Header("Hearing")]
        [Tooltip("Multiplies how far the zombie reacts to noise (1 = exactly the noise radius).")]
        [SerializeField] private float hearingSensitivity = 1f;

        [Header("Movement")]
        [SerializeField] private float wanderSpeed = 0.9f;
        [SerializeField] private float chaseSpeed = 3.2f;
        [SerializeField] private float wanderRadius = 8f;
        [SerializeField] private float wanderInterval = 5f;

        [Header("Attack")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackInterval = 1.2f;
        [SerializeField] private float biteInfectionChance = 0.7f;
        [SerializeField] private float memoryDuration = 6f; // how long it keeps chasing after losing sight

        private NavMeshAgent agent;
        private ZombieHealth health;
        private Transform player;
        private SurvivorState playerState;

        private State state = State.Wander;
        private Vector3 investigatePos;
        private Vector3 lastKnownPlayerPos;
        private float wanderTimer;
        private float attackTimer;
        private float memoryTimer;

        private float staggerTimer;
        private Vector3 knockDir;
        [SerializeField] private float knockbackSpeed = 2.5f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<ZombieHealth>();
            agent.speed = wanderSpeed;
            agent.stoppingDistance = attackRange * 0.8f;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerState = playerObj.GetComponent<SurvivorState>();
            }
        }

        /// <summary>True when it has the player and is chasing or attacking — used by audio / panic systems.</summary>
        public bool IsHunting => state == State.Chase || state == State.Attack;

        private void OnEnable()  => NoiseManager.OnNoise += OnHeardNoise;
        private void OnDisable() => NoiseManager.OnNoise -= OnHeardNoise;

        /// <summary>Knock the zombie back and briefly interrupt it (called on a melee hit).</summary>
        public void Stagger(Vector3 fromDir, float force)
        {
            staggerTimer = Mathf.Clamp(force / 60f, 0.12f, 0.45f);
            knockDir = new Vector3(fromDir.x, 0f, fromDir.z).normalized;
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (player == null) return;

            // Stagger: shoved backward and unable to act for a moment.
            if (staggerTimer > 0f)
            {
                staggerTimer -= Time.deltaTime;
                if (agent.enabled)
                {
                    agent.isStopped = true;
                    agent.Move(knockDir * knockbackSpeed * Time.deltaTime);
                }
                return;
            }
            if (agent.enabled && agent.isStopped) agent.isStopped = false;

            bool canSee = CanSeePlayer();
            if (canSee)
            {
                lastKnownPlayerPos = player.position;
                memoryTimer = memoryDuration;
                state = (DistanceToPlayer() <= attackRange) ? State.Attack : State.Chase;
            }
            else if (state == State.Chase || state == State.Attack)
            {
                // Lost sight — keep heading to where we last saw them for a while.
                memoryTimer -= Time.deltaTime;
                if (memoryTimer <= 0f)
                {
                    state = State.Investigate;
                    investigatePos = lastKnownPlayerPos;
                }
            }

            switch (state)
            {
                case State.Wander:      TickWander();      break;
                case State.Investigate: TickInvestigate(); break;
                case State.Chase:       TickChase();       break;
                case State.Attack:      TickAttack(canSee); break;
            }
        }

        private void TickWander()
        {
            agent.speed = wanderSpeed;
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f || (!agent.pathPending && agent.remainingDistance < 0.6f))
            {
                wanderTimer = wanderInterval;
                Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;
                if (NavMesh.SamplePosition(random, out var hit, wanderRadius, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
            }
        }

        private void TickInvestigate()
        {
            agent.speed = chaseSpeed * 0.75f;
            agent.SetDestination(investigatePos);
            if (!agent.pathPending && agent.remainingDistance < 1.2f)
                state = State.Wander;
        }

        private void TickChase()
        {
            agent.speed = chaseSpeed;
            agent.isStopped = false;
            agent.SetDestination(lastKnownPlayerPos);
            if (DistanceToPlayer() <= attackRange)
                state = State.Attack;
        }

        private void TickAttack(bool canSee)
        {
            FacePlayer();
            if (DistanceToPlayer() > attackRange)
            {
                state = canSee ? State.Chase : State.Investigate;
                if (!canSee) investigatePos = lastKnownPlayerPos;
                return;
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;
                if (playerState != null && !playerState.IsDead)
                {
                    playerState.ApplyDamage(attackDamage);
                    if (Random.value < biteInfectionChance) playerState.StartBleeding();
                }
            }
        }

        private bool CanSeePlayer()
        {
            float dist = DistanceToPlayer();
            if (dist > sightRange) return false;

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = (player.position + Vector3.up * 1f) - eye;
            if (Vector3.Angle(transform.forward, new Vector3(toPlayer.x, 0f, toPlayer.z)) > fieldOfView * 0.5f)
                return false;

            // Line of sight: first thing the ray hits must be the player.
            if (Physics.Raycast(eye, toPlayer.normalized, out var hit, sightRange))
                return hit.collider.GetComponentInParent<SurvivorState>() != null;

            return false;
        }

        private float DistanceToPlayer() => Vector3.Distance(transform.position, player.position);

        private void FacePlayer()
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        }

        private void OnHeardNoise(Vector3 pos, float radius)
        {
            if (state == State.Chase || state == State.Attack) return; // already focused on player
            if (Vector3.Distance(transform.position, pos) <= radius * hearingSensitivity)
            {
                state = State.Investigate;
                investigatePos = pos;
            }
        }
    }
}
