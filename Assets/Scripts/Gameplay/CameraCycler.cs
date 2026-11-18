// CameraCycler.cs  --  cycles the camera views
// -----------------------------------------------------------------------------
// Press C to cycle the main camera between ready-made views of the board
// (Week 2 "cycling cameras" row).
//
// Each view is an empty GameObject in the scene plus its own field of view; the
// camera blends position, rotation and FOV towards it over a fraction of a second
// instead of cutting, so the player keeps their bearings.
//
// Only one Camera is moved (not three switched on and off), so the hit raycast and
// the camera shake keep working without special cases.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WhackAMole.Gameplay
{
    public class CameraCycler : MonoBehaviour
    {
        /// <summary>One stop of the C key: where the camera goes and how wide its lens is.</summary>
        [System.Serializable]
        public class CameraView
        {
            public string label = "View";
            public Transform anchor;
            public float fieldOfView = 45f;
        }

        [Header("References")]
        [SerializeField] private Camera cam;
        [Tooltip("One entry per C press, in order.")]
        [SerializeField] private CameraView[] views;

        [Header("Blend")]
        [Tooltip("Seconds for one camera switch.")]
        [SerializeField] private float blendTime = 0.45f;

        private int currentView;
        private Coroutine blendRoutine;

        private void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        private void Start()
        {
            // Start exactly on the first view, so the first C press moves to view 1.
            if (cam != null && views != null && views.Length > 0)
                Apply(views[0]);
        }

        private void Update()
        {
            // Key shortcuts belong to the game, not to the menus.
            if (Keyboard.current == null) return;
            if (!Keyboard.current.cKey.wasPressedThisFrame) return;
            Cycle();
        }

        /// <summary>Go to the next view in the list (wraps around after the last one).</summary>
        public void Cycle()
        {
            if (views == null || views.Length == 0) return;

            currentView = (currentView + 1) % views.Length;
            var view = views[currentView];
            if (view == null || view.anchor == null) return;

            if (blendRoutine != null) StopCoroutine(blendRoutine);
            blendRoutine = StartCoroutine(BlendTo(view));
        }

        private IEnumerator BlendTo(CameraView view)
        {
            Vector3 fromPosition = cam.transform.position;
            Quaternion fromRotation = cam.transform.rotation;
            float fromFov = cam.fieldOfView;

            float t = 0f;
            while (t < blendTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / blendTime);
                k = k * k * (3f - 2f * k);                     // smoothstep: ease in and out
                cam.transform.position = Vector3.Lerp(fromPosition, view.anchor.position, k);
                cam.transform.rotation = Quaternion.Slerp(fromRotation, view.anchor.rotation, k);
                cam.fieldOfView = Mathf.Lerp(fromFov, view.fieldOfView, k);
                yield return null;
            }

            Apply(view);
            blendRoutine = null;
        }

        /// <summary>Put the camera exactly on a view, with no blend.</summary>
        private void Apply(CameraView view)
        {
            if (view == null || view.anchor == null || cam == null) return;
            cam.transform.position = view.anchor.position;
            cam.transform.rotation = view.anchor.rotation;
            cam.fieldOfView = view.fieldOfView;
        }
    }
}