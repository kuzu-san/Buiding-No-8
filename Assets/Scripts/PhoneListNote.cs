using System.Collections;
using UnityEngine;

public class PhoneListNote : MonoBehaviour
{
    [Header("Положение при приближении")]
    public Vector3 zoomedPosition;
    public Vector3 zoomedRotation;

    [Header("Анимация")]
    public float duration = 0.3f;

    private Vector3 _originPosition;
    private Quaternion _originRotation;
    private bool _isZoomed = false;
    private bool _isAnimating = false;

    private void Start()
    {
        _originPosition = transform.position;
        _originRotation = transform.rotation;
    }

    private void OnMouseDown()
    {
        if (_isAnimating) return;

        if (!_isZoomed)
        {
            _isZoomed = true;
            StartCoroutine(MoveTo(zoomedPosition, Quaternion.Euler(zoomedRotation)));
        }
        else
        {
            _isZoomed = false;
            StartCoroutine(MoveTo(_originPosition, _originRotation));
        }
    }

    private IEnumerator MoveTo(Vector3 targetPos, Quaternion targetRot)
    {
        _isAnimating = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        _isAnimating = false;
    }
}