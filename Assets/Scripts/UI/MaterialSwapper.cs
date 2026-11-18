// MaterialSwapper.cs  --  the in-game material switch
// -----------------------------------------------------------------------------
// The in-game material switch (Week 1 "materials" row).
//
// One button in the HUD cycles the mole body through a small palette of materials
// that SceneBuilder creates. The chosen material is kept in a static field, so a
// mole that spawns later (a new prefab instance) is born in the same colour, and
// every mole already on the board is repainted immediately.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhackAMole.Gameplay;

namespace WhackAMole.UI
{
    public class MaterialSwapper : MonoBehaviour
    {
        /// <summary>The colour currently chosen. Moles read this when they appear.</summary>
        public static Material CurrentBodyMaterial { get; private set; }

        [Header("Palette")]
        [Tooltip("Body materials to cycle through. The first one is the default brown.")]
        [SerializeField] private Material[] palette;
        [SerializeField] private Button swapButton;
        [SerializeField] private TextMeshProUGUI swapLabel;

        private int index;

        private void Awake()
        {
            if (swapButton != null) swapButton.onClick.AddListener(NextMaterial);
        }

        private void Start()
        {
            if (palette == null || palette.Length == 0) return;

            // Open with the first colour (the normal brown mole).
            CurrentBodyMaterial = palette[0];
            ApplyToBoard();
            UpdateLabel();
        }

        /// <summary>Switch every mole to the next colour in the palette.</summary>
        public void NextMaterial()
        {
            if (palette == null || palette.Length == 0) return;

            index = (index + 1) % palette.Length;
            CurrentBodyMaterial = palette[index];
            ApplyToBoard();
            UpdateLabel();
        }

        /// <summary>Repaint the moles that are already in the scene.</summary>
        private void ApplyToBoard()
        {
            var moles = FindObjectsByType<Mole>(FindObjectsSortMode.None);
            foreach (var mole in moles) mole.ApplyLook(CurrentBodyMaterial);
        }

        private void UpdateLabel()
        {
            if (swapLabel == null) return;
            swapLabel.text = CurrentBodyMaterial != null ? CurrentBodyMaterial.name.Replace("Mole_Body_", "").Replace("Mole_Body", "BROWN") : "COLOUR";
        }
    }
}