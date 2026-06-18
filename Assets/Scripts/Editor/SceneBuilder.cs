#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
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

            player.AddComponent<SurvivorState>();
            player.AddComponent<FirstPersonController>();
            player.AddComponent<PlayerMelee>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerHUD>();
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

                var z = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                z.name = "Zombie";
                z.transform.SetParent(parent);
                z.transform.position = hit.position + Vector3.up;
                z.GetComponent<Renderer>().sharedMaterial = mat;

                var agent = z.AddComponent<NavMeshAgent>();
                agent.radius = 0.4f; agent.height = 1.9f; agent.baseOffset = 1f;
                agent.angularSpeed = 240f; agent.acceleration = 12f;

                z.AddComponent<ZombieHealth>();
                z.AddComponent<ZombieAI>();

                // A head child so headshots are detectable by name.
                var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(z.transform);
                head.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                head.transform.localScale = Vector3.one * 0.55f;
                head.GetComponent<Renderer>().sharedMaterial = headMat;
            }
        }

        private static void BuildPickups(Transform parent)
        {
            var mat = ColorMat(new Color(0.9f, 0.8f, 0.2f));
            Vector3[] spots = { new Vector3(3, 0.5f, 3), new Vector3(-5, 0.5f, 2), new Vector3(2, 0.5f, -7) };
            string[] names = { "Canned Food", "Water Bottle", "Bandage" };

            for (int i = 0; i < spots.Length; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = "Pickup_" + names[i];
                p.transform.SetParent(parent);
                p.transform.position = spots[i];
                p.transform.localScale = Vector3.one * 0.4f;
                p.GetComponent<Renderer>().sharedMaterial = mat;
                p.AddComponent<ConsumablePickup>();
                // Note: tweak each pickup's nutrition/hydration/healing in the
                // Inspector — they default to canned food values.
            }
        }
    }
}
#endif
