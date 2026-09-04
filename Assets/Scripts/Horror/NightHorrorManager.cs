using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum NightPhase
{
    DayTime,          // Siang / Pagi - waktu membaca catatan / bersiap
    NightCountdown,   // Malam tiba - alarm mulai berbunyi, lampu redup, waktu bersiap (10-15 detik)
    MonsterPatrol,    // Monster tiba di luar pintu/kamar - memeriksa apakah pintu terkunci dan alarm mati
    Survived,         // Berhasil selamat sampai pagi
    Jumpscared        // Gagal dan tertangkap
}

public class NightHorrorManager : MonoBehaviour
{
    public static NightHorrorManager Instance { get; private set; }

    [Header("Cycle Timers")]
    [Tooltip("Durasi waktu pagi sebelum malam tiba")]
    public float dayDuration = 12f;
    [Tooltip("Waktu peringatan saat malam tiba sebelum monster memeriksa kamar")]
    public float nightPrepDuration = 10f;
    [Tooltip("Durasi monster berpatroli jika player sukses bertahan")]
    public float monsterPatrolDuration = 8f;

    [Header("Current States")]
    public NightPhase currentPhase = NightPhase.DayTime;
    public bool isDoorChained = false;
    public bool isAlarmTurnedOff = false;
    public bool IsGameOver => currentPhase == NightPhase.Jumpscared;

    [Header("UI References")]
    public Text timerStatusText;
    public Text promptText;
    public Text warningBannerText;
    public GameObject stickyNotePopup;
    public Text checklistText;

    [Header("Background & Visual References")]
    public SpriteRenderer backgroundRenderer;
    public Sprite dayBackground;
    public Sprite nightBackground;
    public GameObject chainOverlayObject;
    public GameObject alarmClockObject;
    public GameObject stickyNoteObject;
    public Transform doorPatrolPosition;
    public SpriteRenderer monsterStalkerRenderer;
    public Sprite[] monsterWalkSprites;
    public Sprite[] monsterAggroSprites;

    [Header("DayNight Lighting Link")]
    public DayNightCycle dayNightCycle;
    public UnityEngine.Rendering.Universal.Light2D globalLight2D;

    private float currentTimer = 0f;
    private Coroutine gameLoopCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (dayNightCycle == null) dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        if (globalLight2D == null) globalLight2D = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();

        if (stickyNotePopup != null) stickyNotePopup.SetActive(false);
        if (promptText != null) promptText.text = "";
        if (warningBannerText != null) warningBannerText.text = "";

