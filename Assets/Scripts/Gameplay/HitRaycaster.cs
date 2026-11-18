// HitRaycaster.cs  --  turns a click or tap into a hit
// -----------------------------------------------------------------------------
// Turns a mouse click or a touch into a hit on a mole or a bomb.
// It shoots one ray from the camera through the pointer position (TRD section 6
// "Hit detection") and asks the first thing under the cursor whether it has a
// HoleOccupant in its parents (the collider sits on the prefab root).
//
// This project uses the NEW Input System only (activeInputHandler = 1), so we
// read Mouse.current / Touchscreen.current instead of Input.GetMouseButtonDown,
// which would throw an exception here.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using WhackAMole.Core;

namespace WhackAMole.Gameplay
{
    public class HitRaycaster : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The camera raycasts are shot from. Defaults to Camera.main.")]
        [SerializeField] private Camera cam;

        [SerializeField] private float maxDistance = 200f;

        private void Start()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[HitRaycaster] No camera found. Disabling input.");
                enabled = false;
            }
        }

        private void Update()
        {
            // Only hits that count are scored during a running round (TRD T3/T4).
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (!PointerPressed()) return;

            Vector2 pointer = GetPointerPosition();
            int pointerId = GetPointerId();

            // Let the UI swallow clicks on buttons (the pause and menu buttons in M3).
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(pointerId)) return;

            // A ray from the camera through the cursor.
            var ray = cam.ScreenPointToRay(pointer);
            if (!Physics.Raycast(ray, out var hit, maxDistance)) return;

            // The collider that was hit belongs to a Mole or Bomb prefab root.
            var occupant = hit.collider.GetComponentInParent<HoleOccupant>();
            if (occupant != null) occupant.Hit();
        }

        /// <summary>True on the frame the mouse or a finger is first pressed.</summary>
        private bool PointerPressed()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        /// <summary>Cursor position in screen pixels (primary finger on a touch screen).</summary>
        private Vector2 GetPointerPosition()
        {
            // Use the mouse position whenever a mouse exists. Do not check IsActuated():
            // before the pointer is first moved the position reads (0,0).
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        /// <summary>The screen-centre ray used by gaze (VR) is a separate script in M7.</summary>
        private int GetPointerId()
        {
            // -1 is the mouse pointer; a touch needs its own finger id for the UI check.
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return Touchscreen.current.primaryTouch.touchId.ReadValue();
            return -1;
        }
    }
}