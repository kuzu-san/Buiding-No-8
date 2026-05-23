using UnityEngine;
using TMPro;

/// <summary>
/// Электронные часы на сцене.
/// Отображают текущее игровое время
/// </summary>
public class OfficeClock : MonoBehaviour
{
    [Header("Текстовый компонент")]
    [Tooltip("TextMeshPro компонент на циферблате (3D или UI)")]
    public TMP_Text timeText;

    [Header("Формат")]
    [Tooltip("Формат времени. HH = часы 00-23, mm = минуты")]
    public string timeFormat = "HH:mm";

    [Tooltip("Цвет текста в начале дня")]
    public Color morningColor = Color.cyan;

    [Tooltip("Цвет текста в конце дня (предупреждение)")]
    public Color eveningColor = Color.red;

    [Tooltip("С какого прогресса сессии начать переход к вечернему цвету (0..1)")]
    [Range(0f, 1f)]
    public float colorShiftStart = 0.7f;

    private void Awake()
    {
        if (timeText == null)
            timeText = GetComponentInChildren<TMP_Text>();

        if (timeText == null)
            Debug.LogError("[OfficeClock] TMP_Text не найден! Назначьте timeText в Inspector.", this);
    }

    private void Update()
    {
        if (GameManager.Instance == null || timeText == null) return;

        var (hour, minute) = GameManager.Instance.CurrentGameTime;
        timeText.text = FormatTime(hour, minute);

        float progress = GameManager.Instance.SessionProgress;
        float t = Mathf.InverseLerp(colorShiftStart, 1f, progress);
        timeText.color = Color.Lerp(morningColor, eveningColor, t);
    }

    private string FormatTime(int hour, int minute)
    {
        return timeFormat
            .Replace("HH", hour.ToString("D2"))
            .Replace("mm", minute.ToString("D2"));
    }
}
