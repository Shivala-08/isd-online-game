// ScorePopup.cs  --  the floating score label
// -----------------------------------------------------------------------------
// The floating "+1" / "-2" that appears where a mole or bomb was hit.
//
// The prefab holds a 3D TextMeshPro label and this script. Spawn() is called by
// HoleOccupant when a hit lands; Update() then floats the label upwards, keeps it
// facing the camera so it is always readable, and fades it out before destroying
// the object. Nothing else has to clean it up.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;

namespace WhackAMole.UI
{
    public class ScorePopup : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private float lifetime = 0.85f;    // seconds until it disappears
        [SerializeField] private float riseSpeed = 1.6f;    // world units per second
        [SerializeField] private float fadeStart = 0.35f;   // fraction of lifetime when fading begins
        [SerializeField] private bool faceCamera = true;

        private TMP_Text label;
        private Color baseColor = Color.white;
        private float age;
        private Camera cachedCamera;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        /// <summary>Show a value such as "+1" at this object's position.</summary>
        public void Show(string text, Color color)
        {
            if (label == null) label = GetComponent<TMP_Text>();
            baseColor = color;
            age = 0f;

            if (label != null)
            {
                label.text = text;
                label.color = color;
            }
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;

            // 1. Float upwards.
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            // 2. Face the camera so the digits never appear mirrored.
            if (faceCamera)
            {
                if (cachedCamera == null) cachedCamera = Camera.main;
                if (cachedCamera != null)
                {
                    // LookRotation aims +Z at the camera, but a TextMeshPro label is
                    // readable from its -Z side, hence the extra half turn.
                    transform.rotation = Quaternion.LookRotation(transform.position - cachedCamera.transform.position)
                                         * Quaternion.Euler(0f, 180f, 0f);
                }
            }

            // 3. Fade out over the last part of the lifetime.
            if (label != null)
            {
                float k = Mathf.InverseLerp(lifetime * fadeStart, lifetime, age);
                var c = baseColor;
                c.a = 1f - k;
                label.color = c;
            }

            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
