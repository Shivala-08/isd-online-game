// Mole.cs  --  the mole: +1 point, hit effects, body colour
// -----------------------------------------------------------------------------
// A friendly mole. Hitting it is worth +1 point (TRD F4).
// All the movement lives in HoleOccupant; this script only says what a hit is
// worth (score) and what plays (sparks + a floating "+1"), and it reports its body
// renderer so the in-game material switcher can recolour every mole at once.
// -----------------------------------------------------------------------------

using WhackAMole.Core;
using WhackAMole.UI;
using UnityEngine;

namespace WhackAMole.Gameplay
{
    public class Mole : HoleOccupant
    {
        [Header("Scoring")]
        [Tooltip("Points for hitting a mole.")]
        [SerializeField] private int points = 1;

        [Tooltip("Colour of the floating '+1' label.")]
        [SerializeField] private Color popupColor = new Color(0.45f, 1f, 0.45f);

        [Tooltip("The renderer that the material switcher recolours (the body sphere).")]
        [SerializeField] private Renderer bodyRenderer;

        private void OnEnable()
        {
            // A mole spawned after the player changed the colour must get that colour too.
            if (MaterialSwapper.CurrentBodyMaterial != null)
                ApplyLook(MaterialSwapper.CurrentBodyMaterial);
        }

        protected override void OnHit()
        {
            // Score through the GameManager so the "never below zero" rule stays in one place.
            if (GameManager.Instance != null) GameManager.Instance.AddScore(points);
            Debug.Log("[Mole] hit in hole " + HoleIndex + " (+" + points + ")");
        }

        protected override void PlayHitEffects(Vector3 worldPosition)
        {
            if (HitEffects.Instance == null) return;
            HitEffects.Instance.MoleHit(worldPosition);
            HitEffects.Instance.ScoreLabel(worldPosition, "+" + points, popupColor);
        }

        /// <summary>Change the colour of the mole's body (used by MaterialSwapper).</summary>
        public override void ApplyLook(Material bodyMaterial)
        {
            if (bodyRenderer == null || bodyMaterial == null) return;
            bodyRenderer.sharedMaterial = bodyMaterial;
        }
    }
}