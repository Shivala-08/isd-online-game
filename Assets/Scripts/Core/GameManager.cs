// GameManager.cs  --  round state, score and timer
// -----------------------------------------------------------------------------
// The "brain" of the game. It owns the round: the score, the countdown timer,
// the current state (Menu / Playing / Paused / GameOver) and the high score.
// Other scripts never change the score directly; they call AddScore(), so all the
// rules (never below zero, save the high score) live in one place.
//
// It is a singleton (GameManager.Instance) so small scripts like Mole can find it
// without dragging references through the scene.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace WhackAMole.Core
{
    /// <summary>The four screens of the game.</summary>
    public enum GameState
    {
        Menu,      // title screen, nothing running
        Playing,   // moles spawn, timer runs
        Paused,    // timer frozen
        GameOver   // round finished, results shown
    }

    public class GameManager : MonoBehaviour
    {
        // --- Singleton ------------------------------------------------------
        public static GameManager Instance { get; private set; }

        [Header("Round settings")]
        [Tooltip("Length of one round in seconds.")]
        [SerializeField] private float roundTime = 30f;

        [Tooltip("Start the round automatically when Play is pressed (used until the main menu exists).")]
        [SerializeField] private bool autoStart = true;

        // --- State that other scripts read ----------------------------------
        public GameState State { get; private set; } = GameState.Menu;
        public int Score { get; private set; }
        public float TimeLeft { get; private set; }
        public int HighScore { get; private set; }
        public float RoundTime => roundTime;

        /// <summary>0 at the start of the round, 1 at the end. Drives the difficulty curve.</summary>
        public float Progress => roundTime <= 0f ? 0f : 1f - Mathf.Clamp01(TimeLeft / roundTime);

        // --- Events the UI listens to (C# events = no polling in Update) -----
        public event Action<int> ScoreChanged;            // new score
        public event Action<float> TimeChanged;           // seconds left
        public event Action<GameState> StateChanged;      // new state
        public event Action<int, int, bool> RoundOver;    // score, high score, isNewRecord

        /// <summary>
        /// Fired every time a round starts, including a restart in the middle of a round.
        /// StateChanged cannot be used for that case, because Playing -> Playing is not a
        /// change, so MoleSpawner listens to this event to clear the field and restart.
        /// </summary>
        public event Action RoundStarted;

        private const string HighScoreKey = "WhackAMole.HighScore"; // PlayerPrefs key

        private void Awake()
        {
            // Simple singleton: the first GameManager wins, extra copies remove themselves.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // The high score survives between runs because it is stored in PlayerPrefs.
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        private void Start()
        {
            if (autoStart) StartGame();
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            // Count the round down, in real seconds (Time.deltaTime).
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                TimeChanged?.Invoke(TimeLeft);
                EndGame();
                return;
            }
            TimeChanged?.Invoke(TimeLeft);
        }

        // =====================================================================
        //  Round control
        // =====================================================================

        /// <summary>Reset the score and timer and start spawning moles.</summary>
        public void StartGame()
        {
            Score = 0;
            TimeLeft = roundTime;

            // A restart during Pause left the clock frozen; always run at normal speed.
            Time.timeScale = 1f;

            ScoreChanged?.Invoke(Score);
            TimeChanged?.Invoke(TimeLeft);
            SetState(GameState.Playing);
            RoundStarted?.Invoke();   // also fires on a mid-round restart
        }

        /// <summary>Restart button and the R key both use this.</summary>
        public void Restart() => StartGame();

        /// <summary>Freeze the round (pause menu, M3).</summary>
        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
            Time.timeScale = 0f; // stops Physics and WaitForSeconds too
        }

        /// <summary>Continue a paused round.</summary>
        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Add (or subtract) points. The score can never go below zero, which is the
        /// rule from the TRD for hitting a bomb.
        /// </summary>
        public void AddScore(int amount)
        {
            if (State != GameState.Playing) return;
            Score = Mathf.Max(0, Score + amount);
            ScoreChanged?.Invoke(Score);
        }

        private void EndGame()
        {
            // Guard: EndGame can only end a round that is actually running, so the
            // game over panel and the PlayerPrefs write happen exactly once.
            if (State != GameState.Playing) return;

            SetState(GameState.GameOver);
            Time.timeScale = 1f;

            bool isRecord = Score > HighScore;
            if (isRecord)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
            }

            RoundOver?.Invoke(Score, HighScore, isRecord);
        }

        private void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(State);
        }
    }
}
