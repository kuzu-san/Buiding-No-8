using UnityEngine;
using OfficeGame;
using DialogueEditor;

/// <summary>
/// Телефонный аппарат на сцене.
/// Игрок нажимает физические кнопки, набирает номер работника
/// и запускает диалог
/// </summary>
public class PhoneDialer : MonoBehaviour
{
    public static PhoneDialer Instance { get; private set; }

    [System.Serializable]
    public class WorkerConversationEntry
    {
        [Tooltip("Номер телефона работника (например \"101\")")]
        public string phoneNumber;

        [Tooltip("NPCConversation, привязанная к этому работнику")]
        public NPCConversation conversation;
    }

    [Header("Диалоги работников")]
    [Tooltip("Список: номер телефона → NPCConversation")]
    public WorkerConversationEntry[] workerConversations;

    [Header("Набор номера")]
    [Tooltip("Максимальное количество цифр в номере")]
    [Min(1)]
    public int maxDigits = 3;

    [Header("Дисплей телефона")]
    [Tooltip("(Опционально) TMP_Text для отображения набранных цифр")]
    public TMPro.TMP_Text displayText;

    [Header("Звуки")]
    public AudioSource audioSource;
    public AudioClip   busyClip;

    private string _input = "";

    public static WorkerData ActiveWorker { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void Start()
    {
        ConversationManager.OnConversationEnded += HandleConversationEnded;
        UpdateDisplay();
    }
    private void OnDestroy()
    {
        ConversationManager.OnConversationEnded -= HandleConversationEnded;
    }

    /// <summary>Нажатие цифровой кнопки.</summary>
    public void PressDigit(int digit)
    {
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive) return;

        if (_input.Length >= maxDigits) return;

        _input += digit.ToString();
        UpdateDisplay();
        Debug.Log($"[PhoneDialer] Набрано: {_input}");
    }

    /// <summary>Кнопка «Вызов»</summary>
    public void ConfirmDial()
    {
        if (ConversationManager.Instance != null &&
            ConversationManager.Instance.IsConversationActive) return;

        if (string.IsNullOrEmpty(_input))
        {
            Debug.Log("[PhoneDialer] Номер не набран.");
            return;
        }

        WorkerData worker = WorkerManager.Instance?.FindByNumber(_input);

        if (worker == null)
        {
            Debug.Log($"[PhoneDialer] Номер «{_input}» не найден.");
            PlayBusy();
            ClearDial();
            return;
        }

        NPCConversation conv = FindConversation(_input);

        if (conv == null)
        {
            Debug.LogWarning($"[PhoneDialer] NPCConversation для номера «{_input}» не назначена в Inspector!");
            PlayBusy();
            ClearDial();
            return;
        }

        ActiveWorker = worker;

        ConversationManager.OnConversationStarted += SetDialogueParameters;

        ConversationManager.Instance.StartConversation(conv);

        ClearDial();
    }

    /// <summary>Кнопка «Сброс»</summary>
    public void ClearDial()
    {
        _input = "";
        UpdateDisplay();
    }

    /// <summary>
    /// Читает параметр "SelectedFloor" из ConversationManager и отправляет работника.
    /// </summary>
    public void OnFloorSelected()
    {
        if (ActiveWorker == null)
        {
            Debug.LogWarning("[PhoneDialer] OnFloorSelected: ActiveWorker == null");
            return;
        }

        if (ConversationManager.Instance == null) return;

        int floor = ConversationManager.Instance.GetInt("SelectedFloor");

        bool success = WorkerManager.Instance != null &&
                       WorkerManager.Instance.DispatchWorker(ActiveWorker, floor);

        if (success)
            Debug.Log($"[PhoneDialer] {ActiveWorker.workerName} отправлен на этаж {floor}");
        else
            Debug.LogWarning($"[PhoneDialer] Не удалось отправить {ActiveWorker.workerName} на этаж {floor}");
    }

    /// <summary>
    /// Устанавливает параметры диалога сразу после его старта.
    /// Вызывается однократно через OnConversationStarted.
    /// </summary>
    private void SetDialogueParameters()
    {
        ConversationManager.OnConversationStarted -= SetDialogueParameters;

        if (ActiveWorker == null || ConversationManager.Instance == null) return;

        ConversationManager.Instance.SetBool("WorkerAvailable", !ActiveWorker.IsOnCooldown);
        ConversationManager.Instance.SetInt("SelectedFloor", 0);
    }

    private void HandleConversationEnded()
    {
        ActiveWorker = null;
    }

    private NPCConversation FindConversation(string number)
    {
        if (workerConversations == null) return null;
        foreach (var entry in workerConversations)
            if (entry.phoneNumber == number) return entry.conversation;
        return null;
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
            displayText.text = _input;
    }

    private void PlayBusy()
    {
        if (audioSource != null && busyClip != null)
            audioSource.PlayOneShot(busyClip);
    }
}
