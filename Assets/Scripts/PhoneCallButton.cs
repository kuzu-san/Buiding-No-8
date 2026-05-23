using UnityEngine;

/// <summary>
/// Зелёная кнопка вызова / красная кнопка сброса на телефоне.
/// </summary>
public class PhoneCallButton : MonoBehaviour
{
    public enum ButtonType { Call, Clear }

    [Header("Тип")]
    public ButtonType buttonType = ButtonType.Call;

    [Header("Анимация (опционально)")]
    public float pressDepth   = 0.005f;
    public float pressDuration = 0.05f;

    private Vector3 _origin;
    private bool    _busy;

    private void Awake() => _origin = transform.localPosition;

    private void OnMouseDown()
    {
        if (PhoneDialer.Instance == null) return;

        if (buttonType == ButtonType.Call)
            PhoneDialer.Instance.ConfirmDial();
        else
            PhoneDialer.Instance.ClearDial();

        if (!_busy)
            StartCoroutine(Animate());
    }

    private System.Collections.IEnumerator Animate()
    {
        _busy = true;
        Vector3 pressed = _origin - transform.parent.up * pressDepth;

        float t = 0f;
        while (t < pressDuration)  { t += Time.deltaTime; transform.localPosition = Vector3.Lerp(_origin, pressed, t / pressDuration); yield return null; }
        t = 0f;
        while (t < pressDuration)  { t += Time.deltaTime; transform.localPosition = Vector3.Lerp(pressed, _origin,  t / pressDuration); yield return null; }
        transform.localPosition = _origin;
        _busy = false;
    }
}
