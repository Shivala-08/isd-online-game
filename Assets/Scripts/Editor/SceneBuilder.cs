// SceneBuilder.cs  --  builds the scene, the materials and the prefabs
// -----------------------------------------------------------------------------
// This Editor-only script rebuilds the whole Whack-a-Mole scene and all prefabs
// from code. Why keep it? Because a beginner can press one menu item and get the
// same scene back after a bad change, and because rebuilding by hand would mean
// hundreds of clicks in the Editor.
//
// Menu items (top of the Unity window):
//   Whack-a-Mole / Build All             -> materials + prefabs + Game scene
//   Whack-a-Mole / Build Materials Only
//   Whack-a-Mole / Build Prefabs Only
// -----------------------------------------------------------------------------

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WhackAMole.Core;
using WhackAMole.Gameplay;
using WhackAMole.UI;

namespace WhackAMole.EditorTools
{
    public static class SceneBuilder
    {
        // --- Folder and file paths used by the builder -----------------------
        const string MatFolder    = "Assets/Materials";
        const string SpriteFolder = "Assets/Sprites";
        const string PrefabFolder = "Assets/Prefabs";
        const string ModelFolder  = "Assets/Models";
        const string SceneFolder  = "Assets/Scenes";
        const string GameScene    = SceneFolder + "/Game.unity";

        // Unity's built-in "Ignore Raycast" layer: the pointer ray skips it, physics does not.
        const int IgnoreRaycastLayer = 2;

        // The 3x3 grid of holes: x and z are each one of these (nested loop later).
        public static readonly float[] HoleAxis = { -3f, 0f, 3f };

        [MenuItem("Whack-a-Mole/Build All", false, 0)]
        public static void BuildAll()
        {
            BuildMaterials();
            BuildPrefabs();
            BuildGameScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] Build All finished: materials, prefabs and Assets/Scenes/Game.unity are up to date.");
        }

        [MenuItem("Whack-a-Mole/Build Materials Only", false, 20)]
        public static void BuildMaterials()
        {
            EnsureFolder(MatFolder);

            // URP Lit shader: the visible colour is _BaseColor (material.color).
            MakeMaterial("Ground_Grass",  new Color(0.32f, 0.62f, 0.28f), 0f, 0.15f);
            MakeMaterial("Hole_Dark",     new Color(0.045f, 0.035f, 0.03f), 0f, 0.05f);
            MakeMaterial("Dirt_Mound",    new Color(0.38f, 0.27f, 0.17f), 0f, 0.10f);
            MakeMaterial("Rim_Light",     new Color(0.74f, 0.68f, 0.52f), 0f, 0.15f);
            MakeMaterial("Border_Wood",   new Color(0.45f, 0.29f, 0.16f), 0f, 0.20f);
            MakeMaterial("Mole_Body",     new Color(0.52f, 0.31f, 0.17f), 0f, 0.25f);
            MakeMaterial("Mole_Eye",      new Color(0.04f, 0.04f, 0.04f), 0f, 0.85f);
            MakeMaterial("Mole_Nose",     new Color(0.85f, 0.45f, 0.52f), 0f, 0.45f);
            MakeMaterial("Bomb_Body",     new Color(0.10f, 0.10f, 0.12f), 0.7f, 0.65f);
            MakeMaterial("Bomb_Fuse",     new Color(0.85f, 0.55f, 0.15f), 0f, 0.30f);

            // Extra slots that only the Blender models use (the white of the eye,
            // the pupil, the metal band and the burning spark of the bomb).
            MakeMaterial("Mole_Eye_White", new Color(0.96f, 0.96f, 0.94f), 0f, 0.80f);
            MakeMaterial("Mole_Pupil",     new Color(0.03f, 0.03f, 0.03f), 0f, 0.90f);
            MakeMaterial("Bomb_Band",      new Color(0.32f, 0.33f, 0.36f), 0.75f, 0.75f);
            MakeMaterial("Bomb_Spark",     new Color(1.00f, 0.72f, 0.15f), 0f, 0.90f);
            MakeMaterial("Hammer_Metal",  new Color(0.62f, 0.64f, 0.68f), 0.85f, 0.70f);
            MakeMaterial("Hammer_Handle", new Color(0.40f, 0.24f, 0.12f), 0f, 0.30f);
            MakeMaterial("Sign_Wood",     new Color(0.40f, 0.24f, 0.12f), 0f, 0.20f);

            // Extra mole body colours for the in-game material switcher.
            MakeMaterial("Mole_Body_Blue",  new Color(0.30f, 0.45f, 0.78f), 0f, 0.35f);
            MakeMaterial("Mole_Body_Pink",  new Color(0.86f, 0.45f, 0.62f), 0f, 0.35f);
            MakeMaterial("Mole_Body_Green", new Color(0.42f, 0.68f, 0.34f), 0f, 0.35f);

            // The particle looks. Particles use a particle shader, not the Lit shader.
            MakeParticleMaterial("HitBurst",   new Color(1f, 0.95f, 0.75f));
            MakeParticleMaterial("Dust_Puff",  new Color(0.62f, 0.48f, 0.32f));
            MakeParticleMaterial("Explosion",  new Color(1f, 0.62f, 0.18f));
            MakeParticleMaterial("Confetti",   new Color(1f, 0.45f, 0.62f));

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Materials ready in " + MatFolder);
        }

        [MenuItem("Whack-a-Mole/Build Prefabs Only", false, 21)]
        public static void BuildPrefabs()
        {
            BuildMaterials();
            EnsureFolder(PrefabFolder);

            BuildHolePrefab();
            BuildHitBurstPrefab();     // particles spawned when something is hit
            BuildDustPrefab();         // dust puff when a mole rises
            BuildExplosionPrefab();    // bomb explosion
            BuildConfettiPrefab();     // new high score celebration
            BuildScorePopupPrefab();   // the floating "+1" / "-2" label
            BuildHammerPrefab();       // the physics hammer
            BuildMolePrefab();         // (mole and bomb point at the effect prefabs)
            BuildBombPrefab();
            BuildMenuPrefabs();        // Start, Pause and Game Over cards

            AssetDatabase.SaveAssets();
            Debug.Log("[SceneBuilder] Prefabs ready in " + PrefabFolder);
        }

        // =====================================================================
        //  PREFABS
        // =====================================================================

