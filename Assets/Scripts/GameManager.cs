using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Центральный менеджер игры.
/// Управляет сложностью, игровым временем,
/// состоянием паузы, победой и поражением.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum Difficulty { Easy, Normal, Hard }
    public Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

    public bool IsDifficultyLocked => _sessionStarted;


    [Header("Время игры")]
    [Tooltip("Реальная продолжительность сессии в секундах (600 = 10 минут)")]
    public float sessionDurationSeconds = 600f;

    [Tooltip("Начальный игровой час (9 = 09:00)")]
    public int gameStartHour = 9;

    [Tooltip("Конечный игровой час (18 = 18:00)")]
    public int gameEndHour = 18;

    public bool IsGameRunning { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;

    private bool _sessionStarted = false;
    private bool _gameOver = false;
    private float _elapsed = 0f;
    public float SessionProgress => Mathf.Clamp01(_elapsed / sessionDurationSeconds);

    public (int hour, int minute) CurrentGameTime
    {
        get
        {
            int totalMinutes = (int)(SessionProgress * (gameEndHour - gameStartHour) * 60f);
            return (gameStartHour + totalMinutes / 60, totalMinutes % 60);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (!IsGameRunning || _gameOver) return;

        _elapsed += Time.deltaTime;

        if (_elapsed >= sessionDurationSeconds)
            TriggerVictory();
    }

    public void StartGame()
    {
        if (_gameOver) ResetSession();

        _sessionStarted = true;
        _gameOver = false;
        IsGameRunning = true;
        IsPaused = false;
        Time.timeScale = 1f;

        Debug.Log($"[GameManager] Игра запущена. Сложность: {CurrentDifficulty}");
    }
    public void PauseGame()
    {
        if (!IsGameRunning) return;
        IsGameRunning = false;
        IsPaused = true;

        FindAnyObjectByType<CameraWaypointController>()?.OnPause();
        Debug.Log("[GameManager] Пауза");
    }
    public void ResumeGame()
    {
        if (!IsPaused) return;
        IsGameRunning = true;
        IsPaused = false;

        FindAnyObjectByType<CameraWaypointController>()?.OnResume();
        Debug.Log("[GameManager] Игра возобновлена");
    }

    public bool SetDifficulty(Difficulty diff)
    {
        if (IsDifficultyLocked)
        {
            Debug.LogWarning("[GameManager] Нельзя менять сложность во время игры.");
            return false;
        }
        CurrentDifficulty = diff;
        Debug.Log($"[GameManager] Сложность: {diff}");
        return true;
    }

    public void TriggerVictory()
    {
        if (_gameOver) return;
        _gameOver = true; IsGameRunning = false;
        Debug.Log("[GameManager] ПОБЕДА!");
        ReturnToMenu();
    }

    public void TriggerDefeat()
    {
        if (_gameOver) return;
        _gameOver = true; IsGameRunning = false;
        Debug.Log("[GameManager] ПОРАЖЕНИЕ!");
        ReturnToMenu();
    }

    /// <summary>
    /// Возвращает камеру в главное меню и сбрасывает состояние кнопок телевизора.
    /// </summary>
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        var cam = FindAnyObjectByType<CameraWaypointController>();
        if (cam != null)
        {
            cam.ForceReturnToMenu();
        }

        TVMenuController.Instance?.ResetToInitialState();

        ResetSession();
        Debug.Log("[GameManager] Возврат в главное меню.");
    }
    private void ResetSession()
    {
        _elapsed = 0f;
        _gameOver = false;
        _sessionStarted = false;
    }
}