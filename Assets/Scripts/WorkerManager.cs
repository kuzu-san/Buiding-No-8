using System.Collections.Generic;
using UnityEngine;
using OfficeGame;

/// <summary>
/// Центральный реестр работников.
/// Тикает кулдауны каждый кадр и предоставляет поиск работника по номеру телефона.
/// </summary>
public class WorkerManager : MonoBehaviour
{
    public static WorkerManager Instance { get; private set; }

    [Header("Работники")]
    [Tooltip("Все WorkerData ScriptableObject'ы проекта")]
    public List<WorkerData> workers = new List<WorkerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// Каждый кадр уменьшает оставшийся кулдаун каждого работника.
    /// Когда кулдаун достигает 0 — WorkerData.IsOnCooldown становится false.
    /// </summary>
    private void Update()
    {
        foreach (var w in workers)
            w.TickCooldown(Time.deltaTime);
    }

    /// <summary>
    /// Ищет работника по номеру телефона.
    /// Возвращает null если работник с таким номером не найден.
    /// </summary>
    public WorkerData FindByNumber(string number)
    {
        foreach (var w in workers)
            if (w.phoneNumber == number) return w;
        return null;
    }

    /// <summary>
    /// Отправляет работника на указанный этаж.
    /// Выключает все активные объекты с его тегом на этаже, затем ставит на кулдаун.
    /// Возвращает false если работник недоступен, не обслуживает этаж, или этаж не найден.
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

        // Ищем FloorMoodSystem нужного этажа
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

        // Выключаем все активные объекты тега работника на этом этаже
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

        // Ставим работника на кулдаун
        worker.StartCooldown();

        Debug.Log($"[WorkerManager] {worker.workerName} убрал {removed} объектов " +
                  $"(тег «{worker.handledTag}») на этаже {floorIndex}. " +
                  $"Кулдаун: {worker.CooldownRemaining:F0}с");
        return true;
    }
}
