#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using ZombieSurvival.Audio;
using ZombieSurvival.Core;
using ZombieSurvival.Player;
using ZombieSurvival.UI;
using ZombieSurvival.World;
using ZombieSurvival.Zombies;

namespace ZombieSurvival.EditorTools
{
    /// <summary>
    /// Builds the entire MVP test scene from scratch and bakes the NavMesh, so
    /// you don't hand-place anything. Run it from the menu:
    ///   Tools -> Zombie Survival -> Build Test Scene
    /// Re-running it wipes the previously generated objects and rebuilds clean.
    /// </summary>
    public static class SceneBuilder
    {
        private const string RootName = "_ZS_Generated";

        [MenuItem("Tools/Zombie Survival/Build Test Scene")]
        public static void Build()
        {
            // Clear any previous build.
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);

            BuildGround(root.transform);
            BuildCover(root.transform);
            var sun = BuildSun(root.transform);
            var player = BuildPlayer(root.transform);
            BuildSystems(root.transform);

            // NavMesh must be baked after the ground/cover geometry exists.
            BakeNavMesh(root.transform);

            // Zombies + pickups get sampled onto the freshly baked NavMesh.
            BuildZombies(root.transform, 6);
            BuildPickups(root.transform);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=lime>Zombie Survival test scene built.</color> Press Play. " +
                      "WASD move, mouse look, Shift sprint, Ctrl crouch, Left-click swing, E interact.");
        }

        private static Material ColorMat(Color c)
        {
            // Works with URP's default Lit shader.
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        private static void BuildGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60x60 m
            ground.GetComponent<Renderer>().sharedMaterial = ColorMat(new Color(0.25f, 0.28f, 0.22f));
        }

        private static void BuildCover(Transform parent)
        {
            var mat = ColorMat(new Color(0.5f, 0.45f, 0.4f));
            // A loose scatter of "buildings" to break line of sight.
            Vector3[] spots =
            {
                new Vector3(8, 1.5f, 6), new Vector3(-10, 1.5f, -4), new Vector3(4, 1.5f, -12),
                new Vector3(-6, 1.5f, 10), new Vector3(14, 1.5f, -6), new Vector3(-14, 1.5f, 8)
            };
            foreach (var p in spots)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = "Building";
                b.transform.SetParent(parent);
                b.transform.position = p;
                b.transform.localScale = new Vector3(Random.Range(3f, 6f), 3f, Random.Range(3f, 6f));
                b.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private static GameObject BuildSun(Transform parent)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(parent);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            go.AddComponent<DayNightCycle>();
            return go;
        }

        private static GameObject BuildPlayer(Transform parent)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 1.1f, 0f);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);

            // Reuse the scene's Main Camera if it exists, else make one.
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.SetParent(player.transform);
            cam.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam.transform.localRotation = Quaternion.identity;
            cam.nearClipPlane = 0.05f; // so the close-up viewmodel hands aren't clipped away

            player.AddComponent<SurvivorState>();
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerViewModel>();
            player.AddComponent<PlayerMelee>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerHUD>();
            player.AddComponent<PlayerFootstepAudio>();
            player.AddComponent<ThreatResponse>();
            return player;
        }

        private static void BuildSystems(Transform parent)
        {
            var sys = new GameObject("_GameSystems");
            sys.transform.SetParent(parent);
            sys.AddComponent<GameManager>();
            sys.AddComponent<DeathScreen>();
        }

        private static void BakeNavMesh(Transform parent)
        {
            var go = new GameObject("NavMesh");
            go.transform.SetParent(parent);
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();

            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0)
                Debug.LogError("NavMesh bake produced 0 vertices — zombies won't move. " +
                               "Check that the Ground has a collider/renderer.");
            else
                Debug.Log($"NavMesh baked OK ({tri.vertices.Length} vertices).");
        }

        private static void BuildZombies(Transform parent, int count)
        {
            var mat = ColorMat(new Color(0.3f, 0.45f, 0.3f));
            var headMat = ColorMat(new Color(0.5f, 0.55f, 0.45f));

            for (int i = 0; i < count; i++)
            {
                Vector3 random = new Vector3(Random.Range(-22f, 22f), 0f, Random.Range(-22f, 22f));
                if (!NavMesh.SamplePosition(random, out var hit, 8f, NavMesh.AllAreas))
                    continue;

                // Root carries the agent + logic; the "Rig" child holds the animatable body.
                var z = new GameObject("Zombie");
                z.transform.SetParent(parent);
                z.transform.position = hit.position;

                var agent = z.AddComponent<NavMeshAgent>();
                agent.radius = 0.4f; agent.height = 1.9f; agent.baseOffset = 0f;
                agent.angularSpeed = 240f; agent.acceleration = 12f;

                z.AddComponent<ZombieHealth>();
                z.AddComponent<ZombieAI>();
                z.AddComponent<ZombieAudio>();
                z.AddComponent<ZombieAnimator>();

                var rig = new GameObject("Rig").transform;
                rig.SetParent(z.transform, false);

                // Torso.
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                body.transform.SetParent(rig, false);
                body.transform.localPosition = new Vector3(0f, 1f, 0f);
                body.GetComponent<Renderer>().sharedMaterial = mat;

                // Head (headshots detected by collider name).
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(rig, false);
                head.transform.localPosition = new Vector3(0f, 1.95f, 0f);
                head.transform.localScale = Vector3.one * 0.55f;
                head.GetComponent<Renderer>().sharedMaterial = headMat;

                // Shoulder-pivoted arms so they swing/reach naturally.
                MakeArm(rig, mat, true);
                MakeArm(rig, mat, false);
            }
        }

        private static void MakeArm(Transform rig, Material mat, bool left)
        {
            float side = left ? -1f : 1f;

            var shoulder = new GameObject(left ? "ShoulderL" : "ShoulderR").transform;
            shoulder.SetParent(rig, false);
            shoulder.localPosition = new Vector3(0.3f * side, 1.45f, 0f);

            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.name = "Arm";
            arm.transform.SetParent(shoulder, false);
            arm.transform.localPosition = new Vector3(0f, -0.28f, 0.18f);
            arm.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            arm.transform.localScale = new Vector3(0.16f, 0.34f, 0.16f);
            arm.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void BuildPickups(Transform parent)
        {
            // Consumables: (name, nutrition, hydration, healing, bandages, colour, position)
            MakeConsumable(parent, "Canned Food",  40f, 0f,  0f,  false, new Color(0.85f, 0.7f, 0.2f), new Vector3(3f, 0.4f, 3f));
            MakeConsumable(parent, "Water Bottle", 0f,  50f, 0f,  false, new Color(0.3f, 0.6f, 0.95f), new Vector3(-5f, 0.4f, 2f));
            MakeConsumable(parent, "Water Bottle", 0f,  50f, 0f,  false, new Color(0.3f, 0.6f, 0.95f), new Vector3(6f, 0.4f, -3f));
            MakeConsumable(parent, "Bandage",      0f,  0f,  25f, true,  new Color(0.95f, 0.95f, 0.95f), new Vector3(2f, 0.4f, -7f));
            MakeConsumable(parent, "Bandage",      0f,  0f,  25f, true,  new Color(0.95f, 0.95f, 0.95f), new Vector3(-8f, 0.4f, -2f));

            // Melee weapons: (data, colour comes from data, position)
            MakeWeapon(parent, new MeleeWeaponData("Baseball Bat", 35f, 2.4f, 0.45f, 10f, new Color(0.55f, 0.35f, 0.15f), 0.45f), new Vector3(-3f, 0.5f, 5f));
            MakeWeapon(parent, new MeleeWeaponData("Fire Axe",     60f, 2.2f, 0.85f, 18f, new Color(0.6f, 0.1f, 0.1f),    0.4f),  new Vector3(8f, 0.5f, 4f));
            MakeWeapon(parent, new MeleeWeaponData("Kitchen Knife", 18f, 1.6f, 0.35f, 6f, new Color(0.8f, 0.8f, 0.85f),  0.25f), new Vector3(4f, 0.5f, -4f));
        }

        private static void MakeConsumable(Transform parent, string name, float nutrition, float hydration,
                                           float healing, bool bandages, Color color, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Pickup_" + name;
            p.transform.SetParent(parent);
            p.transform.position = pos;
            p.transform.localScale = Vector3.one * 0.4f;
            p.GetComponent<Renderer>().sharedMaterial = ColorMat(color);
            p.AddComponent<ConsumablePickup>().Configure(name, nutrition, hydration, healing, bandages);
        }

        private static void MakeWeapon(Transform parent, MeleeWeaponData data, Vector3 pos)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "Weapon_" + data.name;
            p.transform.SetParent(parent);
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.12f, 0.12f, 0.8f); // weapon-ish proportions
            p.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            p.GetComponent<Renderer>().sharedMaterial = ColorMat(data.color);
            p.AddComponent<WeaponPickup>().Configure(data);
        }
    }
}
#endif
