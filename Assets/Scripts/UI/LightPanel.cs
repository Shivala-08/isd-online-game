// LightPanel.cs  --  the day and night switch
// -----------------------------------------------------------------------------
// The day / night switch (Week 1 "lights" row: directional, point and spot lights).
//
// One button in the HUD flips the scene between:
//   Day   - a bright sun, soft ambient light, the point and spot lamps OFF
//   Night - a dim blue moon, dark ambient, the point and spot lamps ON
//
// It changes the real Light components and the RenderSettings (ambient + fog), and
// it is built by SceneBuilder, which also creates the sun, the point light and the
// spot light.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WhackAMole.UI
{
    public class LightPanel : MonoBehaviour
    {
        [Header("Lights")]
        [SerializeField] private Light sun;          // the directional light
        [SerializeField] private Light pointLamp;    // small warm lamp on the board
        [SerializeField] private Light spotLamp;     // spot light above the podium

        [Header("Day")]
        [SerializeField] private Color daySunColor = new Color(1f, 0.96f, 0.87f);
        [SerializeField] private float daySunIntensity = 1.25f;
        [SerializeField] private Color dayFogColor = new Color(0.72f, 0.85f, 0.96f);
        [SerializeField] private float dayAmbient = 1.05f;

        [Header("Night")]
        [SerializeField] private Color nightSunColor = new Color(0.55f, 0.65f, 1f);
        [SerializeField] private float nightSunIntensity = 0.28f;
        [SerializeField] private Color nightFogColor = new Color(0.07f, 0.09f, 0.16f);
        [SerializeField] private float nightAmbient = 0.35f;

        [Header("Sky")]
        [Tooltip("The scene skybox material. It is copied at start, so tonight's sky, tomorrow's asset.")]
        [SerializeField] private Material skyMaterial;
        [SerializeField] private float daySkyExposure = 1.2f;
        [SerializeField] private float nightSkyExposure = 0.35f;

        [Header("UI")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleLabel;

        private bool isNight;
        private Material skyInstance;      // runtime copy of the skybox

        private void Awake()
        {
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        }

        private void Start()
        {
            // Work on a copy: writing the exposure into the material asset would change
            // the skybox for good, even after leaving Play mode.
            if (skyMaterial != null)
            {
                skyInstance = new Material(skyMaterial) { name = skyMaterial.name + " (runtime)" };
                RenderSettings.skybox = skyInstance;
            }

            Apply(false);   // always open in daylight
        }

        private void OnDestroy()
        {
            // Put the asset back and drop the copy.
            if (skyInstance != null)
            {
                RenderSettings.skybox = skyMaterial;
                Destroy(skyInstance);
            }
        }

        /// <summary>Flip between day and night.</summary>
        public void Toggle() => Apply(!isNight);

        private void Apply(bool night)
        {
            isNight = night;

            if (sun != null)
            {
                sun.color = night ? nightSunColor : daySunColor;
                sun.intensity = night ? nightSunIntensity : daySunIntensity;
            }

            // The extra lamps are the "point and spot light" demo, so they are switched
            // on only at night; during the day the sun is enough.
            if (pointLamp != null) pointLamp.enabled = night;
            if (spotLamp != null) spotLamp.enabled = night;

            RenderSettings.fogColor = night ? nightFogColor : dayFogColor;
            RenderSettings.ambientIntensity = night ? nightAmbient : dayAmbient;

            // The skybox is most of the light in a skybox-ambient scene, so dimming it is
            // what actually makes the board look like night.
            if (skyInstance != null && skyInstance.HasProperty("_Exposure"))
                skyInstance.SetFloat("_Exposure", night ? nightSkyExposure : daySkyExposure);

            if (toggleLabel != null) toggleLabel.text = night ? "DAY" : "NIGHT";
        }
    }
}