using System.Collections.Generic;
using UnityEngine;
using OfficeGame;

/// <summary>
/// Управляет шкалой настроения одного этажа и физической панелью лампочек.
/// Считает активные event-объекты на этаже и пропорционально заполняет шкалу.
/// Когда все этажи перегреты (MoodNormalized >= 1) — вызывает поражение.
/// </summary>
public class FloorMoodSystem : MonoBehaviour
{
    public static List<FloorMoodSystem> AllFloors { get; private set; } = new List<FloorMoodSystem>();

    [Header("Идентификация")]
    [Tooltip("Совпадает с индексом этажа в SurveillanceCameraSystem.floors")]
    public int floorIndex = 0;
    public string floorDisplayName = "Этаж 1";

    [Header("Корень этажа")]
    [Tooltip("GameObject внутри которого находятся все event-объекты этого этажа. Поиск только среди его потомков.")]
    public Transform floorRoot;

    [Header("Теги событий этого этажа")]
    [Tooltip("Теги из ObjectRandomizer, принадлежащие этому этажу")]
    public List<string> floorTags = new List<string>();

    [Header("Параметры шкалы")]
    [Tooltip("Сколько одновременно активных объектов = 100% шкала")]
    [Min(1)]
    public int maxEvents = 3;

    [Tooltip("Скорость роста шкалы (умножается на MoodFillMultiplier из GameDifficulty)")]
    [Range(0.1f, 5f)]
    public float fillSpeed = 1f;

    [Tooltip("Скорость спада шкалы когда нет активных событий")]
    [Range(0f, 5f)]
    public float decaySpeed = 0.1f;

    [Header("Физические лампочки — панель на сцене")]
    [Tooltip("Лампочка-индикатор этажа (левый столбец). Желтеет при просмотре этого этажа на камере.")]
    public Renderer floorIndicatorBulb;

    [Tooltip("5 лампочек настроения слева→направо. Шаг 20%. Каждая имеет свой цвет градиента.")]
    public Renderer[] moodBulbs = new Renderer[5];

    [Tooltip("Название color property в Material. Для URP Lit: _EmissionColor. Для Unlit: _BaseColor.")]
    public string colorProperty = "_EmissionColor";

    [Tooltip("Множитель яркости для HDR-эмиссии")]
    [Range(1f, 8f)]
    public float emissionIntensity = 2f;

    [Header("Цвета лампочек")]
    public Color bulbOffColor = new Color(0.05f, 0.05f, 0.05f);
    public Color bulbFloorSelectedColor = Color.yellow;
    public Color bulbFloorIdleColor = new Color(0.15f, 0.15f, 0.15f);

    [Tooltip("Цвета 5 лампочек настроения")]
    public Color[] moodBulbColors = new Color[5]
    {
        new Color(0.60f, 1.00f, 0.40f), // светло-зелёная
        new Color(0.10f, 0.85f, 0.10f), // зелёная
        new Color(1.00f, 0.90f, 0.00f), // жёлтая
        new Color(1.00f, 0.45f, 0.00f), // оранжевая
        new Color(1.00f, 0.08f, 0.08f), // красная
    };

    /// <summary>Текущее заполнение шкалы от 0 до 1.</summary>
    public float MoodNormalized { get; private set; } = 0f;

    public bool IsOverloaded { get; private set; } = false;

    /// <summary> Словарь: тег → список объектов с этим тегом на данном этаже.</summary>
    private Dictionary<string, List<GameObject>> _objectsByTag
        = new Dictionary<string, List<GameObject>>();

    private Material _floorBulbMat;
    private Material[] _moodBulbMats;

    private const int MOOD_BULB_COUNT = 5;

    private void Awake()
    {
        if (!AllFloors.Contains(this))
            AllFloors.Add(this);
        InstantiateBulbMaterials();
    }

    private void OnDestroy()
    {
        AllFloors.Remove(this);
    }

    private void Start()
    {
        if (floorRoot == null)
        {
            Debug.LogError($"[FloorMoodSystem] {floorDisplayName}: floorRoot не назначен!", this);
            return;
        }

        BuildTrackedObjects();
        SetFloorBulbColor(bulbFloorIdleColor);
        SetAllMoodBulbs(false);
    }

    ///
    /// Каждый кадр обновляет шкалу настроения и лампочки.
    ///
    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        int activeCount = CountActive();
        float targetMood = Mathf.Clamp01((float)activeCount / maxEvents);

        // Читаем множитель сложности из GameDifficulty
        float diffMult = GameDifficulty.Current?.MoodFillMultiplier ?? 1f;

        if (targetMood > MoodNormalized)
            MoodNormalized = Mathf.MoveTowards(MoodNormalized, targetMood, fillSpeed * diffMult * Time.deltaTime);
        else
            MoodNormalized = Mathf.MoveTowards(MoodNormalized, targetMood, decaySpeed * Time.deltaTime);