        /// <summary>
        /// A hole is built from three stacked layers, which reads as a dirt mound with a
        /// light rim around a dark opening:
        ///   1. DirtMound  - the widest, lowest piece: a squashed sphere that rises out of the grass
        ///   2. Rim        - a light sand-coloured disc sitting on the mound
        ///   3. InnerHole  - a small, very dark disc on top: the opening the mole comes out of
        /// None of the three keeps a collider, so a hammer or pointer ray passes straight
        /// through them and can still reach the mole (see HitRaycaster).
        /// </summary>
        static void BuildHolePrefab()
        {
            var root = new GameObject("Hole");

            // 1. Dirt mound: a sphere squashed vertically and sunk so only its cap shows.
            var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.name = "DirtMound";
            mound.transform.SetParent(root.transform, false);
            mound.transform.localPosition = new Vector3(0f, -0.32f, 0f);
            // Wide and low, so a strip of brown dirt stays visible around the light rim
            // (at grass level its radius is about 0.9 while the rim is 0.65).
            mound.transform.localScale = new Vector3(2.6f, 0.9f, 2.6f);
            mound.GetComponent<Renderer>().sharedMaterial = Mat("Dirt_Mound");
            Object.DestroyImmediate(mound.GetComponent<Collider>());

            // 2. Lighter rim: a thin disc on the mound around the opening.
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            rim.transform.localScale = new Vector3(1.30f, 0.015f, 1.30f);
            rim.GetComponent<Renderer>().sharedMaterial = Mat("Rim_Light");
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            // 3. Dark inner hole, slightly above the rim so it is never hidden by it.
            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "InnerHole";
            inner.transform.SetParent(root.transform, false);
            inner.transform.localPosition = new Vector3(0f, 0.135f, 0f);
            inner.transform.localScale = new Vector3(0.95f, 0.015f, 0.95f);
            inner.GetComponent<Renderer>().sharedMaterial = Mat("Hole_Dark");
            Object.DestroyImmediate(inner.GetComponent<Collider>());

            SavePrefab(root, PrefabFolder + "/Hole.prefab");
        }

        // --- particle effects ----------------------------------------------
        // All four effects are one-shot bursts that destroy their own GameObject when
        // the last particle dies (ParticleSystemStopAction.Destroy), so nothing has to
        // clean them up. They only differ in the numbers below.

        /// <summary>Sparks where a mole is hit: quick, small, pulled down by gravity.</summary>
        static void BuildHitBurstPrefab()
        {
            MakeBurstPrefab(new BurstSettings
            {
                name = "HitBurst",
                material = "HitBurst",
                count = 18,
                lifeMin = 0.25f, lifeMax = 0.45f,
                speedMin = 1.6f, speedMax = 3.6f,
                sizeMin = 0.05f, sizeMax = 0.13f,
                gravity = 1.2f,
                shape = ParticleSystemShapeType.Sphere,
                radius = 0.08f,
                colorA = new Color(1f, 0.95f, 0.75f),
                colorB = new Color(0.85f, 0.65f, 0.35f),
            });
        }

        /// <summary>Dust that puffs out of the hole when a mole or bomb starts to rise.</summary>
        static void BuildDustPrefab()
        {
            MakeBurstPrefab(new BurstSettings
            {
                name = "DustPuff",
                material = "Dust_Puff",
                count = 12,
                lifeMin = 0.35f, lifeMax = 0.7f,
                speedMin = 0.5f, speedMax = 1.4f,       // slow: it drifts up, then dies
                sizeMin = 0.10f, sizeMax = 0.24f,
                gravity = -0.15f,                        // slightly negative = rises
                shape = ParticleSystemShapeType.Circle,
                radius = 0.42f,                          // a ring around the hole rim
                colorA = new Color(0.72f, 0.58f, 0.40f),
                colorB = new Color(0.52f, 0.38f, 0.24f),
            });
        }

        /// <summary>Orange blast for a bomb: faster, bigger and longer than the mole sparks.</summary>
        static void BuildExplosionPrefab()
        {
            MakeBurstPrefab(new BurstSettings
            {
                name = "Explosion",
                material = "Explosion",
                count = 34,
                lifeMin = 0.35f, lifeMax = 0.7f,
                speedMin = 3.0f, speedMax = 7.0f,
                sizeMin = 0.16f, sizeMax = 0.36f,
                gravity = 1.4f,
                shape = ParticleSystemShapeType.Sphere,
                radius = 0.18f,
                colorA = new Color(1f, 0.85f, 0.35f),
                colorB = new Color(0.95f, 0.28f, 0.10f),
            });
        }

        /// <summary>Confetti for a new high score: many slow flakes falling from above.</summary>
        static void BuildConfettiPrefab()
        {
            MakeBurstPrefab(new BurstSettings
            {
                name = "Confetti",
                material = "Confetti",
                count = 130,
                lifeMin = 2.2f, lifeMax = 3.6f,
                speedMin = 0.4f, speedMax = 1.6f,
                sizeMin = 0.08f, sizeMax = 0.16f,
                gravity = 0.55f,
                shape = ParticleSystemShapeType.Box,
                boxSize = new Vector3(9f, 0.2f, 9f),     // the whole board wide
                colorA = new Color(1f, 0.45f, 0.62f),
                colorB = new Color(1f, 0.85f, 0.35f),
            });
        }

        /// <summary>Every number one particle look needs. Filled in by the builders above.</summary>
        struct BurstSettings
        {
            public string name;
            public string material;
            public int count;
            public float lifeMin, lifeMax;
            public float speedMin, speedMax;
            public float sizeMin, sizeMax;
            public float gravity;
            public ParticleSystemShapeType shape;
            public float radius;
            public Vector3 boxSize;
            public Color colorA, colorB;
        }

