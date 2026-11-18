// UIController.cs  --  menu cards, pause and restart
// -----------------------------------------------------------------------------
// Owns the three menu cards (Start, Pause, Game Over) and the keyboard shortcuts.
//
//   Start card   -> shown when the game opens, Play button starts a round
//   Pause card   -> Esc or P pauses and resumes, Resume button does the same
//   Game Over    -> shown when the round ends, Restart button starts a new round
//   R            -> restart at any time
//
// The cards themselves are prefab assets (Assets/Prefabs/UI) so they can be edited
// in the Editor without touching this script; this script only shows and hides them
// and calls the GameManager.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WhackAMole.Core;
using WhackAMole.Gameplay;

namespace WhackAMole.UI
{
    public class UIController : MonoBehaviour
    {
        [Header("Menu cards (prefab instances)")]
        [SerializeField] private GameObject startCard;
        [SerializeField] private GameObject pauseCard;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartFromPauseButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button cameraButton;

        [Header("Camera cycling")]
        [Tooltip("The on-screen CAMERA button uses the same cycler as the C key.")]
        [SerializeField] private CameraCycler cycler;

        [Header("Celebration")]
        [Tooltip("Where the confetti shower appears (center of the board).")]
        [SerializeField] private Vector3 confettiPosition = new Vector3(0f, 0f, 0f);

        private GameManager game;

        private void Awake()
        {
            game = GameManager.Instance;

            // Every button calls the GameManager through the singleton looked up at
            // click time, because the reference can be filled in later than Awake.
            Wire(playButton, () => Manager()?.StartGame());
            Wire(resumeButton, () => Manager()?.Resume());
            Wire(restartFromPauseButton, () => Manager()?.Restart());
            Wire(pauseButton, () => Manager()?.Pause());
            Wire(cameraButton, () =>
            {
                if (cycler != null) cycler.Cycle();
            });
        }

        private void Start()
        {
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogError("[UIController] GameManager.Instance is missing; menus will not work.");
                return;
            }

            game.StateChanged += OnStateChanged;
            game.RoundOver += OnRoundOver;

            // The game opens on the Start card (GameManager starts in the Menu state).
            ShowState(game.State);
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.StateChanged -= OnStateChanged;
            game.RoundOver -= OnRoundOver;
        }

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys == null) return;

            // Esc or P: pause and resume.
            if (keys.escapeKey.wasPressedThisFrame || keys.pKey.wasPressedThisFrame)
            {
                var manager = Manager();
                if (manager == null) return;
                if (manager.State == GameState.Playing) manager.Pause();
                else if (manager.State == GameState.Paused) manager.Resume();
            }

            // R: restart from anywhere (the TRD asks for it in the menu and in game).
            if (keys.rKey.wasPressedThisFrame) Manager()?.Restart();
        }

        private static GameManager Manager()
        {
            return GameManager.Instance;
        }

        private static void Wire(Button button, System.Action action)
        {
            if (button == null) return;
            button.onClick.AddListener(() => action());
        }

        private void OnStateChanged(GameState state) => ShowState(state);

        /// <summary>Exactly one card is visible, and never during play.</summary>
        private void ShowState(GameState state)
        {
            SetActive(startCard, state == GameState.Menu);
            SetActive(pauseCard, state == GameState.Paused);
        }

        private void OnRoundOver(int score, int highScore, bool isNewRecord)
        {
            // A new record gets a confetti shower over the board. The result card
            // itself belongs to HUDController, which owns the score labels.
            if (isNewRecord && HitEffects.Instance != null)
                HitEffects.Instance.Confetti(confettiPosition);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}