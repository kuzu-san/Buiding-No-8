using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// Управление основной камерой
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
    [Tooltip("Список контрольных точек камеры")]
    public CameraWaypoint[] waypoints;

    [Header("Плавность перехода")]
    [Tooltip("Продолжительность перехода между точками, секунды")]
    [Min(0.01f)]
    public float transitionDuration = 1.5f;

    [Tooltip("Кривая скорости интерполяции (оставьте S-образную для плавности)")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Управление (новый Input System)")]
    [Tooltip("Клавиша перехода к следующей точке (по умолчанию: D)")]
    public Key nextKey = Key.D;

    [Tooltip("Клавиша перехода к предыдущей точке (по умолчанию: A)")]
    public Key prevKey = Key.A;

    [Header("Главный экран")]
    [Tooltip("Индекс точки, с которой начинается сцена")]
    [Min(0)]
    public int startIndex = 0;

    private Camera _cam;
    private int _currentIndex;
    private Coroutine _transitionCoroutine;
    private bool _ispaused = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();

        if (_cam == null)
            _cam = GetComponentInChildren<Camera>();

        if (_cam == null)
            _cam = Camera.main;

        if (_cam == null)
        {
            Debug.LogError("Camera не найдена ни на объекте, ни в дочерних, ни как Camera.main!", this);
            enabled = false;
            return;
        }

        Debug.Log($"Используется камера: {_cam.gameObject.name}", this);
    }
    private int _indexBeforePause = 1;

    public void OnPause()
    {
        if (_ispaused) return;
        _indexBeforePause = _currentIndex;
        _ispaused = true;
        if (_transitionCoroutine != null)
            StopCoroutine(_transitionCoroutine);

        _transitionCoroutine = StartCoroutine(TransitionTo(waypoints[startIndex]));
        StartCoroutine(StopTimeAfterTransition());
        TVMenuController.Instance?.NotifyPaused();
    }

    private IEnumerator StopTimeAfterTransition()
    {
        yield return new WaitForSecondsRealtime(transitionDuration);
        Time.timeScale = 0f;
    }
    public void OnResume()
    {
        if (!_ispaused) return;
        _ispaused = false;
        Time.timeScale = 1f;
        ApplyWaypointInstant(waypoints[_indexBeforePause]);
        TVMenuController.Instance?.NotifyResumed();
    }

    /// <summary>
    /// Принудительный возврат в главное меню по окончании игры.
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
    public void StartFromMenu(int waypointIndex)
    {
        _ispaused = false;
        Time.timeScale = 1f;
        GoToWaypoint(waypointIndex);
        TVMenuController.Instance?.NotifyResumed();
    }

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

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[nextKey].wasPressedThisFrame && !_ispaused)
            GoToWaypoint(WrapIndex(_currentIndex + 1));
        else if (kb[prevKey].wasPressedThisFrame && !_ispaused)
            GoToWaypoint(WrapIndex(_currentIndex - 1));
        else if (kb[Key.Escape].wasPressedThisFrame && !_ispaused)
        {
            GameManager.Instance?.PauseGame();
        }
    }

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

    private IEnumerator TransitionTo(CameraWaypoint target)
    {

        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float fromFov = _cam.fieldOfView;

        Quaternion toRot = Quaternion.Euler(target.rotation);

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            float curved = easeCurve.Evaluate(t);

            transform.position = Vector3.Lerp(fromPos, target.position, curved);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, curved);
            _cam.fieldOfView = Mathf.Lerp(fromFov, target.fieldOfView, curved);

            yield return null;
        }

        ApplyWaypointInstant(target);
    }

    private void ApplyWaypointInstant(CameraWaypoint wp)
    {
        transform.position = wp.position;
        transform.rotation = Quaternion.Euler(wp.rotation);
        _cam.fieldOfView = wp.fieldOfView;
    }
    private int WrapIndex(int index)
    {
        int count = waypoints.Length;
        return ((index % count) + count) % count;
    }
}