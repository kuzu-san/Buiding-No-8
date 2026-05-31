using UnityEngine;
using OfficeGame;

/// <summary>
/// Центральный менеджер игры.
/// Хранит состояние (запущена/пауза/конец), считает игровое время,
/// вызывает победу и поражение.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGameRunning { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;

    [Header("Время игры")]
    [Tooltip("Реальная продолжительность сессии в секундах")]
    public float sessionDurationSeconds = 600f;

    [Tooltip("Начальный игровой час (9 = 09:00)")]
    public int gameStartHour = 9;

    [Tooltip("Конечный игровой час (18 = 18:00)")]
    public int gameEndHour = 18;

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

    /// <summary>
    /// Каждый кадр увеличивает игровое время.
    /// Когда время вышло — вызывает победу.
    /// </summary>
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

        _gameOver = false;
        IsGameRunning = true;
        IsPaused = false;
        Time.timeScale = 1f;

        var diff = GameDifficulty.Current?.level.ToString() ?? "Unknown";
        Debug.Log($"[GameManager] Игра запущена. Сложность: {diff}");
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

    public void TriggerVictory()
    {
        if (_gameOver) return;

        _gameOver = true;
        IsGameRunning = false;
        Debug.Log("[GameManager] ПОБЕДА!");
        ReturnToMenu(isVictory: true);
    }

    public void TriggerDefeat()
    {
        if (_gameOver) return;

        _gameOver = true;
        IsGameRunning = false;
        Debug.Log("[GameManager] ПОРАЖЕНИЕ!");
        ReturnToMenu(isVictory: false);
    }

    /// <summary>
    /// Возвращает камеру в главное меню и показывает экран результата.
    /// Вызывается из TriggerVictory и TriggerDefeat.
    /// </summary>
    public void ReturnToMenu(bool isVictory)
    {
        Time.timeScale = 1f;

        // Выключаем все события на этажах и сбрасываем рандомайзер
        var randomizer = FindAnyObjectByType<ObjectRandomizer>();
        if (randomizer != null)
        {
            randomizer.DisableAll();
            randomizer.ResetState();
        }

        FindAnyObjectByType<CameraWaypointController>()?.ForceReturnToMenu();
        TVMenuController.Instance?.ResetToInitialState(isVictory);

        ResetSession();
        Debug.Log("[GameManager] Возврат в главное меню.");
    }

    /// <summary>
    /// Возвращает в главное меню без показа экрана результата.
    /// Используется при нажатии "Выход" во время паузы.
    /// </summary>
    public void ReturnToMenuClean()
    {
        if (_gameOver) return;

        _gameOver = true;
        IsGameRunning = false;
        Time.timeScale = 1f;

        var randomizer = FindAnyObjectByType<ObjectRandomizer>();
        if (randomizer != null) { randomizer.DisableAll(); randomizer.ResetState(); }

        FindAnyObjectByType<CameraWaypointController>()?.ForceReturnToMenu();
        TVMenuController.Instance?.ResetToInitialState(showResult: false);

        ResetSession();
        Debug.Log("[GameManager] Выход в главное меню.");
    }
    private void ResetSession()
    {
        _elapsed = 0f;
        _gameOver = false;
    }
}