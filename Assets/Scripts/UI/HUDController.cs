// HUDController.cs  --  score, time and best score on screen
// -----------------------------------------------------------------------------
// Draws the score, the timer and the high score in rounded panels at the top of
// the screen, and shows the Game Over card at the end of a round.
//
// In the last five seconds the timer turns red and pulses (grows and shrinks), so
// the player feels the pressure without any extra text.
//
// It never asks the GameManager anything; it subscribes to events instead
// (ScoreChanged, TimeChanged, RoundOver), which keeps the two scripts decoupled:
// the HUD can be rebuilt without touching the game logic.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhackAMole.Core;

namespace WhackAMole.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("HUD texts")]
        [SerializeField] private TextMeshProUGUI scoreText;       // "SCORE  12"
        [SerializeField] private TextMeshProUGUI timeText;        // "0:23" - also pulses
        [SerializeField] private TextMeshProUGUI highScoreText;   // "BEST  42"

        [Header("Game Over panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI finalScoreText;     // "Your score: 12"
        [SerializeField] private TextMeshProUGUI highScoreLineText;  // "New record!" or "Best: 42"
        [SerializeField] private Button restartButton;

        [Header("Timer pulse (last seconds of a round)")]
        [SerializeField] private float pulseBelowSeconds = 5f;   // start pulsing under 5 s
        [SerializeField] private Color normalTimeColor = Color.white;
        [SerializeField] private Color dangerTimeColor = new Color(1f, 0.22f, 0.18f);
        [SerializeField] private float pulseSpeed = 3f;          // full swings per second
        [SerializeField] private float pulseScale = 1.18f;       // how big the timer grows

        private GameManager game;
        private int lastShownSecond = -1;
        private bool pulsing;

        private void Awake()
        {
            game = GameManager.Instance;

            // Wire the Restart button in code: one click starts a fresh round.
            // GameManager.Instance is looked up again inside the click because the
            // singleton may not exist yet while this Awake runs.
            if (restartButton != null)
                restartButton.onClick.AddListener(() =>
                {
                    var manager = game != null ? game : GameManager.Instance;
                    if (manager != null) manager.Restart();
                });
        }

        private void Start()
        {
            // Subscribe in Start, not OnEnable: GameManager.Instance is set in Awake, and
            // every Awake runs before any Start, so the singleton is guaranteed to exist
            // here even though the order of Awake calls between objects is not fixed.
            game = GameManager.Instance;
            if (game == null)
            {
                Debug.LogError("[HUDController] GameManager.Instance is missing; the HUD will not update.");
                return;
            }

            game.ScoreChanged += OnScoreChanged;
            game.TimeChanged += OnTimeChanged;
            game.RoundOver += OnRoundOver;
            game.StateChanged += OnStateChanged;

            // Paint the initial values immediately (no need to wait for an event).
            OnScoreChanged(game.Score);
            OnTimeChanged(game.TimeLeft);
            if (highScoreText != null) highScoreText.text = "BEST  " + game.HighScore;
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.ScoreChanged -= OnScoreChanged;
            game.TimeChanged -= OnTimeChanged;
            game.RoundOver -= OnRoundOver;
            game.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (timeText == null) return;

            if (!pulsing)
            {
                // Idle: make sure the timer is back to its normal size and colour, even
                // if a round was restarted in the middle of a pulse.
                if (timeText.rectTransform.localScale != Vector3.one)
                    timeText.rectTransform.localScale = Vector3.one;
                return;
            }

            // PingPong swings 0 -> 1 -> 0, so scale and colour breathe together.
            // Unscaled time keeps the pulse alive while the game is paused.
            float t = Mathf.PingPong(Time.unscaledTime * pulseSpeed, 1f);
            float scale = Mathf.Lerp(1f, pulseScale, t);
            timeText.rectTransform.localScale = new Vector3(scale, scale, 1f);
            timeText.color = Color.Lerp(dangerTimeColor, Color.Lerp(dangerTimeColor, Color.white, 0.45f), t);
        }

        private void OnScoreChanged(int score)
        {
            if (scoreText != null) scoreText.text = "SCORE  " + score;
        }

        private void OnTimeChanged(float seconds)
        {
            // Should the timer be pulsing right now?
            bool danger = seconds > 0f && seconds <= pulseBelowSeconds
                          && (game == null || game.State == GameState.Playing);
            if (danger != pulsing)
            {
                pulsing = danger;
                if (timeText != null && !pulsing)
                {
                    // Leaving the danger zone: reset size and colour at once.
                    timeText.rectTransform.localScale = Vector3.one;
                    timeText.color = normalTimeColor;
                }
            }

            // The event arrives every frame; only touch the UI text when the whole
            // second changes, so the HUD does not rebuild its text 60 times a second.
            int whole = Mathf.CeilToInt(seconds);
            if (whole == lastShownSecond) return;
            lastShownSecond = whole;

            if (timeText != null)
                timeText.text = string.Format("{0}:{1:00}", whole / 60, whole % 60);
        }

        private void OnRoundOver(int score, int highScore, bool isRecord)
        {
            pulsing = false;
            if (timeText != null)
            {
                timeText.rectTransform.localScale = Vector3.one;
                timeText.color = normalTimeColor;
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (highScoreText != null) highScoreText.text = "BEST  " + highScore;
            if (finalScoreText != null) finalScoreText.text = "Your score: " + score;
            if (highScoreLineText != null)
                highScoreLineText.text = isRecord ? "New record! " + score : "Best: " + highScore;
        }

        private void OnStateChanged(GameState state)
        {
            // Hide the game over panel as soon as a new round starts.
            if (state == GameState.Playing && gameOverPanel != null) gameOverPanel.SetActive(false);
        }
    }
}
