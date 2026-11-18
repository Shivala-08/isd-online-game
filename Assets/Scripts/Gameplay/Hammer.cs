// Hammer.cs  --  the physics hammer
// -----------------------------------------------------------------------------
// The physical hammer (Week 1 "physics" and Week 2 "inputs" rows).
//
//   * It follows the pointer around the board, and W A S D nudges it too, so the
//     keyboard works without a mouse.
//   * Space (or a click) lifts and DROPS it: a real Rigidbody falls under gravity,
//     so the swing is physics, not an animation.
//   * While it falls it looks for a mole under its head and calls Hit() on it.
//
// The hammer prefab is on the "Ignore Raycast" layer, so the pointer ray from
// HitRaycaster passes straight through it and never blocks a click on a mole.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WhackAMole.Core;

namespace WhackAMole.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class Hammer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera used to turn the pointer position into a spot on the board.")]
        [SerializeField] private Camera cam;
        [Tooltip("The heavy part that actually hits the moles.")]
        [SerializeField] private Transform head;

        [Header("Follow")]
        [Tooltip("Height above the board where the hammer waits.")]
        [SerializeField] private float hoverHeight = 1.55f;
        [Tooltip("How quickly the hammer slides towards the pointer.")]
        [SerializeField] private float followSpeed = 14f;
        [Tooltip("Keyboard nudge speed in metres per second (W A S D).")]
        [SerializeField] private float keyboardSpeed = 7f;
        [Tooltip("Half the board size; the hammer never leaves this square.")]
        [SerializeField] private float boardLimit = 5.5f;

        [Header("Swing")]
        [Tooltip("Radius around the hammer head that counts as a hit.")]
        [SerializeField] private float hitRadius = 0.55f;
        [Tooltip("Seconds the hammer stays down before it is lifted again.")]
        [SerializeField] private float downTime = 0.22f;
        [Tooltip("Extra downward speed given at the start of a swing.")]
        [SerializeField] private float swingKick = 7f;

        private Rigidbody body;
        private RigidbodyConstraints restingConstraints;
        private Vector3 targetPosition;
        private Vector3 keyboardOffset;   // WASD nudge, kept on top of the pointer position
        private bool swinging;
        private Coroutine swingRoutine;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            // Only rotation is frozen while hovering: the Y position is set every frame by
            // the follow code, so freezing it would stop the hammer from ever moving up.
            restingConstraints = RigidbodyConstraints.FreezeRotation;

            if (cam == null) cam = Camera.main;
            if (head == null) head = transform;

            // The hammer hangs in the air until it is dropped, so it ignores gravity
            // and cannot rotate while it follows the pointer.
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = restingConstraints;
            targetPosition = transform.position;
        }

        private void Update()
        {
            // Do not swing outside a running round.
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

            UpdateTarget();
            if (WasSwingPressed() && !swinging) Swing();
        }

        /// <summary>Work out where the hammer should hover: under the pointer, nudged by WASD.</summary>
        private void UpdateTarget()
        {
            if (cam == null) cam = Camera.main;

            Vector3 point = targetPosition;
            if (cam != null && PointerScreenPosition(out Vector2 screen))
            {
                // A horizontal plane at hover height: the pointer is projected onto it.
                var plane = new Plane(Vector3.up, new Vector3(0f, hoverHeight, 0f));
                var ray = cam.ScreenPointToRay(screen);
                if (plane.Raycast(ray, out float distance))
                {
                    point = ray.GetPoint(distance);
                }
            }

            // Keyboard nudge (works even when the pointer is outside the game view).
            if (Keyboard.current != null)
            {
                var keys = Keyboard.current;
                float x = (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f)
                        - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f);
                float z = (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f)
                        - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f);
                // The nudge is remembered, not applied once: otherwise the pointer
                // position would overwrite it on the very next frame.
                if (x != 0f || z != 0f)
                    keyboardOffset += new Vector3(x, 0f, z) * (keyboardSpeed * Time.deltaTime);
                point += keyboardOffset;
            }

            point.y = hoverHeight;
            point.x = Mathf.Clamp(point.x, -boardLimit, boardLimit);
            point.z = Mathf.Clamp(point.z, -boardLimit, boardLimit);
            targetPosition = point;
        }

        private void FixedUpdate()
        {
            if (swinging) return;

            // MovePosition keeps the hammer smooth and still lets the physics engine
            // resolve the drop later. While the hammer waits it is kinematic.
            Vector3 next = Vector3.Lerp(transform.position, targetPosition,
                                        1f - Mathf.Exp(-followSpeed * Time.fixedDeltaTime));
            body.MovePosition(next);
        }

        /// <summary>Lift, drop and hit, then return to the hover height.</summary>
        private void Swing()
        {
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = StartCoroutine(SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            swinging = true;

            // 1. Lift a little, so the drop is visible.
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            transform.position = new Vector3(targetPosition.x, hoverHeight + 0.45f, targetPosition.z);
            yield return null;

            // 2. Release: gravity does the work from here.
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.down * swingKick;

            // 3. Watch the fall and hit anything under the head on the way down.
            float guard = 0f;
            while (guard < 1.5f)
            {
                guard += Time.deltaTime;

                if (TryHitUnderHead()) break;
                if (head.position.y <= 0.30f) break;        // reached the board/ground
                yield return null;
            }

            // 4. Stay down for a moment, then float back up.
            yield return new WaitForSeconds(downTime);

            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = restingConstraints;
            while (transform.position.y < hoverHeight - 0.02f)
            {
                body.MovePosition(Vector3.Lerp(transform.position,
                    new Vector3(targetPosition.x, hoverHeight, targetPosition.z), 0.35f));
                yield return null;
            }

            swinging = false;
            swingRoutine = null;
        }

        /// <summary>True if a mole or bomb was hit by this swing.</summary>
        private bool TryHitUnderHead()
        {
            if (head == null) return false;

            var found = Physics.OverlapSphere(head.position, hitRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var col in found)
            {
                // The hammer itself is on the same object graph, so skip its own colliders.
                if (col.transform.IsChildOf(transform)) continue;

                var occupant = col.GetComponentInParent<HoleOccupant>();
                if (occupant != null && occupant.IsHittable)
                {
                    occupant.Hit();
                    return true;
                }
            }
            return false;
        }

        /// <summary>The pointer position in screen pixels, if there is a pointer at all.</summary>
        private bool PointerScreenPosition(out Vector2 screen)
        {
            if (Mouse.current != null)
            {
                screen = Mouse.current.position.ReadValue();
                return true;
            }
            if (Touchscreen.current != null)
            {
                screen = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            screen = Vector2.zero;
            return false;
        }

        /// <summary>
        /// Only Space swings the hammer. A mouse click or tap is handled by
        /// HitRaycaster, which scores the mole under the pointer at once; keeping the
        /// two apart means one click is never counted twice.
        /// </summary>
        private bool WasSwingPressed()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        private void OnDisable()
        {
            // Leaving play mode in the middle of a swing must not leave it stuck down.
            if (swingRoutine != null) StopCoroutine(swingRoutine);
            swingRoutine = null;
            swinging = false;
        }
    }
}