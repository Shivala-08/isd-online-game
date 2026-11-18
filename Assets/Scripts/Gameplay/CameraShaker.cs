// CameraShaker.cs  --  the shake after a bomb
// -----------------------------------------------------------------------------
// Small camera shake for hits and explosions.
//
// Something calls Shake(amount, seconds) and this script wobbles the camera for
// that long. The wobble is an OFFSET that is added in LateUpdate and removed again
// at the start of the next LateUpdate, so it never fights with CameraCycler
// (which sets the camera position in Update) and it always ends exactly where the
// camera was.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace WhackAMole.Gameplay
{
    public class CameraShaker : MonoBehaviour
    {
        [Tooltip("How fast the camera wobbles, in shakes per second.")]
        [SerializeField] private float frequency = 18f;

        private float remaining;
        private float duration;
        private float amplitude;
        private Vector3 appliedOffset;

        /// <summary>Start a shake (a stronger or longer one replaces a running shake).</summary>
        /// <param name="amount">Maximum offset in metres, e.g. 0.05 for a hit.</param>
        /// <param name="seconds">How long the shake lasts.</param>
        public void Shake(float amount, float seconds)
        {
            amplitude = Mathf.Max(amplitude, amount);
            duration = Mathf.Max(duration, seconds);
            remaining = Mathf.Max(remaining, seconds);
        }

        private void LateUpdate()
        {
            // 1. Undo last frame's offset, so every other script sees the real position.
            if (appliedOffset != Vector3.zero)
            {
                transform.position -= appliedOffset;
                appliedOffset = Vector3.zero;
            }

            if (remaining <= 0f) return;

            remaining -= Time.unscaledDeltaTime;

            // 2. The shake fades out as it runs, so it ends smoothly instead of snapping.
            float k = duration <= 0f ? 0f : Mathf.Clamp01(remaining / duration);
            float strength = amplitude * k;
            float t = Time.unscaledTime * frequency;

            appliedOffset = new Vector3(Mathf.Sin(t * 2.7f) * strength,
                                        Mathf.Cos(t * 3.1f) * strength,
                                        0f);
            transform.position += appliedOffset;

            if (remaining <= 0f)
            {
                // 3. Finished: leave the camera exactly where it was.
                transform.position -= appliedOffset;
                appliedOffset = Vector3.zero;
                amplitude = 0f;
                duration = 0f;
            }
        }
    }
}