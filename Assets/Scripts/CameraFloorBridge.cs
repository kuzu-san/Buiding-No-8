using UnityEngine;

/// <summary>
/// Мост между SurveillanceCameraSystem и FloorMoodSystem.
/// Отслеживает текущий просматриваемый этаж и уведомляет FloorMoodSystem
/// чтобы тот зажёг жёлтую лампочку нужного этажа.
/// Требует SurveillanceCameraSystem на том же GameObject.
/// </summary>
[RequireComponent(typeof(SurveillanceCameraSystem))]
public class CameraFloorBridge : MonoBehaviour
{
    private SurveillanceCameraSystem _camSystem;
    private int _lastFloorIndex = -1;

    private void Awake()
    {
        _camSystem = GetComponent<SurveillanceCameraSystem>();
    }

    /// <summary>
    /// Ждёт один кадр перед первым уведомлением.
    /// Нужно чтобы все FloorMoodSystem.Start() успели выполниться
    /// и инициализировать материалы лампочек.
    /// </summary>
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        NotifyFloorChanged(0);
    }

    /// <summary>
    /// Каждый кадр проверяет не сменился ли текущий этаж.
    /// Если сменился — вызывает NotifyFloorChanged.
    /// </summary>
    private void Update()
    {
        int floor = _camSystem != null ? _camSystem.CurrentFloorIndex : 0;

        if (floor != _lastFloorIndex)
            NotifyFloorChanged(floor);
    }

    /// <summary>
    /// Уведомляет все FloorMoodSystem о смене просматриваемого этажа.
    /// Нужный этаж зажигает жёлтую лампочку, остальные гасятся.
    /// Также вызывается напрямую из SurveillanceCameraSystem.SwitchToFloor().
    /// </summary>
    public void NotifyFloorChanged(int floorIndex)
    {
        _lastFloorIndex = floorIndex;

        foreach (var floor in FloorMoodSystem.AllFloors)
            floor.SetFloorSelected(floor.floorIndex == floorIndex);

        Debug.Log($"[CameraFloorBridge] Просматривается этаж {floorIndex}");
    }
}
