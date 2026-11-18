// HoleOccupant.cs  --  the life cycle shared by the mole and the bomb
// -----------------------------------------------------------------------------
// Shared behaviour of anything that pops out of a hole: a mole or a bomb.
// The life cycle is the same for both (TRD section 6):
//
//   Rising (0.25 s) -> Up (the "stay" time) -> Falling (0.25 s) -> destroyed
//
// A hit only counts while the object is Rising or Up, and it only counts once.
// Subclasses decide what a hit is worth by overriding OnHit(), and which effects
// play by overriding PlayHitEffects().
//
// The look and feel handled here:
//   * Dust puff when the object starts to rise.
//   * Squash and stretch: the prefab's "Visual" child is stretched while it shoots
//     out of the hole, springs back with a small bounce, and is flattened on a hit.
//   * Physics knock-away: a hit mole gets a Rigidbody and is thrown up and away
//     from the camera instead of sinking back politely (Week 1 "physics" row).
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

namespace WhackAMole.Gameplay
{
    public abstract class HoleOccupant : MonoBehaviour
    {
        [Header("Life cycle (seconds)")]
        [SerializeField] protected float riseTime = 0.25f;
        [SerializeField] protected float fallTime = 0.25f;

        [Tooltip("How far below the hole the object waits, so it is hidden by the ground.")]
        [SerializeField] protected float hiddenHeight = -1.05f;

        [Tooltip("While rising, the object only counts as hittable once its base is above this height.")]
        [SerializeField] protected float hittableFromOffset = -0.35f;

        [Header("Squash and stretch")]
        [Tooltip("The visual part that is stretched and squashed. Leave empty to use this object.")]
        [SerializeField] private Transform squashTarget;
        [Tooltip("0.28 = the body is 28% taller and 28% thinner while it pops out.")]
        [SerializeField] private float stretchAmount = 0.28f;
        [Tooltip("0.35 = the body is 35% flatter and 35% wider when it is hit.")]
        [SerializeField] private float squashAmount = 0.35f;
        [Tooltip("How long the body takes to spring back to its normal shape at the top.")]
        [SerializeField] private float settleTime = 0.16f;

        [Header("Knock-away (physics)")]
        [SerializeField] private bool knockAwayOnHit = true;
        [Tooltip("How fast the body flies off after a hit, in metres per second.")]
        [SerializeField] private float knockAwaySpeed = 3.4f;
        [Tooltip("Seconds before the flying body is removed from the scene.")]
        [SerializeField] private float knockAwayLifetime = 1.1f;

        /// <summary>0..8, which hole of the 3x3 grid this object came out of.</summary>
        public int HoleIndex { get; private set; }

        /// <summary>True while the object may be hit.</summary>
        public bool IsHittable
        {
            get
            {
                if (!alive || phase == Phase.Falling) return false;

                // While rising, only the part that has cleared the ground can be hit.
                // Without this test a gaze pointer or the AI could score on a mole that
                // is still hidden below the surface (a player's click cannot reach it
                // because the ground blocks the ray).
                if (phase == Phase.Rising)
                    return transform.position.y - anchorPosition.y >= hittableFromOffset;

                return true;
            }
        }

        private enum Phase { Rising, Up, Falling }

        private Phase phase = Phase.Rising;
        private bool alive = true;         // false once hit or once it starts to retreat
        private bool wasHit;               // true only if the player hit it (not a timeout)
        private float staySeconds = 1f;
        private Vector3 anchorPosition;    // the hole's world position
        private Vector3 baseScale = Vector3.one;   // the "normal" shape of the visual part
        private Coroutine lifeRoutine;

        private void Awake()
        {
            // Remember the normal shape once, so every squash starts from the real size.
            if (squashTarget == null) squashTarget = transform;
            baseScale = squashTarget.localScale;
        }

        /// <summary>
        /// Called by MoleSpawner right after Instantiate.
        /// </summary>
        /// <param name="holeIndex">Which of the 9 holes this object belongs to.</param>
        /// <param name="stay">How long it stays up before retreating.</param>
        /// <param name="holePosition">World position of the hole.</param>
        public void Setup(int holeIndex, float stay, Vector3 holePosition)
        {
            HoleIndex = holeIndex;
            staySeconds = stay;
            anchorPosition = holePosition;

            // Start hidden under the hole.
            transform.position = anchorPosition + Vector3.up * hiddenHeight;

            // A puff of dust where it breaks the surface.
            if (HitEffects.Instance != null) HitEffects.Instance.RiseDust(anchorPosition);

            lifeRoutine = StartCoroutine(LifeCycle());
        }

