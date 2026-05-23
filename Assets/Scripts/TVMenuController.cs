using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OfficeGame;

/// <summary>
/// Главное меню игры, отображаемое на экране телевизора.
/// </summary>
public class TVMenuController : MonoBehaviour
{
    public static TVMenuController Instance { get; private set; }

    [Header("Камера")]
    public CameraWaypointController cameraController;

    [Tooltip("Индекс waypoint геймплея (куда уходит камера при старте игры)")]
    public int gameplayWaypointIndex = 1;

    [Header("Сложность")]
    [Tooltip("Перетащите сюда ассет GameDifficulty из папки Assets")]
    public GameDifficulty difficultyAsset;

    [Header("Панели")]
    public GameObject panelMain;
    public GameObject panelDifficulty;


    [Header("Главное меню")]
    public Button btnStart;
    public Button btnDifficulty;
    public Button btnQuit;
    public TMP_Text textGameTitle;
    public string gameTitleText = "Office Nightmare";

    [Header("Меню сложности")]
    public Button btnEasy;
    public Button btnNormal;
    public Button btnHard;
    public Button btnBack;

    [Header("Цвета выбора сложности")]
    public Color colorSelected = new Color(1f, 0.85f, 0.2f);
    public Color colorUnselected = new Color(1f, 1f, 1f);

    private bool _gameEverStarted = false;
    private bool _isPaused = false;

    private enum Panel { Main, Difficulty }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (difficultyAsset != null)
            GameDifficulty.Current = difficultyAsset;
        else
            Debug.LogError("[TVMenuController] difficultyAsset не назначен в Inspector!", this);

        BindButtons();
    }

    private void Start()
    {
        if (textGameTitle != null) textGameTitle.text = gameTitleText;
        ShowPanel(Panel.Main);
        RefreshDiffHighlight();
        SetStartButtonLabel(false);
    }

    private void BindButtons()
    {
        btnStart?.onClick.AddListener(OnStartClicked);
        btnDifficulty?.onClick.AddListener(OnDifficultyClicked);
        btnQuit?.onClick.AddListener(OnQuitClicked);

        btnEasy?.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Level.Easy));
        btnNormal?.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Level.Normal));
        btnHard?.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Level.Hard));
        btnBack?.onClick.AddListener(() => ShowPanel(Panel.Main));
    }

    private void OnStartClicked()
    {
        if (_isPaused)
        {
            GameManager.Instance?.ResumeGame();
        }
        else if (!_gameEverStarted)
        {
            _gameEverStarted = true;
            SetStartButtonLabel(true);
            cameraController?.StartFromMenu(gameplayWaypointIndex);
            StartCoroutine(LaunchGameAfterTransition());
        }
    }

    private void OnDifficultyClicked()
    {
        if (_gameEverStarted)
        {
            StartCoroutine(FlashRed(btnDifficulty));
            Debug.Log("[TVMenu] Сложность нельзя изменить после начала игры.");
            return;
        }

        ShowPanel(Panel.Difficulty);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SelectDifficulty(GameDifficulty.Level level)
    {
        if (GameDifficulty.Current != null)
            GameDifficulty.Current.level = level;

        RefreshDiffHighlight();
        Debug.Log($"[TVMenu] Сложность: {level}");
    }
    /// <summary>
    /// Вызвать когда игра ставится на паузу.
    /// Переименовывает кнопку и блокирует сложность визуально.
    /// </summary>
    public void NotifyPaused()
    {
        _isPaused = true;
        ShowPanel(Panel.Main);
        SetStartButtonLabel(true);
        SetDiffButtonLocked(true);
    }

    /// <summary>
    /// Вызвать когда игра возобновляется.
    /// </summary>
    public void NotifyResumed()
    {
        _isPaused = false;
        SetDiffButtonLocked(_gameEverStarted);
    }
    private void ShowPanel(Panel panel)
    {
        if (panelMain) panelMain.SetActive(panel == Panel.Main);
        if (panelDifficulty) panelDifficulty.SetActive(panel == Panel.Difficulty);
    }

    /// <summary>Подсвечивает кнопку текущей сложности.</summary>
    private void RefreshDiffHighlight()
    {
        var cur = GameDifficulty.Current?.level ?? GameDifficulty.Level.Normal;
        SetBtnTint(btnEasy, cur == GameDifficulty.Level.Easy);
        SetBtnTint(btnNormal, cur == GameDifficulty.Level.Normal);
        SetBtnTint(btnHard, cur == GameDifficulty.Level.Hard);
    }

    private void SetBtnTint(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? colorSelected : colorUnselected;
    }

    /// <summary>Меняет надпись кнопки старта.</summary>
    private void SetStartButtonLabel(bool isResume)
    {
        var lbl = btnStart?.GetComponentInChildren<TMP_Text>();
        if (lbl != null) lbl.text = isResume ? "Продолжить" : "Начать игру";
    }

    /// <summary>Визуально блокирует / разблокирует кнопку сложности.</summary>
    private void SetDiffButtonLocked(bool locked)
    {
        if (btnDifficulty == null) return;
        var img = btnDifficulty.GetComponent<Image>();
        if (img != null) img.color = locked ? new Color(0.4f, 0.4f, 0.4f) : Color.white;
    }

    /// <summary>
    /// Сбрасывает меню в исходное состояние как при первом запуске игры.
    /// Вызывается при победе или поражении.
    /// </summary>
    public void ResetToInitialState()
    {
        _gameEverStarted = false;
        _isPaused = false;

        ShowPanel(Panel.Main);
        SetStartButtonLabel(false);
        SetDiffButtonLocked(false);
        RefreshDiffHighlight();
    }
    private IEnumerator FlashRed(Button btn)
    {
        if (btn == null) yield break;
        var img = btn.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        for (int i = 0; i < 3; i++)
        {
            img.color = Color.red;
            yield return new WaitForSecondsRealtime(0.12f);
            img.color = original;
            yield return new WaitForSecondsRealtime(0.12f);
        }
    }

    /// <summary>Ждёт завершения перехода камеры, затем запускает GameManager.</summary>
    private IEnumerator LaunchGameAfterTransition()
    {
        float dur = cameraController != null ? cameraController.transitionDuration : 1.5f;
        yield return new WaitForSecondsRealtime(dur);
        GameManager.Instance?.StartGame();
    }
}