        IsOverloaded = MoodNormalized >= 1f;

        UpdateMoodBulbs();
        CheckDefeat();
    }

    /// <summary>
    /// Вызывается CameraFloorBridge при смене просматриваемого этажа.
    /// Зажигает жёлтую лампочку если этот этаж выбран, гасит иначе.
    /// </summary>
    public void SetFloorSelected(bool selected)
    {
        SetFloorBulbColor(selected ? bulbFloorSelectedColor : bulbFloorIdleColor);
    }

    /// <summary>
    /// Возвращает список активных объектов с указанным тегом только на этом этаже.
    /// Используется WorkerManager.DispatchWorker() для уборки объектов.
    /// </summary>
    public List<GameObject> GetObjectsByTag(string tag)
    {
        if (_objectsByTag.TryGetValue(tag, out var list))
            return list;
        return new List<GameObject>();
    }

    /// <summary>
    /// Собирает все дочерние объекты floorRoot с нужными тегами в словарь.
    /// Вызывается в Start. Можно вызвать вручную если объекты создаются динамически.
    /// </summary>
    public void BuildTrackedObjects()
    {
        _objectsByTag.Clear();

        foreach (string tag in floorTags)
        {
            if (string.IsNullOrEmpty(tag) || tag == "Untagged") continue;

            var list = new List<GameObject>();

            // Ищем ТОЛЬКО среди потомков floorRoot — не по всей сцене
            foreach (Transform child in floorRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child == floorRoot) continue;
                if (child.CompareTag(tag))
                    list.Add(child.gameObject);
            }

            _objectsByTag[tag] = list;
            Debug.Log($"[FloorMoodSystem] {floorDisplayName} / тег «{tag}»: {list.Count} объектов");
        }
    }

    /// <summary>Считает количество активных event-объектов на этаже.</summary>
    private int CountActive()
    {
        int count = 0;
        foreach (var list in _objectsByTag.Values)
            foreach (var obj in list)
                if (obj != null && obj.activeInHierarchy) count++;
        return count;
    }

    /// <summary>
    /// Зажигает лампочки настроения по строгим порогам.
    /// Каждая лампочка имеет свой цвет из moodBulbColors.
    /// </summary>
    private void UpdateMoodBulbs()
    {
        if (_moodBulbMats == null || _moodBulbMats.Length < MOOD_BULB_COUNT) return;

        int litCount = Mathf.Clamp(Mathf.FloorToInt(MoodNormalized * MOOD_BULB_COUNT), 0, MOOD_BULB_COUNT);

        for (int i = 0; i < MOOD_BULB_COUNT; i++)
        {
            if (i < litCount)
            {
                Color c = (i < moodBulbColors.Length) ? moodBulbColors[i] : Color.red;
                SetMat(_moodBulbMats[i], c * emissionIntensity);
            }
            else
            {
                SetMat(_moodBulbMats[i], bulbOffColor);
            }
        }
    }

    /// <summary>Включает все лампочки их цветами или выключает все сразу.</summary>
    private void SetAllMoodBulbs(bool on)
    {
        if (_moodBulbMats == null) return;
        for (int i = 0; i < Mathf.Min(_moodBulbMats.Length, MOOD_BULB_COUNT); i++)
        {
            Color c = (on && i < moodBulbColors.Length) ? moodBulbColors[i] * emissionIntensity : bulbOffColor;
            SetMat(_moodBulbMats[i], c);
        }
    }

    /// <summary>Устанавливает цвет лампочки-индикатора этажа.</summary>
    private void SetFloorBulbColor(Color color)
    {
        if (_floorBulbMat == null) return;
        SetMat(_floorBulbMat, color * emissionIntensity);
    }

    /// <summary>Применяет цвет к материалу и включает keyword эмиссии.</summary>
    private void SetMat(Material mat, Color color)
    {
        if (mat == null) return;
        mat.SetColor(colorProperty, color);
        mat.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// Создаёт инстансы материалов для каждой лампочки.
    /// Нужно чтобы изменение цвета не затрагивало другие объекты с тем же shared material.
    /// </summary>
    private void InstantiateBulbMaterials()
    {
        if (floorIndicatorBulb != null)
            _floorBulbMat = floorIndicatorBulb.material;

        if (moodBulbs != null)
        {
            _moodBulbMats = new Material[moodBulbs.Length];
            for (int i = 0; i < moodBulbs.Length; i++)
                if (moodBulbs[i] != null)
                    _moodBulbMats[i] = moodBulbs[i].material;
        }
    }

    /// <summary>
    /// Проверяет перегрев всех этажей.
    /// Если все FloorMoodSystem.IsOverloaded == true — вызывает поражение.
    /// </summary>
    private void CheckDefeat()
    {
        if (!IsOverloaded) return;
        foreach (var floor in AllFloors)
            if (!floor.IsOverloaded) return;
        GameManager.Instance?.TriggerDefeat();
    }
}