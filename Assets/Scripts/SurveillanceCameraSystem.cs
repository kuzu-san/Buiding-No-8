using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный менеджер системы видеонаблюдения
/// </summary>
public class SurveillanceCameraSystem : MonoBehaviour
{
    public static SurveillanceCameraSystem Instance { get; private set; }

    [System.Serializable]
    public class Floor
    {
        public string floorName = "Этаж 1";
        public List<GameObject> cameras = new List<GameObject>();
    }

    [Header("Этажи и камеры")]
    public List<Floor> floors = new List<Floor>();

    public int CurrentFloorIndex => currentFloorIndex;

    private int currentFloorIndex = 0;
    private int currentCameraIndex = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (floors == null || floors.Count == 0)
        {
            Debug.LogError("[SurveillanceCameraSystem] Список этажей пуст!");
            return;
        }
        DisableAllCameras();
        EnableCamera(currentFloorIndex, currentCameraIndex);
    }

    public void NextCamera()
    {
        Floor floor = GetCurrentFloor();
        if (floor == null || floor.cameras.Count == 0) return;

        int nextIdx = (currentCameraIndex + 1) % floor.cameras.Count;
        SwitchCamera(currentFloorIndex, nextIdx);
    }

    public void PreviousCamera()
    {
        Floor floor = GetCurrentFloor();
        if (floor == null || floor.cameras.Count == 0) return;

        int prevIdx = (currentCameraIndex - 1 + floor.cameras.Count) % floor.cameras.Count;
        SwitchCamera(currentFloorIndex, prevIdx);
    }

    public void SwitchToFloor(int floorIndex)
    {
        if (floorIndex < 0 || floorIndex >= floors.Count)
        {
            Debug.LogWarning($"[SurveillanceCameraSystem] Некорректный индекс этажа: {floorIndex}");
            return;
        }
        DisableFloorCameras(currentFloorIndex);

        currentFloorIndex = floorIndex;
        currentCameraIndex = 0;

        EnableCamera(currentFloorIndex, currentCameraIndex);

        CameraFloorBridge bridge = GetComponent<CameraFloorBridge>();
        bridge?.NotifyFloorChanged(currentFloorIndex);

        Debug.Log($"[SurveillanceCameraSystem] Этаж: {floors[currentFloorIndex].floorName}, камера 0");
    }
    /// <summary>
    /// Переключает камеру внутри одного этажа:
    /// выключает текущую, включает новую.
    /// </summary>
    private void SwitchCamera(int floorIdx, int camIdx)
    {
        Floor floor = floors[floorIdx];
        if (floor.cameras == null || floor.cameras.Count == 0) return;
        DisableCamera(floorIdx, currentCameraIndex);
        currentCameraIndex = camIdx;
        EnableCamera(floorIdx, currentCameraIndex);

        Debug.Log($"[SurveillanceCameraSystem] {floor.floorName} → камера [{currentCameraIndex}]");
    }

    private void EnableCamera(int floorIdx, int camIdx)
    {
        var cam = GetCamera(floorIdx, camIdx);
        if (cam != null) cam.SetActive(true);
    }

    private void DisableCamera(int floorIdx, int camIdx)
    {
        var cam = GetCamera(floorIdx, camIdx);
        if (cam != null) cam.SetActive(false);
    }

    private void DisableFloorCameras(int floorIdx)
    {
        if (floors == null || floorIdx < 0 || floorIdx >= floors.Count) return;
        foreach (var cam in floors[floorIdx].cameras)
            if (cam != null) cam.SetActive(false);
    }
    private void DisableAllCameras()
    {
        foreach (var floor in floors)
            foreach (var cam in floor.cameras)
                if (cam != null) cam.SetActive(false);
    }

    private GameObject GetCamera(int floorIdx, int camIdx)
    {
        if (floors == null || floorIdx < 0 || floorIdx >= floors.Count) return null;
        var floor = floors[floorIdx];
        if (floor.cameras == null || camIdx < 0 || camIdx >= floor.cameras.Count) return null;
        return floor.cameras[camIdx];
    }

    private Floor GetCurrentFloor()
    {
        if (floors == null || currentFloorIndex >= floors.Count) return null;
        return floors[currentFloorIndex];
    }
}