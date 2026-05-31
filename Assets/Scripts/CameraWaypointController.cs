using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Управляет перемещением главной камеры между контрольными точками (waypoints).
/// </summary>
public class CameraWaypointController : MonoBehaviour
{
    [Serializable]
    public class CameraWaypoint
    {
        [Tooltip("Произвольное имя точки (для удобства в инспекторе)")]
        public string label = "Waypoint";

        [Tooltip("Мировые координаты камеры")]
        public Vector3 position;

        [Tooltip("Углы поворота камеры (Euler: X = pitch, Y = yaw, Z = roll)")]
        public Vector3 rotation;

        [Tooltip("Угол обзора (Field of View), градусы")]
        [Range(1f, 179f)]
        public float fieldOfView = 60f;
    }

    [Header("Контрольные точки")]
    public CameraWaypoint[] waypoints;

    [Header("Плавность перехода")]
    [Tooltip("Продолжительность перехода между точками, секунды")]
    [Min(0.01f)]
    public float transitionDuration = 1.5f;

    [Tooltip("Кривая скорости интерполяции (S-образная для плавности)")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Управление (новый Input System)")]
    [Tooltip("Клавиша перехода к следующей точке")]
    public Key nextKey = Key.D;

    [Tooltip("Клавиша перехода к предыдущей точке")]
    public Key prevKey = Key.A;

    [Header("Главный экран")]
    [Tooltip("Индекс waypoint главного меню. Камера стартует здесь.")]
    [Min(0)]
    public int startIndex = 0;

    private Camera _cam;
    private int _currentIndex;
    private Coroutine _transitionCoroutine;

    private bool _ispaused = false;
    private int _indexBeforePause = 1;


    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;

        if (_cam == null)
        {
            Debug.LogError("Camera не найдена ни на объекте, ни в дочерних, ни как Camera.main!", this);
            enabled = false;
            return;
        }
        Debug.Log($"Используется камера: {_cam.gameObject.name}", this);
    }

    /// <summary>
    /// Устанавливает камеру на стартовую точку мгновенно.
    /// Замораживает время — игра ждёт нажатия "Начать" в меню.
    /// </summary>
    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("Список контрольных точек пуст.", this);
            enabled = false;
            return;
        }
        _currentIndex = Mathf.Clamp(startIndex, 0, waypoints.Length - 1);
        ApplyWaypointInstant(waypoints[_currentIndex]);
        Time.timeScale = 0f;
        _ispaused = true;
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш перехода и паузы.
    /// </summary>
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[nextKey].wasPressedThisFrame && !_ispaused)
            GoToWaypoint(WrapIndex(_currentIndex + 1));
        else if (kb[prevKey].wasPressedThisFrame && !_ispaused)
            GoToWaypoint(WrapIndex(_currentIndex - 1));
        else if (kb[Key.Escape].wasPressedThisFrame && !_ispaused)
            GameManager.Instance?.PauseGame();
    }

    /// <summary>
    /// Плавно перемещает камеру к указанному waypoint.
    /// Блокирует переход на startIndex во время игры (только через Escape).
    /// </summary>
    public void GoToWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0) return;

        index = WrapIndex(index);

        if (index == startIndex && !_ispaused)
        {
            return;
        }

        if (index == _currentIndex) return;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _currentIndex = index;
        _transitionCoroutine = StartCoroutine(TransitionTo(waypoints[_currentIndex]));

        Debug.Log($"[CameraWaypointController] Переход к точке [{_currentIndex}] \"{waypoints[_currentIndex].label}\"");
    }

    /// <summary>
    /// Вызывается GameManager.PauseGame().
    /// Сохраняет текущий индекс, переводит камеру в меню, останавливает время.
    /// </summary>
    public void OnPause()
    {
        if (_ispaused) return;

        _indexBeforePause = _currentIndex;
        _ispaused = true;
        GoTomain();
        StartCoroutine(StopTimeAfterTransition());
        TVMenuController.Instance?.NotifyPaused();
    }

    /// <summary>
    /// Вызывается GameManager.ResumeGame().
    /// Возвращает камеру на точку где была до паузы.
    /// </summary>
    public void OnResume()
    {
        if (!_ispaused) return;

        _ispaused = false;
        Time.timeScale = 1f;
        GoToWaypoint(_indexBeforePause);
        TVMenuController.Instance?.NotifyResumed();
    }

    /// <summary>
    /// Вызывается TVMenuController при первом старте игры.
    /// Снимает паузу и переходит к геймплейной точке.
    /// </summary>
    public void StartFromMenu(int waypointIndex)
    {
        _ispaused = false;
        Time.timeScale = 1f;
        GoToWaypoint(waypointIndex);
        TVMenuController.Instance?.NotifyResumed();
    }

    /// <summary>
    /// Принудительный возврат в главное меню при окончании игры.
    /// Не сохраняет _indexBeforePause — возврата обратно не будет.
    /// </summary>
    public void ForceReturnToMenu()
    {
        _ispaused = true;

        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionTo(waypoints[startIndex]));
        _currentIndex = startIndex;

        StartCoroutine(StopTimeAfterTransition());
    }

    /// <summary>Переводит камеру на точку главного меню (startIndex).</summary>
    private void GoTomain()
    {
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionTo(waypoints[startIndex]));
    }

    /// <summary>
    /// Ждёт завершения перехода камеры, затем замораживает время.
    /// Используется при паузе и окончании игры.
    /// </summary>
    private IEnumerator StopTimeAfterTransition()
    {
        yield return new WaitForSecondsRealtime(transitionDuration);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Плавно перемещает камеру к целевому waypoint за transitionDuration секунд.
    /// Использует unscaledDeltaTime чтобы работать при Time.timeScale = 0.
    /// </summary>
    private IEnumerator TransitionTo(CameraWaypoint target)
    {
        Vector3    fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float      fromFov = _cam.fieldOfView;
        Quaternion toRot   = Quaternion.Euler(target.rotation);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t      = Mathf.Clamp01(elapsed / transitionDuration);
            float curved = easeCurve.Evaluate(t);

            transform.position  = Vector3.Lerp(fromPos, target.position, curved);
            transform.rotation  = Quaternion.Slerp(fromRot, toRot, curved);
            _cam.fieldOfView    = Mathf.Lerp(fromFov, target.fieldOfView, curved);

            yield return null;
        }

        ApplyWaypointInstant(target);
    }

    /// <summary>Мгновенно применяет позицию, поворот и FOV waypoint без анимации.</summary>
    private void ApplyWaypointInstant(CameraWaypoint wp)
    {
        transform.position = wp.position;
        transform.rotation = Quaternion.Euler(wp.rotation);
        _cam.fieldOfView   = wp.fieldOfView;
    }

    /// <summary>Возвращает индекс в пределах массива waypoints (зацикливание).</summary>
    private int WrapIndex(int index)
    {
        int count = waypoints.Length;
        return ((index % count) + count) % count;
    }
}
