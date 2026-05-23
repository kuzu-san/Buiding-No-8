using System.Collections;
using UnityEngine;

/// <summary>
/// Физическая кнопка телефона на сцене.
/// </summary>
public class DialButton : MonoBehaviour
{
    [Header("Цифра")]
    [Range(0, 9)]
    public int digit = 0;

    [Header("Телефон")]
    [Tooltip("Ссылка на PhoneDialer в сцене")]
    public PhoneDialer dialerReference;

    [Header("Анимация — физическое смещение")]
    [Tooltip("Глубина нажатия по локальной оси -Y (метры)")]
    public float pressDepth = 0.005f;

    [Tooltip("Время нажатия вниз (секунды)")]
    public float pressDuration = 0.05f;

    [Tooltip("Время возврата вверх (секунды)")]
    public float returnDuration = 0.1f;

    [Header("Звук")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    private Vector3 _originLocalPos;
    private bool _isAnimating = false;

    private void Awake()
    {
        _originLocalPos = transform.localPosition;

        if (dialerReference == null)
            dialerReference = FindAnyObjectByType<PhoneDialer>();
    }

    private void OnMouseDown()
    {
        if (dialerReference == null) return;

        dialerReference.PressDigit(digit);
        PlayClick();

        if (!_isAnimating)
            StartCoroutine(AnimatePress());
    }

    private void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }

    private IEnumerator AnimatePress()
    {
        _isAnimating = true;

        Vector3 pressedPos = _originLocalPos - transform.parent.up * pressDepth;
        yield return MoveLocal(_originLocalPos, pressedPos, pressDuration);
        yield return MoveLocal(pressedPos, _originLocalPos, returnDuration);

        _isAnimating = false;
    }

    private IEnumerator MoveLocal(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.localPosition = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localPosition = to;
    }
}