        /// <summary>
        /// Called by the hit raycast or the hammer. Ignored if the object is already
        /// falling or gone, which is what stops a double click from scoring twice.
        /// </summary>
        public void Hit()
        {
            if (!IsHittable) return;

            alive = false;
            wasHit = true;

            // Stop the rise/stay/fall animation: the body is about to fly instead.
            if (lifeRoutine != null)
            {
                StopCoroutine(lifeRoutine);
                lifeRoutine = null;
            }

            OnHit();                                                   // score
            PlayHitEffects(anchorPosition + Vector3.up * 0.5f);         // sparks + label

            if (knockAwayOnHit) StartCoroutine(KnockAwayRoutine());
            else StartCoroutine(RetreatRoutine());
        }

        /// <summary>What this object is worth when hit (add or remove points).</summary>
        protected abstract void OnHit();

        /// <summary>Which effects play on a hit. A mole sparkles, a bomb explodes.</summary>
        protected abstract void PlayHitEffects(Vector3 worldPosition);

        /// <summary>
        /// Optional look swap used by the in-game material switcher (M3).
        /// A subclass that owns a body renderer overrides this.
        /// </summary>
        public virtual void ApplyLook(Material bodyMaterial) { }

        // =====================================================================
        //  Animation
        // =====================================================================

        private IEnumerator LifeCycle()
        {
            // 1. Rise out of the hole while stretching tall, like a spring.
            StartCoroutine(TweenScale(Stretched(), riseTime));
            yield return MoveY(hiddenHeight, 0f, riseTime);

            // Let the stretch spring back to the normal shape, with a small bounce
            // past it (that bounce is what makes the mole feel soft, not stiff).
            yield return TweenScale(baseScale, settleTime, 0.25f);

            // 2. Stay up, unless the player hits it first (then alive becomes false).
            phase = Phase.Up;
            float waited = 0f;
            while (waited < staySeconds && alive)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // 3. The player did not hit it: sink back into the hole and disappear.
            alive = false;
            if (wasHit) yield break;    // Hit() already started the knock-away
            yield return RetreatRoutine();

            Destroy(gameObject);
        }

        /// <summary>Sink back into the hole (used when nobody hits it).</summary>
        private IEnumerator RetreatRoutine()
        {
            phase = Phase.Falling;
            yield return MoveY(0f, hiddenHeight, fallTime);
        }

        /// <summary>
        /// Flatten, then turn the body into a physics object and throw it up and away
        /// from the camera. This is the "mole knocked away on hit" physics demo.
        /// </summary>
        private IEnumerator KnockAwayRoutine()
        {
            phase = Phase.Falling;

            // 1. Squash flat for a moment: the moment of impact.
            yield return TweenScale(Squashed(), 0.06f);

            // 2. Let Unity physics take over from here.
            var body = gameObject.AddComponent<Rigidbody>();
            body.mass = 0.35f;

            // Throw it away from the camera, never straight down into the board.
            var cam = Camera.main;
            Vector3 away = cam != null ? transform.position - cam.transform.position : Vector3.back;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Vector3.back;
            away.Normalize();

            body.linearVelocity = away * (knockAwaySpeed * 0.45f) + Vector3.up * knockAwaySpeed;
            body.angularVelocity = Random.insideUnitSphere * 7f;

            Destroy(gameObject, knockAwayLifetime);
        }

        /// <summary>The shape used while the object shoots out of its hole.</summary>
        private Vector3 Stretched()
        {
            return new Vector3(baseScale.x * (1f - stretchAmount),
                               baseScale.y * (1f + stretchAmount),
                               baseScale.z * (1f - stretchAmount));
        }

        /// <summary>The flat, wide shape used the moment the object is hit.</summary>
        private Vector3 Squashed()
        {
            return new Vector3(baseScale.x * (1f + squashAmount),
                               baseScale.y * (1f - squashAmount),
                               baseScale.z * (1f + squashAmount));
        }

        /// <summary>
        /// Blend the visual part from its current shape to a new one.
        /// </summary>
        /// <param name="to">Shape to end on.</param>
        /// <param name="duration">Seconds for the whole blend.</param>
        /// <param name="overshoot">
        /// 0 is a plain smooth blend. A small value like 0.25 makes it pass the target
        /// shape first and come back, which reads as a bounce.
        /// </param>
        private IEnumerator TweenScale(Vector3 to, float duration, float overshoot = 0f)
        {
            if (squashTarget == null) yield break;

            Vector3 from = squashTarget.localScale;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                float eased = 1f - Mathf.Pow(1f - k, 3f);                        // ease out
                eased += Mathf.Sin(k * Mathf.PI) * overshoot;                     // optional bounce
                squashTarget.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }
            squashTarget.localScale = to;
        }

        /// <summary>Smoothly move the object between two heights above its hole.</summary>
        private IEnumerator MoveY(float fromY, float toY, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                transform.position = anchorPosition + Vector3.up * Mathf.Lerp(fromY, toY, k);
                yield return null;
            }
            transform.position = anchorPosition + Vector3.up * toY;
        }
    }
}