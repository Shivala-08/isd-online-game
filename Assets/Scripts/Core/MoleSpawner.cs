// MoleSpawner.cs  --  spawns moles and bombs out of the holes
// -----------------------------------------------------------------------------
// Pops moles (and sometimes bombs) out of random empty holes.
//
// Spawning rules:
//   * A new mole appears every 1.0 .. 1.5 seconds, picked at random.
//   * Later in the round moles stay up for less time (TRD difficulty curve).
//
// "Empty" is decided by looking at the array of live objects: when a Mole is
// destroyed by the game, its slot becomes empty automatically, so no hole can be
// used twice at the same moment (TRD F2).
// -----------------------------------------------------------------------------

using System.Collections;
using WhackAMole.Gameplay;
using UnityEngine;

namespace WhackAMole.Core
{
    public class MoleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Transform holesParent;   // the "Holes" object in the scene
        [SerializeField] private Mole molePrefab;
        [SerializeField] private Bomb bombPrefab;

        [Header("Spawning speed")]
        [Tooltip("Seconds to wait between two moles, picked at random inside this range.")]
        [SerializeField] private float gapMin = 1.0f;
        [SerializeField] private float gapMax = 1.5f;

        [Tooltip("Moles stay up for this many seconds at the start and at the end of a round.")]
        [SerializeField] private float stayAtStart = 1.4f;
        [SerializeField] private float stayAtEnd = 0.6f;

        [Header("Bombs")]
        [Tooltip("Chance that a spawned object is a bomb instead of a mole.")]
        [Range(0f, 1f)]
        [SerializeField] private float bombChance = 0.18f;

        // One slot per hole: null means the hole is free.
        private HoleOccupant[] occupants;
        private Coroutine spawnRoutine;

        private void Awake()
        {
            ResolveReferences();
            occupants = new HoleOccupant[holesParent != null ? holesParent.childCount : 0];
        }

        /// <summary>
        /// Find the pieces this script needs. Called from Awake and again from Start,
        /// because Awake order between GameManager and MoleSpawner is not guaranteed.
        /// </summary>
        private void ResolveReferences()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (holesParent == null)
            {
                var found = GameObject.Find("Holes");
                if (found != null) holesParent = found.transform;
            }
        }

        private void Start()
        {
            ResolveReferences();

            // Subscribing in Start (not OnEnable) makes the order of Awake/Start
            // between GameManager and this script irrelevant: GameManager may already
            // have started the round before this line runs, so we also check the
            // current state once.
            if (gameManager != null)
            {
                gameManager.StateChanged += OnStateChanged;
                gameManager.RoundStarted += OnRoundStarted;
            }

            if (gameManager != null && gameManager.State == GameState.Playing) BeginSpawning();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= OnStateChanged;
                gameManager.RoundStarted -= OnRoundStarted;
            }

            // Do not keep a coroutine alive on a switched-off object.
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }

        /// <summary>
        /// A new round started. This also covers "restart while playing", where the state
        /// does not change: the field is cleared and the spawn loop starts again, so no
        /// mole from the previous round survives (found while testing a restart).
        /// </summary>
        private void OnRoundStarted()
        {
            ClearField();
            BeginSpawning();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                BeginSpawning();
            }
            else if (spawnRoutine != null)
            {
                // Pause / game over / menu: stop spawning and clear the field.
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
                ClearField();
            }
        }

        private void BeginSpawning()
        {
            if (spawnRoutine != null) StopCoroutine(spawnRoutine);
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        /// <summary>The main loop: wait a random gap, spawn one, repeat until the round ends.</summary>
        private IEnumerator SpawnLoop()
        {
            while (gameManager != null && gameManager.State == GameState.Playing)
            {
                // Wait 1.0 .. 1.5 seconds, chosen at random each time.
                yield return new WaitForSeconds(Random.Range(gapMin, gapMax));

                if (gameManager == null || gameManager.State != GameState.Playing) break;

                SpawnOne();
            }
        }

        /// <summary>Pick a free hole and pop a mole or a bomb out of it.</summary>
        private void SpawnOne()
        {
            int hole = FindFreeHole();
            if (hole < 0) return;   // every hole is busy, try again next tick

            float stay = Mathf.Lerp(stayAtStart, stayAtEnd, gameManager != null ? gameManager.Progress : 0f);

            bool makeBomb = Random.value < bombChance;
            HoleOccupant prefab = makeBomb ? (HoleOccupant)bombPrefab : molePrefab;
            if (prefab == null) return;

            var holeTransform = holesParent.GetChild(hole);
            var spawned = Instantiate(prefab, holeTransform.position, Quaternion.identity, holesParent);
            spawned.Setup(hole, stay, holeTransform.position);

            occupants[hole] = spawned;
        }

        /// <summary>Index of a random hole with nothing in it, or -1 if all are busy.</summary>
        private int FindFreeHole()
        {
            // Count free holes first so the pick is uniform (a loop over 9 items).
            int freeCount = 0;
            for (int i = 0; i < occupants.Length; i++)
            {
                // "== null" is also true for a destroyed object, which frees the hole.
                if (occupants[i] == null) freeCount++;
            }
            if (freeCount == 0) return -1;

            int pick = Random.Range(0, freeCount);
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] != null) continue;
                if (pick == 0) return i;
                pick--;
            }
            return -1;
        }

        /// <summary>Remove every mole and bomb (used on game over and restart).</summary>
        private void ClearField()
        {
            for (int i = 0; i < occupants.Length; i++)
            {
                if (occupants[i] != null) Destroy(occupants[i].gameObject);
                occupants[i] = null;
            }
        }
    }
}