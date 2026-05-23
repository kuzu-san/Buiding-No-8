using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Шкала настроения одного этажа + поддержка физической панели лампочек на сцене.
/// </summary>
public class FloorMoodSystem : MonoBehaviour
{
    public static List<FloorMoodSystem> AllFloors { get; private set; } = new List<FloorMoodSystem>();

    [Header("Идентификация")]
    public int floorIndex = 0;
    public string floorDisplayName = "Этаж 1";

    [Header("Корень этажа")]
    [Tooltip("GameObject, ВНУТРИ которого находятся все объекты событий этого этажа.")]
    public Transform floorRoot;

    [Header("Теги событий этого этажа")]
    public List<string> floorTags = new List<string>();

    [Header("Параметры шкалы")]
    [Min(1)]
    public int maxEvents = 3;

    [Range(0.1f, 5f)]
    public float fillSpeed = 1f;

    [Range(0f, 5f)]
    public float decaySpeed = 0.1f;

    [Header("Физические лампочки — панель на сцене")]

    [Tooltip("Лампочка-индикатор этажа")]
    public Renderer floorIndicatorBulb;

    [Tooltip("Лампочки настроения этого этажа слева→направо")]
    public Renderer[] moodBulbs = new Renderer[4];

    [Tooltip("Название property в Material для эмиссии/цвета. " +
             "Стандартный Lit: \"_EmissionColor\". Unlit/Custom: \"_BaseColor\" или \"_Color\".")]
    public string colorProperty = "_EmissionColor";

    [Tooltip("Множитель яркости для цветов (для HDR-эмиссии; 1 = обычный цвет)")]
    [Range(1f, 8f)]
    public float emissionIntensity = 2f;

    [Header("Цвета лампочек")]
    public Color bulbOffColor = new Color(0.05f, 0.05f, 0.05f);
    public Color bulbMoodOnColor = Color.red;
    public Color bulbFloorSelectedColor = Color.yellow;
    public Color bulbFloorIdleColor = new Color(0.15f, 0.15f, 0.15f);

    public float MoodNormalized { get; private set; } = 0f;
    public bool IsOverloaded { get; private set; } = false;

    private Dictionary<string, List<GameObject>> _objectsByTag
        = new Dictionary<string, List<GameObject>>();

    private Material _floorBulbMat;
    private Material[] _moodBulbMats;

    private bool _isFloorSelected = false;


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

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning) return;

        int activeCount = CountActive();
        float targetMood = Mathf.Clamp01((float)activeCount / maxEvents);

        if (targetMood > MoodNormalized)
            MoodNormalized = Mathf.MoveTowards(MoodNormalized, targetMood, fillSpeed * Time.deltaTime);
        else
            MoodNormalized = Mathf.MoveTowards(MoodNormalized, targetMood, decaySpeed * Time.deltaTime);

        IsOverloaded = MoodNormalized >= 1f;

        UpdateMoodBulbs();
        CheckDefeat();
    }

    /// <summary>
    /// Вызывается SurveillanceCameraSystem
    /// при переключении просматриваемого этажа.
    /// </summary>
    public void SetFloorSelected(bool selected)
    {
        _isFloorSelected = selected;
        SetFloorBulbColor(selected ? bulbFloorSelectedColor : bulbFloorIdleColor);
    }

    /// <summary>Возвращает объекты с указанным тегом только на этом этаже.</summary>
    public List<GameObject> GetObjectsByTag(string tag)
    {
        if (_objectsByTag.TryGetValue(tag, out var list))
            return list;
        return new List<GameObject>();
    }

    /// <summary>
    /// Пересобрать список отслеживаемых объектов.
    /// Вызовите вручную если объекты создаются динамически.
    /// </summary>
    public void BuildTrackedObjects()
    {
        _objectsByTag.Clear();

        foreach (string tag in floorTags)
        {
            if (string.IsNullOrEmpty(tag) || tag == "Untagged") continue;

            var list = new List<GameObject>();

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

    private int CountActive()
    {
        int count = 0;
        foreach (var list in _objectsByTag.Values)
            foreach (var obj in list)
                if (obj != null && obj.activeInHierarchy) count++;
        return count;
    }

    private const int MOOD_BULB_COUNT = 4;

    /// <summary>
    /// Зажигает лампочки настроения по строгим порогам 25%.
    /// </summary>
    private void UpdateMoodBulbs()
    {
        if (_moodBulbMats == null || _moodBulbMats.Length < MOOD_BULB_COUNT) return;
        int litCount = Mathf.FloorToInt(MoodNormalized * MOOD_BULB_COUNT);
        litCount = Mathf.Clamp(litCount, 0, MOOD_BULB_COUNT);

        for (int i = 0; i < MOOD_BULB_COUNT; i++)
        {
            bool lit = i < litCount;
            SetMat(_moodBulbMats[i], lit ? bulbMoodOnColor * emissionIntensity : bulbOffColor);
        }
    }

    private void SetAllMoodBulbs(bool on)
    {
        if (_moodBulbMats == null) return;
        for (int i = 0; i < Mathf.Min(_moodBulbMats.Length, MOOD_BULB_COUNT); i++)
            SetMat(_moodBulbMats[i], on ? bulbMoodOnColor * emissionIntensity : bulbOffColor);
    }

    private void SetFloorBulbColor(Color color)
    {
        if (_floorBulbMat == null) return;
        SetMat(_floorBulbMat, color * emissionIntensity);
    }

    private void SetMat(Material mat, Color color)
    {
        if (mat == null) return;
        mat.SetColor(colorProperty, color);
        mat.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// Создаём инстансы материалов для каждой лампочки, чтобы
    /// не менять shared material и не затрагивать другие объекты.
    /// </summary>
    private void InstantiateBulbMaterials()
    {
        if (floorIndicatorBulb != null)
            _floorBulbMat = floorIndicatorBulb.material;

        if (moodBulbs != null)
        {
            _moodBulbMats = new Material[moodBulbs.Length];
            for (int i = 0; i < moodBulbs.Length; i++)
            {
                if (moodBulbs[i] != null)
                    _moodBulbMats[i] = moodBulbs[i].material;
            }
        }
    }

    private void CheckDefeat()
    {
        if (!IsOverloaded) return;
        foreach (var floor in AllFloors)
            if (!floor.IsOverloaded) return;
        GameManager.Instance?.TriggerDefeat();
    }
}