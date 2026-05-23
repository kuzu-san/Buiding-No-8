using UnityEngine;

/// <summary>
/// Кнопка навигации по камерам текущего этажа.
/// </summary>
public class CameraNavButton : MonoBehaviour
{
    public enum NavDirection { Next, Previous }

    [Header("Направление переключения")]
    public NavDirection direction = NavDirection.Next;

    private void OnMouseDown()
    {

        if (SurveillanceCameraSystem.Instance == null)
        {
            Debug.LogError("[CameraNavButton] SurveillanceCameraSystem не найден на сцене!");
            return;
        }

        if (direction == NavDirection.Next)
            SurveillanceCameraSystem.Instance.NextCamera();
        else
            SurveillanceCameraSystem.Instance.PreviousCamera();
    }
}