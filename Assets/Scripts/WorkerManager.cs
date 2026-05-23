using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Центральный реестр работников.
/// Тикает кулдауны, предоставляет поиск по номеру телефона.
/// </summary>
public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }

    [Header("Работники")]
    public List<WorkerData> workers = new List<WorkerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        foreach (var w in workers)
            w.TickCooldown(Time.deltaTime);
    }
    public WorkerData FindByNumber(string number)
    {
        foreach (var w in workers)
            if (w.phoneNumber == number) return w;
        return null;
    }

    /// <summary>
    /// Отправить работника на этаж: убирает все активные объекты его тега на этаже,
    /// затем ставит его на кулдаун.
    /// </summary>
    public bool DispatchWorker(WorkerData worker, int floorIndex)
    {
        if (worker == null || worker.IsOnCooldown)
        {
            Debug.LogWarning($"[WorkerManager] Работник {worker?.workerName} недоступен (кулдаун).");
            return false;
        }

        if (!worker.ServesFloor(floorIndex))
        {
            Debug.LogWarning($"[WorkerManager] Работник {worker.workerName} не обслуживает этаж {floorIndex}.");
            return false;
        }

        FloorMoodSystem floor = null;
        foreach (var f in FloorMoodSystem.AllFloors)
        {
            if (f.floorIndex == floorIndex) { floor = f; break; }
        }

        if (floor == null)
        {
            Debug.LogWarning($"[WorkerManager] FloorMoodSystem с индексом {floorIndex} не найден.");
            return false;
        }

        List<GameObject> targets = floor.GetObjectsByTag(worker.handledTag);
        int removed = 0;
        foreach (var obj in targets)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                obj.SetActive(false);
                removed++;
            }
        }

        worker.StartCooldown();

        Debug.Log($"[WorkerManager] {worker.workerName} убрал {removed} объектов (тег «{worker.handledTag}») на этаже {floorIndex}. Кулдаун: {worker.cooldownTime}с");
        return true;
    }
}