        /// <summary>
        /// Build one burst prefab from a BurstSettings and save it in Assets/Prefabs.
        /// </summary>
        static void MakeBurstPrefab(BurstSettings b)
        {
            var root = new GameObject(b.name);
            var ps = root.AddComponent<ParticleSystem>();

            // Stop before editing: changing the duration of a playing system is a warning.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(b.lifeMin, b.lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(b.speedMin, b.speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(b.sizeMin, b.sizeMax);
            main.gravityModifier = b.gravity;
            main.startColor = new ParticleSystem.MinMaxGradient(b.colorA, b.colorB);
            main.stopAction = ParticleSystemStopAction.Destroy;      // self cleanup
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // No steady emission: exactly one burst, so the effect lasts one moment.
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)b.count) });

            var shape = ps.shape;
            shape.shapeType = b.shape;
            shape.radius = b.radius;
            shape.scale = b.boxSize;

            // Fade out over the particle's life so nothing pops out of existence.
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            // NOTE: the ParticleSystem "Lights" module needs a Light prefab asset, not a
            // scene Light, so it is left off; the explosion reads through its colours,
            // its size and the camera shake instead.

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Mat(b.material);

            SavePrefab(root, PrefabFolder + "/" + b.name + ".prefab");
        }

        /// <summary>
        /// The floating "+1" / "-2" label (M3). A 3D TextMeshPro text with the
        /// ScorePopup script, which floats it up, turns it to the camera and fades it out.
        /// </summary>
        static void BuildScorePopupPrefab()
        {
            var root = new GameObject("ScorePopup");

            // TextMeshPro instead of the old TextMesh: sharp text and a real outline.
            var text = root.AddComponent<TextMeshPro>();
            text.text = "+1";
            text.fontSize = 9f;          // about 0.9 world units tall: readable from the game camera
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.sizeDelta = new Vector2(2f, 1f);

            // A dark outline keeps the number readable against the grass and the sky.
            // TMP only draws an outline when the flag is set on the MATERIAL, so the
            // default font material is copied into a project asset once and edited there.
            var outlineMat = MakeTextOutlineMaterial("ScorePopup_Text", 0.18f,
                                                     new Color(0.10f, 0.06f, 0.02f, 1f));
            if (outlineMat != null) text.fontSharedMaterial = outlineMat;

            root.AddComponent<ScorePopup>();

            SavePrefab(root, PrefabFolder + "/ScorePopup.prefab");
        }

        /// <summary>
        /// Copy the default TextMeshPro font material into a project asset and switch its
        /// outline on. Returns null (with a warning) if the TMP default font is missing,
        /// for example when the TMP Essential Resources were never imported.
        /// </summary>
        static Material MakeTextOutlineMaterial(string name, float width, Color color)
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                Debug.LogWarning("[SceneBuilder] No default TMP font asset. Import Window > TextMeshPro > Import TMP Essential Resources.");
                return null;
            }

            var source = defaultFont.material;
            string path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.CopyPropertiesFromMaterial(source);
            }

            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// The mole: a capsule body plus eyes and a nose, built from primitives.
        /// Origin (0,0,0) is at the bottom of the mole, so the Mole script can move
        /// the whole prefab up/down to make it rise out of its hole.
        /// When Mole.fbx is imported, it replaces the body mesh.
        /// </summary>
        static void BuildMolePrefab()
        {
            var root = new GameObject("Mole");

            // Everything that is drawn sits under "Visual". HoleOccupant stretches and
            // squashes only this child, so the collider on the root keeps a steady size.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // If Blender exported a model, use it. Otherwise fall back to primitives,
            // so the menu still builds a playable game on a machine without the FBX.
            var model = BuildModelObject("Mole", visual, new[]
            {
                "Mole_Body", "Mole_Nose", "Mole_Eye_White", "Mole_Pupil"
            });

            Renderer body;
            if (model != null)
            {
                body = model.GetComponentInChildren<Renderer>();
            }
            else
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Body";
                capsule.transform.SetParent(visual.transform, false);
                capsule.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                capsule.transform.localScale = new Vector3(0.62f, 0.42f, 0.62f);
                capsule.GetComponent<Renderer>().sharedMaterial = Mat("Mole_Body");
                Object.DestroyImmediate(capsule.GetComponent<Collider>());

                AddSphere(visual, "Eye_L",  new Vector3(-0.13f, 0.60f, 0.22f), 0.07f, "Mole_Eye");
                AddSphere(visual, "Eye_R",  new Vector3( 0.13f, 0.60f, 0.22f), 0.07f, "Mole_Eye");
                AddSphere(visual, "Nose",   new Vector3( 0.00f, 0.50f, 0.28f), 0.08f, "Mole_Nose");
                AddSphere(visual, "Ear_L",  new Vector3(-0.22f, 0.72f, 0.02f), 0.08f, "Mole_Nose");
                AddSphere(visual, "Ear_R",  new Vector3( 0.22f, 0.72f, 0.02f), 0.08f, "Mole_Nose");

                body = capsule.GetComponent<Renderer>();
            }

            // One capsule collider on the root: the raycast hits this object.
            // Sized to the imported model (0.6 wide, 0.75 tall, base at y = 0).
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.37f, 0f);
            col.height = 0.75f;
            col.radius = 0.30f;
            col.direction = 1; // Y axis

            // Rise/stay/fall plus +1 point, the squash/stretch target and the body
            // renderer that the in-game material switcher repaints.
            var mole = root.AddComponent<Mole>();
            SetRef(mole,
                ("squashTarget", visual.transform),
                ("bodyRenderer", body));

            SavePrefab(root, PrefabFolder + "/Mole.prefab");
        }

        /// <summary>The bomb: a dark sphere with a small fuse. Hitting it costs points.</summary>
        static void BuildBombPrefab()
        {
            var root = new GameObject("Bomb");

            // Same layout as the mole: the drawn parts hang under "Visual", which is
            // the object that gets squashed and stretched.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            // The Blender bomb when it exists, primitives otherwise.
            var model = BuildModelObject("Bomb", visual, new[]
            {
                "Bomb_Body", "Bomb_Band", "Bomb_Fuse", "Bomb_Spark"
            });

            if (model == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Body";
                sphere.transform.SetParent(visual.transform, false);
                sphere.transform.localPosition = new Vector3(0f, 0.36f, 0f);
                sphere.transform.localScale = Vector3.one * 0.72f;
                sphere.GetComponent<Renderer>().sharedMaterial = Mat("Bomb_Body");
                Object.DestroyImmediate(sphere.GetComponent<Collider>());

                var fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fuse.name = "Fuse";
                fuse.transform.SetParent(visual.transform, false);
                fuse.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                fuse.transform.localScale = new Vector3(0.08f, 0.10f, 0.08f);
                fuse.GetComponent<Renderer>().sharedMaterial = Mat("Bomb_Fuse");
                Object.DestroyImmediate(fuse.GetComponent<Collider>());
            }

            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.36f, 0f);
            col.radius = 0.36f;

            // Rise/stay/fall, -2 points and the squash/stretch target.
            // (The explosion itself comes from HitEffects, so the prefab needs no refs.)
            var bomb = root.AddComponent<Bomb>();
            SetRef(bomb, ("squashTarget", visual.transform));

            SavePrefab(root, PrefabFolder + "/Bomb.prefab");
        }

        // =====================================================================
        //  HAMMER (M3)
        // =====================================================================

        /// <summary>
        /// The hammer: a metal head on a wooden handle, moved by Hammer.cs. It is put
        /// on the "Ignore Raycast" layer so the pointer ray goes straight through it and
        /// can still reach a mole underneath (HitRaycaster would otherwise always hit
        /// the hammer, because the hammer follows the pointer).
        /// </summary>
        static void BuildHammerPrefab()
        {
            var root = new GameObject("Hammer");
            root.layer = IgnoreRaycastLayer;

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.layer = IgnoreRaycastLayer;
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            head.transform.localScale = new Vector3(0.95f, 0.42f, 0.5f);
            head.GetComponent<Renderer>().sharedMaterial = Mat("Hammer_Metal");
            // The head keeps its BoxCollider: it is what lands on the board.

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "Handle";
            handle.layer = IgnoreRaycastLayer;
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            handle.transform.localScale = new Vector3(0.11f, 0.45f, 0.11f);
            handle.GetComponent<Renderer>().sharedMaterial = Mat("Hammer_Handle");
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            var body = root.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.useGravity = false;
            body.isKinematic = true;

            var hammer = root.AddComponent<Hammer>();
            SetRef(hammer, ("head", head.transform));

            SavePrefab(root, PrefabFolder + "/Hammer.prefab");
        }

        // =====================================================================
        //  MENU CARDS (M3) - prefab assets in Assets/Prefabs/UI
        // =====================================================================

        /// <summary>
        /// Build the three menu cards as prefab assets, so they can be edited in the
        /// Editor and dropped into any scene: Start, Pause and Game Over.
        /// </summary>
        static void BuildMenuPrefabs()
        {
            EnsureFolder(PrefabFolder + "/UI");

            BuildStartCardPrefab();
            BuildPauseCardPrefab();
            BuildGameOverCardPrefab();
        }

        /// <summary>Full-screen dim behind a card: blocks clicks and darkens the board.</summary>
        static GameObject NewCardRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = root.GetComponent<RectTransform>();
            Stretch(rt);
            var image = root.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.62f);
            image.raycastTarget = true;
            return root;
        }

        /// <summary>A centred rounded card with a title, ready to hold text and buttons.</summary>
        static RectTransform NewCardBody(GameObject root, string title, Color titleColor)
        {
            var card = CreatePanel(root.transform, "Card", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 560f), new Color(0.09f, 0.11f, 0.16f, 0.95f));

            CreateTmp(card, "Title", title, new Vector2(0.5f, 0.5f),
                new Vector2(0f, 175f), new Vector2(820f, 120f), 84f, TextAlignmentOptions.Center, titleColor);
            return card;
        }

        static void BuildStartCardPrefab()
        {
            var root = NewCardRoot("StartCard");
            var card = NewCardBody(root, "WHACK-A-MOLE", new Color(1f, 0.85f, 0.35f));

            CreateTmp(card, "Subtitle", "Click the moles. Do not touch the bombs.", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 70f), new Vector2(820f, 70f), 42f, TextAlignmentOptions.Center, Color.white);

            CreateButton(card, "PlayButton", "PLAY", new Vector2(0f, -40f), new Vector2(420f, 110f));

            CreateTmp(card, "Hint", "Space swings the hammer  ·  C changes camera  ·  P pauses", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -170f), new Vector2(860f, 60f), 30f, TextAlignmentOptions.Center,
                new Color(0.75f, 0.80f, 0.88f));

            SavePrefab(root, PrefabFolder + "/UI/StartCard.prefab");
        }

        static void BuildPauseCardPrefab()
        {
            var root = NewCardRoot("PauseCard");
            var card = NewCardBody(root, "PAUSED", Color.white);

            CreateButton(card, "ResumeButton", "RESUME", new Vector2(0f, -20f), new Vector2(420f, 110f));
            CreateButton(card, "RestartButton", "RESTART", new Vector2(0f, -160f), new Vector2(420f, 110f));

            SavePrefab(root, PrefabFolder + "/UI/PauseCard.prefab");
        }

        static void BuildGameOverCardPrefab()
        {
            var root = NewCardRoot("GameOverCard");
            var card = NewCardBody(root, "GAME OVER", Color.white);

            CreateTmp(card, "FinalScoreText", "Your score: 0", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 55f), new Vector2(760f, 80f), 54f, TextAlignmentOptions.Center, Color.white);
            CreateTmp(card, "HighScoreLineText", "Best: 0", new Vector2(0.5f, 0.5f),
                new Vector2(0f, -25f), new Vector2(760f, 70f), 40f, TextAlignmentOptions.Center,
                new Color(1f, 0.85f, 0.35f));

            CreateButton(card, "RestartButton", "PLAY AGAIN", new Vector2(0f, -155f), new Vector2(460f, 110f));

            SavePrefab(root, PrefabFolder + "/UI/GameOverCard.prefab");
        }

        /// <summary>Instantiate a UI prefab card under the canvas and return it.</summary>
        static GameObject InstantiateCard(string prefabName, Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/UI/" + prefabName + ".prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[SceneBuilder] Missing UI prefab " + prefabName);
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.name = prefabName;
            return instance;
        }

        /// <summary>Find one button inside a card by its object name.</summary>
        static Button FindButton(GameObject root, string name)
        {
            if (root == null) return null;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (button.name == name) return button;
            Debug.LogWarning("[SceneBuilder] No button '" + name + "' in " + root.name);
            return null;
        }

        /// <summary>Find one text label inside a card by its object name.</summary>
        static TextMeshProUGUI FindLabel(GameObject root, string name)
        {
            if (root == null) return null;
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == name) return label;
            Debug.LogWarning("[SceneBuilder] No label '" + name + "' in " + root.name);
            return null;
        }

        // =====================================================================
        //  SCENE
        // =====================================================================

        [MenuItem("Whack-a-Mole/Build Game Scene", false, 22)]
        public static void BuildGameScene()
        {
            EnsureFolder(SceneFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Sky and ambient light ---------------------------------------
            RenderSettings.skybox = MakeSkyMaterial();
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.85f;   // slightly below 1 so colours stay saturated

            // --- Ground: a flat cube 12 x 12, top face at y = 0 --------------
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(12f, 0.5f, 12f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat("Ground_Grass");

            // --- Wooden border around the ground (four rails) -----------------
            // Each rail is a cube: two along Z, two along X. They sit on top of the
            // ground (top face at y = 0) and are a little longer than the ground so the
            // corners close without a gap.
            var border = new GameObject("Border");
            float half = 6f;          // the ground is 12 x 12
            float railLength = half * 2f + 0.4f;
            AddRail(border.transform, "Border_North", new Vector3(0f, 0.125f,  half + 0.2f), new Vector3(railLength, 0.25f, 0.4f));
            AddRail(border.transform, "Border_South", new Vector3(0f, 0.125f, -half - 0.2f), new Vector3(railLength, 0.25f, 0.4f));
            AddRail(border.transform, "Border_East",  new Vector3( half + 0.2f, 0.125f, 0f), new Vector3(0.4f, 0.25f, railLength));
            AddRail(border.transform, "Border_West",  new Vector3(-half - 0.2f, 0.125f, 0f), new Vector3(0.4f, 0.25f, railLength));

            // --- Holes: 3x3 grid made with a nested loop ---------------------
            var holesParent = new GameObject("Holes");
            var holePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Hole.prefab");

            int index = 0;                                   // 0..8, used by the AI later
            for (int xi = 0; xi < HoleAxis.Length; xi++)      // outer loop: x
            {
                for (int zi = 0; zi < HoleAxis.Length; zi++)  // inner loop: z
                {
                    var hole = (GameObject)PrefabUtility.InstantiatePrefab(holePrefab, holesParent.transform);
                    hole.name = "Hole_" + index;
                    hole.transform.localPosition = new Vector3(HoleAxis[xi], 0f, HoleAxis[zi]);
                    index++;
                }
            }

            // --- Wooden sign at the back of the board -------------------------
            BuildSign();

            // --- Cameras ----------------------------------------------------
            // Main camera: sits above and in front of the grid at (0, 9, 9) and looks
            // at the origin. LookAt is used instead of a fixed angle so the camera always
            // frames the middle hole no matter how the position is tuned.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            camGo.transform.position = new Vector3(0f, 6.5f, 6f);
            // Tilted 50 degrees down. Yaw is 180 because the camera sits at +Z: a yaw of 0
            // would aim it at empty sky behind the board (verified with a raycast), so it
            // is turned to face the board. The view centre then lands on the middle of the
            // 3x3 grid, about z = 0.5.
            camGo.transform.rotation = Quaternion.Euler(50f, 180f, 0f);
            camGo.AddComponent<AudioListener>();

            // The camera can shake (hits, explosions) and it can cycle through views.
            var shaker = camGo.AddComponent<CameraShaker>();

            // --- Lights: directional + point + spot -------------------------
            var sun = new GameObject("Directional Light");
            var sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 1.1f;
            sunLight.color = new Color(1f, 0.98f, 0.92f);   // warm daylight
            // Soft shadows: the edges fade out instead of being sharp pixel steps.
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.85f;                // a little light still fills the shadows
            sunLight.shadowBias = 0.05f;
            sunLight.shadowNormalBias = 0.4f;
            sunLight.shadowResolution = LightShadowResolution.FromQualitySettings;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // The procedural skybox draws its sun disc where this light is.
            RenderSettings.sun = sunLight;

            // --- Light blue fog (hides the far edge of the ground plane) -----
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.75f, 0.87f, 0.98f); // pale sky blue
            RenderSettings.fogDensity = 0.032f;

            var point = new GameObject("Point Light");
            var pointLight = point.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.93f, 0.78f);
            pointLight.intensity = 6f;
            pointLight.range = 12f;
            pointLight.enabled = false;      // switched on at night by LightPanel
            point.transform.position = new Vector3(0f, 3.5f, 0f);

            var spot = new GameObject("Spot Light");
            var spotLight = spot.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = new Color(0.75f, 0.85f, 1f);
            spotLight.intensity = 12f;
            spotLight.range = 20f;
            spotLight.spotAngle = 55f;
            spotLight.enabled = false;       // switched on at night by LightPanel
            spot.transform.position = new Vector3(-5.5f, 6f, -5.5f);
            spot.transform.LookAt(Vector3.zero);

            // --- Post-processing volume (URP) -------------------------------
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

            // --- Camera views for the C key (M3) -----------------------------
            // Empty objects with a position, a rotation and a lens: CameraCycler blends
            // the camera between them, so one press of C gives a different viewpoint.
            // Yaw is 180 for every view: the camera sits on the +Z side of the board, so a
            // yaw of 0 would aim it at the empty sky behind the board.
            var viewAngled = NewView("View_Angled", new Vector3(0f, 6.5f, 6f), 50f, 180f);
            var viewTop    = NewView("View_Top",    new Vector3(0f, 12.5f, 0.01f), 90f, 180f);
            var viewClose  = NewView("View_Close",  new Vector3(0f, 2.6f, 4.8f), 16f, 180f);

            var cycler = camGo.AddComponent<CameraCycler>();
            SetRef(cycler, ("cam", cam));
            SetCameraViews(cycler, new[]
            {
                (viewAngled.transform, 45f),
                (viewTop.transform, 50f),
                (viewClose.transform, 55f),
            });

            // --- Hit effects (M3): particles, sounds and camera shake ---------
            var effects = BuildHitEffects(shaker);

            // --- Managers, gameplay wiring and the HUD -----------------------
            BuildGameplayObjects(cam, shaker);
            BuildHUD(sunLight, pointLight, spotLight, shaker, cycler);

            // --- Save the scene and put it in the build list -----------------
            EditorSceneManager.SaveScene(scene, GameScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScene, true) };

            Debug.Log("[SceneBuilder] Game scene built: " + GameScene);
        }

        // =====================================================================
        //  GAMEPLAY OBJECTS (M2)
        // =====================================================================

        /// <summary>
        /// One "Game Manager" object holds GameManager (score, timer, state),
        /// MoleSpawner (spawning) and HitRaycaster (clicks). References between them
        /// are set here, so the scene works right after Build All.
        /// </summary>
        static void BuildGameplayObjects(Camera cam, CameraShaker shaker)
        {
            var go = new GameObject("Game Manager");
            var manager = go.AddComponent<GameManager>();
            var spawner = go.AddComponent<MoleSpawner>();
            go.AddComponent<HitRaycaster>();

            var holesParent = GameObject.Find("Holes");

            // The game opens on the Start card, so the round is started by the
            // Play button instead of automatically.
            SetBool(manager, "autoStart", false);

            SetRef(spawner,
                ("gameManager", manager),
                ("holesParent", holesParent != null ? holesParent.transform : null),
                ("molePrefab", AssetDatabase.LoadAssetAtPath<Mole>(PrefabFolder + "/Mole.prefab")),
                ("bombPrefab", AssetDatabase.LoadAssetAtPath<Bomb>(PrefabFolder + "/Bomb.prefab")));

            // --- The hammer (M3) ---------------------------------------------
            var hammerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Hammer.prefab");
            if (hammerPrefab != null)
            {
                var hammer = (GameObject)PrefabUtility.InstantiatePrefab(hammerPrefab);
                hammer.name = "Hammer";
                hammer.transform.position = new Vector3(0f, 1.55f, 0f);
                SetRef(hammer.GetComponent<Hammer>(), ("cam", cam));
            }
        }

        /// <summary>
        /// The "Hit Effects" object: particles, sounds and camera shake in one place.
        /// Everything it needs is assigned here so the scene works right after a build.
        /// </summary>
        static HitEffects BuildHitEffects(CameraShaker shaker)
        {
            var go = new GameObject("Hit Effects");
            var effects = go.AddComponent<HitEffects>();

            SetRef(effects,
                ("hitBurstPrefab", LoadPrefab("HitBurst")),
                ("explosionPrefab", LoadPrefab("Explosion")),
                ("dustPrefab", LoadPrefab("DustPuff")),
                ("confettiPrefab", LoadPrefab("Confetti")),
                ("scorePopupPrefab", AssetDatabase.LoadAssetAtPath<ScorePopup>(PrefabFolder + "/ScorePopup.prefab")),
                ("popSound", LoadAudio("SFX_Pop")),
                ("hitSound", LoadAudio("SFX_Hit")),
                ("bombSound", LoadAudio("SFX_Bomb")),
                ("cheerSound", LoadAudio("SFX_Cheer")),
                ("cameraShaker", shaker));
            return effects;
        }

        /// <summary>Load a prefab asset from Assets/Prefabs by name.</summary>
        static GameObject LoadPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + name + ".prefab");
            if (prefab == null) Debug.LogWarning("[SceneBuilder] Missing prefab " + name);
            return prefab;
        }

        /// <summary>
        /// Put a Blender model under a prefab.
        ///
        /// The FBX arrives as a prefab instance, so it is unpacked ("completely") into a
        /// plain GameObject first: the game prefab then holds real mesh objects instead of
        /// a nested prefab, which keeps the prefab files simple.
        ///
        /// The FBX carries its own simple materials. Unity keeps them in the order Blender
        /// wrote them, so the caller passes one project material name per slot and the
        /// model ends up painted with the same URP materials as the rest of the scene.
        ///
        /// Returns null when the FBX is missing, and the caller builds primitives instead.
        /// </summary>
        static GameObject BuildModelObject(string modelName, GameObject parent, string[] materialSlots)
        {
            string path = ModelFolder + "/" + modelName + ".fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("[SceneBuilder] No model at " + path + ", using primitives.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(asset);
            }

            instance.name = "Body";
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                                               InteractionMode.AutomatedAction);
            instance.transform.SetParent(parent.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            // Paint each sub-mesh with the project's own URP material.
            foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length && i < materialSlots.Length; i++)
                    materials[i] = Mat(materialSlots[i]);
                renderer.sharedMaterials = materials;

                // Static batching / light probes are pointless for a popped-up mole.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Debug.Log("[SceneBuilder] Model " + modelName + " imported into the prefab.");
            return instance;
        }

        /// <summary>Load a sound from Assets/Audio by name.</summary>
        static AudioClip LoadAudio(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
            if (clip == null) Debug.LogWarning("[SceneBuilder] Missing sound " + name);
            return clip;
        }

        /// <summary>Create one empty "camera view" object (position + looking direction).</summary>
        static GameObject NewView(string name, Vector3 position, float pitch, float yaw)
        {
            var view = new GameObject(name);
            view.transform.position = position;
            view.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return view;
        }

        /// <summary>Fill the CameraCycler view list from code (it is a serialized array).</summary>
        static void SetCameraViews(CameraCycler cycler, (Transform anchor, float fov)[] viewList)
        {
            var so = new SerializedObject(cycler);
            var array = so.FindProperty("views");
            if (array == null)
            {
                Debug.LogWarning("[SceneBuilder] CameraCycler has no 'views' array.");
                return;
            }

            array.arraySize = viewList.Length;
            for (int i = 0; i < viewList.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("label").stringValue = "View " + (i + 1);
                element.FindPropertyRelative("anchor").objectReferenceValue = viewList[i].anchor;
                element.FindPropertyRelative("fieldOfView").floatValue = viewList[i].fov;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Fill a Material[] field from material names in Assets/Materials.</summary>
        static void SetMaterialArray(Object target, string field, string[] materialNames)
        {
            var so = new SerializedObject(target);
            var array = so.FindProperty(field);
            if (array == null)
            {
                Debug.LogWarning("[SceneBuilder] " + target.GetType().Name + " has no field '" + field + "'");
                return;
            }

            array.arraySize = materialNames.Length;
            for (int i = 0; i < materialNames.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/" + materialNames[i] + ".mat");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Set a bool [SerializeField] from code.</summary>
        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[SceneBuilder] " + target.GetType().Name + " has no field '" + field + "'");
                return;
            }
            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        //  HUD + GAME OVER PANEL (M2)
        // =====================================================================

        /// <summary>
        /// Builds the on-screen interface: rounded semi-transparent panels with the score,
        /// the timer and the high score at the top, plus a hidden Game Over panel with a
        /// Restart button. All text is TextMeshPro (TMP essential resources are imported).
        /// </summary>
        static void BuildHUD(Light sun, Light pointLamp, Light spotLamp, CameraShaker shaker,
                             CameraCycler cycler)
        {
            // --- Canvas -----------------------------------------------------
            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Colours used by every panel. Alpha below 1 makes them see-through.
            var panelColor = new Color(0.06f, 0.08f, 0.11f, 0.55f);
            var goldColor  = new Color(1f, 0.85f, 0.35f);

            // --- Top bar: three rounded panels ------------------------------
            var scorePanel = CreatePanel(canvasGo.transform, "ScorePanel", new Vector2(0f, 1f),
                new Vector2(36f, -28f), new Vector2(380f, 100f), panelColor);
            var scoreText = CreateTmp(canvasGo.transform, "ScoreText", "SCORE  0", new Vector2(0f, 1f),
                new Vector2(60f, -46f), new Vector2(340f, 70f), 46f, TextAlignmentOptions.Left, Color.white);

            var timerPanel = CreatePanel(canvasGo.transform, "TimerPanel", new Vector2(0.5f, 1f),
                new Vector2(0f, -24f), new Vector2(320f, 136f), new Color(0.06f, 0.08f, 0.11f, 0.82f));
            // The timer text is the object that pulses (see HUDController).
            var timeText = CreateTmp(canvasGo.transform, "TimeText", "0:30", new Vector2(0.5f, 1f),
                new Vector2(0f, -46f), new Vector2(300f, 96f), 76f, TextAlignmentOptions.Center, Color.white);

            var bestPanel = CreatePanel(canvasGo.transform, "BestPanel", new Vector2(1f, 1f),
                new Vector2(-36f, -28f), new Vector2(380f, 100f), panelColor);
            var highScoreText = CreateTmp(canvasGo.transform, "HighScoreText", "BEST  0", new Vector2(1f, 1f),
                new Vector2(-60f, -46f), new Vector2(340f, 70f), 40f, TextAlignmentOptions.Right, goldColor);

            // --- Bottom-right control strip (M3) ----------------------------
            // Camera, day/night and mole colour switches, plus a Pause button.
            var cameraButton = CreateHudButton(canvasGo.transform, "CameraButton", "CAMERA",
                new Vector2(-36f, 64f), new Vector2(300f, 76f), out _);
            var nightButton = CreateHudButton(canvasGo.transform, "NightButton", "NIGHT",
                new Vector2(-36f, 150f), new Vector2(300f, 76f), out var nightLabel);
            var colourButton = CreateHudButton(canvasGo.transform, "ColourButton", "BROWN",
                new Vector2(-36f, 236f), new Vector2(300f, 76f), out var colourLabel);
            var pauseButton = CreateHudButton(canvasGo.transform, "PauseButton", "PAUSE",
                new Vector2(-36f, 322f), new Vector2(300f, 76f), out _);

            // --- Menu cards, from the prefab assets (M3) --------------------
            var startCard = InstantiateCard("StartCard", canvasGo.transform);
            var pauseCard = InstantiateCard("PauseCard", canvasGo.transform);
            var gameOverCard = InstantiateCard("GameOverCard", canvasGo.transform);
            if (pauseCard != null) pauseCard.SetActive(false);
            if (gameOverCard != null) gameOverCard.SetActive(false);

            var finalScoreText = FindLabel(gameOverCard, "FinalScoreText");
            var highScoreLineText = FindLabel(gameOverCard, "HighScoreLineText");
            var restartButton = FindButton(gameOverCard, "RestartButton");

            // --- EventSystem so the button can be clicked -------------------
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions(); // uses the project's default UI actions

            // --- Point the HUD script at the objects it must update ---------
            var hud = canvasGo.AddComponent<HUDController>();
            SetRef(hud,
                ("scoreText", scoreText),
                ("timeText", timeText),
                ("highScoreText", highScoreText),
                ("gameOverPanel", gameOverCard),
                ("finalScoreText", finalScoreText),
                ("highScoreLineText", highScoreLineText),
                ("restartButton", restartButton));

            // --- Menus and shortcuts (M3) -----------------------------------
            var ui = canvasGo.AddComponent<UIController>();
            SetRef(ui,
                ("startCard", startCard),
                ("pauseCard", pauseCard),
                ("playButton", FindButton(startCard, "PlayButton")),
                ("resumeButton", FindButton(pauseCard, "ResumeButton")),
                ("restartFromPauseButton", FindButton(pauseCard, "RestartButton")),
                ("pauseButton", pauseButton),
                ("cameraButton", cameraButton),
                ("cycler", cycler));

            // --- Day / night switch (M3) ------------------------------------
            var lightPanel = canvasGo.AddComponent<LightPanel>();
            SetRef(lightPanel,
                ("sun", sun),
                ("pointLamp", pointLamp),
                ("spotLamp", spotLamp),
                ("toggleButton", nightButton),
                ("toggleLabel", nightLabel),
                ("skyMaterial", AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/Sky_Procedural.mat")));

            // --- Mole colour switcher (M3) ----------------------------------
            var swapper = canvasGo.AddComponent<MaterialSwapper>();
            SetRef(swapper,
                ("swapButton", colourButton),
                ("swapLabel", colourLabel));
            SetMaterialArray(swapper, "palette", new[]
            {
                "Mole_Body", "Mole_Body_Blue", "Mole_Body_Pink", "Mole_Body_Green",
            });
        }

        /// <summary>
        /// A small rounded button in the HUD corner (different anchoring from
        /// CreateButton, which anchors to the centre of its parent).
        /// </summary>
        static Button CreateHudButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, Vector2 size, out TextMeshProUGUI labelText)
        {
            var background = CreatePanel(parent, name, new Vector2(1f, 0f), anchoredPosition, size,
                new Color(0.20f, 0.62f, 0.35f, 0.92f));
            var image = background.GetComponent<Image>();
            image.raycastTarget = true;      // the HUD button must catch the click

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            labelText = CreateTmp(background, "Label", label, new Vector2(0.5f, 0.5f),
                Vector2.zero, size, 34f, TextAlignmentOptions.Center, Color.white);

            var rt = labelText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return button;
        }

        // =====================================================================
        //  UI BUILDING BLOCKS
        // =====================================================================

        /// <summary>
        /// Loads (or once creates) a white rounded-rectangle sprite used by every panel.
        /// It is imported as a 9-sliced sprite, so one 64x64 texture stretches to any panel
        /// size while the rounded corners keep their true radius instead of being smeared.
        /// </summary>
        static Sprite RoundedSprite()
        {
            string path = SpriteFolder + "/RoundedPanel.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            EnsureFolder(SpriteFolder);

            const int size = 64;
            const int radius = 22;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Corner centres: the pixel is inside the rounded shape when it is
                    // within `radius` of the nearest corner centre.
                    float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                    float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - d + 0.5f);   // 1-pixel soft edge
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(24f, 24f, 24f, 24f);   // left, bottom, right, top
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A rounded, semi-transparent panel. Returns the Canvas it is drawn on.</summary>
        static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return rt;
        }

        /// <summary>Create a TextMeshPro label anchored to a point of its parent.</summary>
        static TextMeshProUGUI CreateTmp(Transform parent, string name, string content, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;   // HUD labels must not block clicks on moles
            return text;
        }

        /// <summary>
        /// A wooden plaque at the back of the board, leaning towards the camera, with the
        /// game title on it. It is laid almost flat (30 degrees above the ground) because the
        /// camera looks down at 50 degrees with a narrow 45 degree field of view: a tall
        /// upright sign at that distance would be above the top edge of the frame.
        /// </summary>
        static void BuildSign()
        {
            var sign = new GameObject("Sign");
            sign.transform.position = new Vector3(0f, 0.35f, -4.5f);
            sign.transform.rotation = Quaternion.Euler(-60f, 0f, 0f);   // face tilted up at the camera

            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = "Plaque";
            plaque.transform.SetParent(sign.transform, false);
            plaque.transform.localScale = new Vector3(5.4f, 0.9f, 0.12f);
            plaque.GetComponent<Renderer>().sharedMaterial = Mat("Sign_Wood");
            Object.DestroyImmediate(plaque.GetComponent<Collider>());

            // The letters are a legacy 3D TextMesh (TextMeshPro is not imported in this
            // project), placed just in front of the plaque's face.
            var title = new GameObject("Title");
            title.transform.SetParent(sign.transform, false);
            title.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            // A TextMesh is readable from its -Z side, so turn it half a circle to face the
            // camera; otherwise the plaque shows mirrored, upside-down letters.
            title.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var tm = title.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.text = "WHACK-A-MOLE";
            tm.fontSize = 96;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.13f, 0.08f, 0.04f);   // dark lettering on light wood

            var tmRenderer = title.GetComponent<MeshRenderer>();
            tmRenderer.sharedMaterial = tm.font.material;   // the atlas that draws the letters

            // Fit the text to the plaque width. characterSize scales the mesh linearly, so
            // measure the built mesh once and correct the size to 4.7 units wide.
            var bounds = tmRenderer.localBounds;
            if (bounds.size.x > 0.0001f)
                tm.characterSize *= 4.7f / bounds.size.x;
        }

        static void AddRail(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = position;
            rail.transform.localScale = scale;
            rail.GetComponent<Renderer>().sharedMaterial = Mat("Border_Wood");
            // Keep the box collider: the wooden border is solid, clicks on it hit nothing.
        }

        /// <summary>Create a rounded green button with a centred TextMeshPro label.</summary>
        static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.20f, 0.62f, 0.35f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelText = CreateTmp(go.transform, "Label", label, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, 44f, TextAlignmentOptions.Center, Color.white);

            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>Make a RectTransform fill its parent.</summary>
        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Assign private [SerializeField] references from code (the same thing the
        /// Inspector does). Used so Build All produces a fully wired scene.
        /// </summary>
        static void SetRef(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var property = so.FindProperty(field);
                if (property == null)
                {
                    Debug.LogWarning("[SceneBuilder] " + target.GetType().Name + " has no field '" + field + "'");
                    continue;
                }
                property.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        //  SKY
        // =====================================================================

        /// <summary>
        /// Build a bright procedural (gradient) sky: light blue sky tint, a soft sun disc
        /// and a muted ground colour. Assigned to RenderSettings.skybox so the camera's
        /// Skybox clear mode shows it. The sun disc follows RenderSettings.sun.
        /// </summary>
        static Material MakeSkyMaterial()
        {
            string path = MatFolder + "/Sky_Procedural.mat";
            var shader = Shader.Find("Skybox/Procedural");
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (sky == null)
            {
                sky = new Material(shader);
                AssetDatabase.CreateAsset(sky, path);
            }
            else if (sky.shader != shader)
            {
                sky.shader = shader;
            }

            sky.SetColor("_SkyTint", new Color(0.55f, 0.75f, 1f));        // bright blue sky
            // The game camera looks down at the board, so most of the background it sees
            // is the LOWER half of the sky dome. Keep that colour close to the fog so the
            // backdrop reads as bright hazy sky instead of the default dull olive.
            sky.SetColor("_GroundColor", new Color(0.70f, 0.84f, 0.95f));
            sky.SetFloat("_Exposure", 1.2f);                               // bright, but not blown out
            sky.SetFloat("_AtmosphereThickness", 0.75f);                  // thinner air = cleaner blue
            sky.SetFloat("_SunSize", 0.045f);
            sky.SetFloat("_SunSizeConvergence", 5f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        // =====================================================================
        //  Small helpers
        // =====================================================================

        static Material Mat(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatFolder + "/" + name + ".mat");
            if (m == null) Debug.LogWarning("[SceneBuilder] Missing material " + name + ", using default.");
            return m;
        }

        static void AddSphere(GameObject parent, string name, Vector3 localPos, float scale, string materialName)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = name;
            s.transform.SetParent(parent.transform, false);
            s.transform.localPosition = localPos;
            s.transform.localScale = Vector3.one * scale;
            s.GetComponent<Renderer>().sharedMaterial = Mat(materialName);
            Object.DestroyImmediate(s.GetComponent<Collider>());
        }

        /// <summary>Create (or recolour, if it already exists) a URP Lit material asset.</summary>
        static void MakeMaterial(string name, Color color, float metallic, float smoothness)
        {
            string path = MatFolder + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.color = color;                                  // -> _BaseColor in URP
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
        }

        /// <summary>
        /// Create (or recolour) a URP particle material. Particles use their own shader
        /// ("Universal Render Pipeline/Particles/Unlit"), not the Lit shader, so the
        /// normal MakeMaterial helper cannot be used.
        /// </summary>
        static void MakeParticleMaterial(string name, Color color)
        {
            string path = MatFolder + "/" + name + ".mat";
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
        }

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root); // the prefab asset stays, the temp scene object goes
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
