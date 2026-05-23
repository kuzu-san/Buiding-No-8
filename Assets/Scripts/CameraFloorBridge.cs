using UnityEngine;

/// <summary>
/// Отслеживает, какой этаж сейчас просматривается на камерах,
/// и сообщает FloorMoodSystem — тот зажигает жёлтую лампочку этажа.
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

    private System.Collections.IEnumerator Start()
    {
        yield return null;
        NotifyFloorChanged(0);
    }

    private void Update()
    {
        int floor = _camSystem != null ? _camSystem.CurrentFloorIndex : 0;

        if (floor != _lastFloorIndex)
            NotifyFloorChanged(floor);
    }
    public void NotifyFloorChanged(int floorIndex)
    {
        _lastFloorIndex = floorIndex;

        foreach (var floor in FloorMoodSystem.AllFloors)
            floor.SetFloorSelected(floor.floorIndex == floorIndex);

        Debug.Log($"[CameraFloorBridge] Просматривается этаж {floorIndex}");
    }

}