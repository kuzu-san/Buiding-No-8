using UnityEngine;

/// <summary>
/// ScriptableObject — данные одного работника.
/// Создайте ассет через Assets → Create → Game → WorkerData.
///
/// НАСТРОЙКА:
/// • phoneNumber   — номер для набора (например "101", "202")
/// • workerName    — имя для диалога
/// • handledTag    — тег объектов RandomEvents, которые он убирает
/// • cooldownTime  — время (секунды) до следующего вызова
/// • assignedFloors — индексы этажей, на которых работник трудится
/// </summary>
[CreateAssetMenu(fileName = "WorkerData", menuName = "Game/WorkerData", order = 1)]
public class WorkerData : ScriptableObject
{
    [Header("Идентификация")]
    public string workerName = "Иван";
    public string phoneNumber = "101";

    [Header("Функционал")]
    [Tooltip("Тег объектов RandomEvents, которые этот работник убирает")]
    public string handledTag = "GroupA";

    [Tooltip("Индексы этажей, на которых работник может работать (0-based)")]
    public int[] assignedFloors = { 0 };

    [Header("Кулдаун")]
    [Tooltip("Сколько секунд работник недоступен после выполнения задачи")]
    [Min(1f)]
    public float cooldownTime = 60f;

    // ──────────────────────────────────────────────
    // Состояние (рантайм, не сохраняется)
    // ──────────────────────────────────────────────

    [System.NonSerialized] public bool IsOnCooldown = false;
    [System.NonSerialized] public float CooldownRemaining = 0f;

    public void StartCooldown()
    {
        IsOnCooldown = true;
        CooldownRemaining = cooldownTime;
    }

    public void TickCooldown(float delta)
    {
        if (!IsOnCooldown) return;
        CooldownRemaining -= delta;
        if (CooldownRemaining <= 0f)
        {
            CooldownRemaining = 0f;
            IsOnCooldown = false;
        }
    }

    /// <summary>Проверить, обслуживает ли работник данный этаж.</summary>
    public bool ServesFloor(int floorIndex)
    {
        foreach (int f in assignedFloors)
            if (f == floorIndex) return true;
        return false;
    }
}