// Bomb.cs  --  the bomb: a penalty and an explosion
// -----------------------------------------------------------------------------
// A bomb looks like a mole but must NOT be hit: it costs points (TRD F10).
// The score can never fall below zero because GameManager.AddScore takes care of it.
// On a hit the bomb explodes (particles + sound + camera shake) and shows a red "-2".
// -----------------------------------------------------------------------------

using WhackAMole.Core;
using UnityEngine;

namespace WhackAMole.Gameplay
{
    public class Bomb : HoleOccupant
    {
        [Header("Scoring")]
        [Tooltip("Points taken away when the player hits a bomb.")]
        [SerializeField] private int penalty = 2;

        [Tooltip("Colour of the floating '-2' label.")]
        [SerializeField] private Color popupColor = new Color(1f, 0.4f, 0.35f);

        protected override void OnHit()
        {
            if (GameManager.Instance != null) GameManager.Instance.AddScore(-penalty);
            Debug.Log("[Bomb] hit in hole " + HoleIndex + " (-" + penalty + ")");
        }

        protected override void PlayHitEffects(Vector3 worldPosition)
        {
            if (HitEffects.Instance == null) return;
            HitEffects.Instance.BombHit(worldPosition);       // explosion + shake
            HitEffects.Instance.ScoreLabel(worldPosition, "-" + penalty, popupColor);
        }
    }
}