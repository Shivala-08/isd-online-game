// HitEffects.cs  --  every effect a hit can play
// -----------------------------------------------------------------------------
// One place that owns every "juicy" effect, so gameplay scripts only say WHAT
// happened and never HOW it is drawn:
//
//   RiseDust(pos)   - a small dust puff when a mole or bomb pops out
//   MoleHit(pos)    - the spark burst, the "+1" label, a click sound, a small shake
//   BombHit(pos)    - an explosion, the "-2" label, a boom, a big shake
//   Confetti()      - the confetti shower for a new high score
//
// It is a scene singleton (HitEffects.Instance) built by SceneBuilder, with every
// particle prefab, sound and camera reference set in the Inspector.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;
using WhackAMole.UI;

namespace WhackAMole.Gameplay
{
    public class HitEffects : MonoBehaviour
    {
        public static HitEffects Instance { get; private set; }

        [Header("Particle prefabs")]
        [SerializeField] private GameObject hitBurstPrefab;   // sparks when a mole is hit
        [SerializeField] private GameObject explosionPrefab;  // orange blast for a bomb
        [SerializeField] private GameObject dustPrefab;       // dirt puff when something rises
        [SerializeField] private GameObject confettiPrefab;   // new high score celebration

        [Header("Floating label")]
        [SerializeField] private ScorePopup scorePopupPrefab;
        [SerializeField] private float popupHeight = 1.1f;

        [Header("Sounds")]
        [SerializeField] private AudioClip popSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip bombSound;
        [SerializeField] private AudioClip cheerSound;
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.75f;

        [Header("Camera shake")]
        [Tooltip("Component on the camera that is allowed to shake. Built by SceneBuilder.")]
        [SerializeField] private CameraShaker cameraShaker;
        [SerializeField] private float moleShake = 0.05f;     // metres
        [SerializeField] private float bombShake = 0.22f;

        private AudioSource audioSource;

        private void Awake()
        {
            Instance = this;

            // Reuse one AudioSource for every effect: cheaper than one per spawn,
            // and PlayOneShot can overlap sounds.
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;   // 2D: the sounds are not positioned in the scene
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // =====================================================================
        //  Public effects
        // =====================================================================

        /// <summary>A dust puff at the hole when something starts to rise.</summary>
        public void RiseDust(Vector3 worldPosition)
        {
            Spawn(dustPrefab, worldPosition + Vector3.up * 0.1f);
            Play(popSound, 0.55f);
        }

        /// <summary>Sparks, "+1" and a light shake where a mole was hit.</summary>
        public void MoleHit(Vector3 worldPosition)
        {
            Spawn(hitBurstPrefab, worldPosition + Vector3.up * 0.5f);
            Play(hitSound, 1f);
            if (cameraShaker != null) cameraShaker.Shake(moleShake, 0.12f);
        }

        /// <summary>Explosion, "-2" and a strong shake where a bomb was hit.</summary>
        public void BombHit(Vector3 worldPosition)
        {
            Spawn(explosionPrefab, worldPosition + Vector3.up * 0.5f);
            Play(bombSound, 1f);
            if (cameraShaker != null) cameraShaker.Shake(bombShake, 0.35f);
        }

        /// <summary>The floating "+1" / "-2" label.</summary>
        public void ScoreLabel(Vector3 worldPosition, string text, Color color)
        {
            if (scorePopupPrefab == null) return;
            var popup = Instantiate(scorePopupPrefab, worldPosition + Vector3.up * popupHeight,
                                    scorePopupPrefab.transform.rotation);
            popup.Show(text, color);
        }

        /// <summary>Confetti over the whole board, used for a new high score.</summary>
        public void Confetti(Vector3 worldPosition)
        {
            Spawn(confettiPrefab, worldPosition + Vector3.up * 4f);
            Play(cheerSound, 1f);
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        /// <summary>
        /// Instantiate one effect prefab. Nothing is returned or tracked: each particle
        /// prefab destroys its own GameObject when its last particle dies
        /// (ParticleSystemStopAction.Destroy).
        /// </summary>
        private void Spawn(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null) return;
            Instantiate(prefab, worldPosition, prefab.transform.rotation);
        }

        private void Play(AudioClip clip, float scale)
        {
            if (audioSource == null || clip == null) return;
            audioSource.PlayOneShot(clip, volume * scale);
        }
    }
}