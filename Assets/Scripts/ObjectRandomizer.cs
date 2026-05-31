using System.Collections.Generic;
using UnityEngine;
using OfficeGame;

/// <summary>
/// Активирует случайный объект из группы по тегу через заданный интервал.
/// Для каждой группы можно настроить свой шанс срабатывания и интервал.
/// </summary>
public class ObjectRandomizer : MonoBehaviour
{
    [System.Serializable]
    public class ObjectGroup
    {
        [Header("Идентификация группы")]
        public string groupName = "Группа";
        public string tag = "Untagged";

        [Header("Таймер")]
        [Tooltip("Интервал между попытками активации (секунды)")]
        [Range(0.1f, 60f)]
        public float interval = 5f;

        [Header("Шансы")]
        [Tooltip("Шанс того, что хоть один объект включится (0 = никогда, 1 = всегда)")]
        [Range(0f, 1f)]
        public float activationChance = 0.5f;

        [Header("Поведение")]
        [Tooltip("Выключать все объекты группы перед активацией нового")]
        public bool disableOthersOnActivate = true;

        [Tooltip("Выключать активный объект, если в этот тик не выпал шанс")]
        public bool disableOnMiss = false;

        [HideInInspector] public List<GameObject> objects = new List<GameObject>();
        [HideInInspector] public float timer = 0f;
        [HideInInspector] public GameObject currentActive = null;
    }

    [Header("Группы объектов")]
    public ObjectGroup[] groups = new ObjectGroup[3]
    {
        new ObjectGroup { groupName = "Группа A", tag = "GroupA", interval = 3f, activationChance = 0.7f },
        new ObjectGroup { groupName = "Группа B", tag = "GroupB", interval = 5f, activationChance = 0.5f },
        new ObjectGroup { groupName = "Группа C", tag = "GroupC", interval = 8f, activationChance = 0.3f },
    };

    [Header("Задержка старта")]
    [Tooltip("Сколько секунд после запуска сцены ничего не активируется")]
    [Range(0f, 120f)]
    public float startDelay = 15f;

    [Header("Дебаг")]
    public bool showDebugLogs = true;

    private float _globalTimer = 0f;
    private bool _started = false;

    private void Start()
    {
        // Собираем все объекты по тегам и выключаем их
        foreach (var group in groups)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(group.tag);
            group.objects.AddRange(found);

            foreach (var obj in group.objects)
                obj.SetActive(false);

            // Немного разносим таймеры, чтобы не стрелять всё разом в 0
            group.timer = Random.Range(0f, group.interval);

            if (showDebugLogs)
                Debug.Log($"[ObjectRandomizer] {group.groupName}: найдено {group.objects.Count} объектов (тег «{group.tag}»)");
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning) return;

        if (!_started)
        {
            _globalTimer += Time.deltaTime;
            if (_globalTimer < startDelay) return;

            _started = true;

            // Сбрасываем таймеры групп, чтобы отсчёт начался с момента разблокировки
            foreach (var group in groups)
                group.timer = Random.Range(0f, group.interval);

            if (showDebugLogs)
                Debug.Log($"[ObjectRandomizer] Задержка {startDelay}с прошла — рандомайзеры запущены");
        }

        foreach (var group in groups)
        {
            if (group.objects.Count == 0) continue;

            group.timer += Time.deltaTime;

            float intervalMult = GameDifficulty.Current?.EventIntervalMultiplier ?? 1f;
            if (group.timer >= group.interval * intervalMult)
            {
                group.timer = 0f;
                TryActivate(group);
            }
        }
    }

    private void TryActivate(ObjectGroup group)
    {
        float roll = Random.value;

        if (roll > group.activationChance)
        {
            if (group.disableOnMiss && group.currentActive != null)
            {
                group.currentActive.SetActive(false);
                group.currentActive = null;

                if (showDebugLogs)
                    Debug.Log($"[ObjectRandomizer] {group.groupName}: шанс не выпал ({roll:F2} > {group.activationChance:F2}), объект выключен");
            }
            else if (showDebugLogs)
            {
                Debug.Log($"[ObjectRandomizer] {group.groupName}: шанс не выпал ({roll:F2} > {group.activationChance:F2})");
            }

            return;
        }

        int index = Random.Range(0, group.objects.Count);
        GameObject chosen = group.objects[index];

        if (group.disableOthersOnActivate)
        {
            foreach (var obj in group.objects)
                obj.SetActive(false);
        }

        chosen.SetActive(true);
        group.currentActive = chosen;

        if (showDebugLogs)
            Debug.Log($"[ObjectRandomizer] {group.groupName}: активирован «{chosen.name}» (roll={roll:F2}, шанс={group.activationChance:F2})");
    }

    /// <summary>Выключить все объекты во всех группах.</summary>
    public void DisableAll()
    {
        foreach (var group in groups)
        {
            foreach (var obj in group.objects)
                obj.SetActive(false);
            group.currentActive = null;
        }
    }

    /// <summary>
    /// Сбрасывает таймеры и флаг старта.
    /// Вызывается GameManager при окончании игры — следующий запуск начнётся с нуля.
    /// </summary>
    public void ResetState()
    {
        _started = false;
        _globalTimer = 0f;

        foreach (var group in groups)
            group.timer = 0f;
    }
}