        UpdateChecklistUI();
        gameLoopCoroutine = StartCoroutine(MainGameRoutine());
    }

    private IEnumerator MainGameRoutine()
    {
        // 1. DAY TIME
        currentPhase = NightPhase.DayTime;
        if (dayNightCycle != null) dayNightCycle.SetMorning();
        if (backgroundRenderer != null && dayBackground != null) backgroundRenderer.sprite = dayBackground;
        if (HorrorAudioSynthesizer.Instance != null) HorrorAudioSynthesizer.Instance.SetAmbientNight(false);
        if (monsterStalkerRenderer != null) monsterStalkerRenderer.gameObject.SetActive(false);

        isAlarmTurnedOff = false; // Reset alarm state for new night
        UpdateChecklistUI();

        ShowWarningBanner("SIANG HARI: Perhatikan catatan dan pelajari aturan bertahan hidup!");

        currentTimer = dayDuration;
        while (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            if (timerStatusText != null)
            {
                timerStatusText.text = $"<color=#ffe680>SIANG HARI</color> | Malam tiba dalam: {Mathf.CeilToInt(currentTimer)}s";
            }
            yield return null;
        }

        // 2. NIGHT ARRIVAL & PREPARATION COUNTDOWN
        currentPhase = NightPhase.NightCountdown;
        if (dayNightCycle != null) dayNightCycle.SetNight();
        if (backgroundRenderer != null && nightBackground != null) backgroundRenderer.sprite = nightBackground;
        if (HorrorAudioSynthesizer.Instance != null)
        {
            HorrorAudioSynthesizer.Instance.SetAmbientNight(true);
            if (!isAlarmTurnedOff)
            {
                HorrorAudioSynthesizer.Instance.StartAlarm();
            }
        }

        ShowWarningBanner("<color=#ff4444>MALAM TELAH TIBA! ALARM BERBUNYI! KUNCI PINTU DENGAN RANTAI & MATIKAN ALARM SEBELUM MONSTER DATANG!</color>");

        currentTimer = nightPrepDuration;
        while (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            if (timerStatusText != null)
            {
                timerStatusText.text = $"<color=#ff4444>WASPADA MALAM!</color> | Monster mendekat dalam: {Mathf.CeilToInt(currentTimer)}s";
            }

            // If alarm is not turned off yet, keep alarm sound playing
            if (!isAlarmTurnedOff && HorrorAudioSynthesizer.Instance != null)
            {
                HorrorAudioSynthesizer.Instance.StartAlarm();
            }

            UpdateChecklistUI();
            yield return null;
        }

        // 3. MONSTER ARRIVES / PATROL & EVALUATION
        currentPhase = NightPhase.MonsterPatrol;
        if (timerStatusText != null)
        {
            timerStatusText.text = "<color=#ff1111>MONSTER BERADA DI DEPAN PINTU!</color>";
        }

        // Check conditions
        bool doorSafe = isDoorChained;
        bool alarmSafe = isAlarmTurnedOff;

        // Visual stalker outside
        if (monsterStalkerRenderer != null)
        {
            monsterStalkerRenderer.gameObject.SetActive(true);
            if (monsterWalkSprites != null && monsterWalkSprites.Length > 0)
                monsterStalkerRenderer.sprite = monsterWalkSprites[0];
        }

        // Monster growls & door rattle
        if (HorrorAudioSynthesizer.Instance != null)
        {
            HorrorAudioSynthesizer.Instance.PlayMonsterGrowl();
            HorrorAudioSynthesizer.Instance.PlayDoorBang();
        }

        // EVALUATION:
        if (!alarmSafe)
        {
            // FAILED ALARM: Monster hears the alarm and breaks in!
            yield return new WaitForSeconds(1.2f);
            if (HorrorAudioSynthesizer.Instance != null)
            {
                HorrorAudioSynthesizer.Instance.PlayDoorBang();
            }
            yield return new WaitForSeconds(0.8f);

            currentPhase = NightPhase.Jumpscared;
            string reason = !doorSafe 
                ? "Kamu TIDAK MEMATIKAN ALARM dan TIDAK MENGUNCI RANTAI PINTU!\nSuara alarm kencang memancing monster mendobrak kamarmu!"
                : "Kamu LUPA MEMATIKAN ALARM JAM!\nSuara alarm yang berisik membuat monster mengetahui posisimu dan mendobrak paksa pintu!";
            
            if (JumpscareManager.Instance != null)
            {
                JumpscareManager.Instance.TriggerJumpscare(reason);
            }
            yield break;
        }
        else if (!doorSafe)
        {
            // FAILED DOOR: Door was not locked with chain, monster easily opens door and attacks!
            yield return new WaitForSeconds(1.2f);
            if (HorrorAudioSynthesizer.Instance != null)
            {
                HorrorAudioSynthesizer.Instance.PlayDoorBang();
            }
            yield return new WaitForSeconds(0.8f);

            currentPhase = NightPhase.Jumpscared;
            string reason = "Kamu LUPA MENGUNCI PINTU DENGAN RANTAI!\nMonster langsung membuka pintu kamar yang tidak terkunci!";
            
            if (JumpscareManager.Instance != null)
            {
                JumpscareManager.Instance.TriggerJumpscare(reason);
            }
            yield break;
        }

        // IF BOTH ARE SAFE: Monster tries the door, bangs on the chain, cannot enter, and leaves!
        ShowWarningBanner("<color=#ffff55>Monster mencoba membuka pintu... Terkunci rapat oleh rantai!</color>");
        
        float patrolTimer = monsterPatrolDuration;
        while (patrolTimer > 0)
        {
            patrolTimer -= Time.deltaTime;
            if (HorrorAudioSynthesizer.Instance != null && Mathf.Approximately(Mathf.Floor(patrolTimer), 4f))
            {
                HorrorAudioSynthesizer.Instance.PlayDoorBang();
                HorrorAudioSynthesizer.Instance.PlayHeartbeat();
            }
            yield return null;
        }

        // 4. SURVIVED! Morning returns
        currentPhase = NightPhase.Survived;
        if (monsterStalkerRenderer != null) monsterStalkerRenderer.gameObject.SetActive(false);

        if (JumpscareManager.Instance != null)
        {
            JumpscareManager.Instance.TriggerNightVictory();
        }

        yield return new WaitForSeconds(5f);

        // Restart loop for next night
        StartCoroutine(MainGameRoutine());
    }

    public void SetDoorChained(bool chained)
    {
        isDoorChained = chained;
        if (chainOverlayObject != null)
        {
            chainOverlayObject.SetActive(chained);
        }
        UpdateChecklistUI();
    }

    public void SetAlarmTurnedOff(bool turnedOff)
    {
        isAlarmTurnedOff = turnedOff;
        UpdateChecklistUI();
    }

    public void ShowPrompt(string msg)
    {
        if (promptText != null)
        {
            promptText.text = msg;
            promptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt(string msg)
    {
        if (promptText != null && promptText.text == msg)
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
        }
    }

    public void ToggleNotePopup()
    {
        if (stickyNotePopup != null)
        {
            stickyNotePopup.SetActive(!stickyNotePopup.activeSelf);
        }
    }

    public void ShowWarningBanner(string text)
    {
        if (warningBannerText != null)
        {
            warningBannerText.text = text;
            warningBannerText.gameObject.SetActive(true);
            StopCoroutine(nameof(HideBannerAfterDelay));
            StartCoroutine(nameof(HideBannerAfterDelay), 5f);
        }
    }

    private IEnumerator HideBannerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (warningBannerText != null)
        {
            warningBannerText.text = "";
        }
    }

    private void UpdateChecklistUI()
    {
        if (checklistText != null)
        {
            string doorStatus = isDoorChained 
                ? "<color=#55ff55>[V] Pintu Terkunci Rantai</color>" 
                : "<color=#ff5555>[X] Pintu Belum Dikunci Rantai!</color>";

            string alarmStatus = isAlarmTurnedOff 
                ? "<color=#55ff55>[V] Alarm Jam Dimatikan</color>" 
                : "<color=#ff5555>[X] Alarm Jam Masih Aktif!</color>";

            checklistText.text = $"<b>TUGAS BERTAHAN HIDUP:</b>\n{doorStatus}\n{alarmStatus}";
        }
    }
}
