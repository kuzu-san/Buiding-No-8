using UnityEngine;
public class FloorSelectButton : MonoBehaviour
{
    [Header("Этаж")]
    [Tooltip("Индекс этажа (0-based). Должен совпадать с порядком в SurveillanceCameraSystem.floors.")]
    public int floorIndex = 0;

    private void OnMouseDown()
    {
        if (SurveillanceCameraSystem.Instance == null)
        {
            Debug.LogError("[FloorSelectButton] SurveillanceCameraSystem не найден на сцене!");
            return;
        }

        SurveillanceCameraSystem.Instance.SwitchToFloor(floorIndex);
    }
}