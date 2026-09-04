using UnityEngine;
using UnityEngine.InputSystem;

public enum InteractionType
{
    ChainDoor,
    AlarmClock,
    StickyNote
}

public class InteractableObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    public InteractionType type;
    public string promptText = "Tekan [E] untuk Berinteraksi";
    public KeyCode legacyKey = KeyCode.E;

    [Header("Visual Feedback")]
    public GameObject activeVisual;    // Visual shown when state is ON/LOCKED
    public GameObject inactiveVisual;  // Visual shown when state is OFF/UNLOCKED
    public SpriteRenderer highlightSprite;
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    [Header("State")]
    public bool isPlayerNearby = false;
    public bool isStateActive = false; // e.g. Door is chained / Alarm is turned OFF

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if (isPlayerNearby)
        {
            bool interactPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.fKey.wasPressedThisFrame)
                {
                    interactPressed = true;
                }
            }
            if (Input.GetKeyDown(legacyKey) || Input.GetKeyDown(KeyCode.F))
            {
                interactPressed = true;
            }

            if (interactPressed)
            {
                Interact();
            }
        }
    }

    public void Interact()
    {
        if (NightHorrorManager.Instance != null && NightHorrorManager.Instance.IsGameOver)
            return;

        switch (type)
        {
            case InteractionType.ChainDoor:
                isStateActive = !isStateActive;
                UpdateVisuals();
                if (HorrorAudioSynthesizer.Instance != null)
                {
                    HorrorAudioSynthesizer.Instance.PlayChainLock();
                }
                if (NightHorrorManager.Instance != null)
                {
                    NightHorrorManager.Instance.SetDoorChained(isStateActive);
                }
                break;

            case InteractionType.AlarmClock:
                // Toggling alarm off
                if (!isStateActive)
                {
                    isStateActive = true; // Alarm is disabled/turned off
                    UpdateVisuals();
                    if (HorrorAudioSynthesizer.Instance != null)
                    {
                        HorrorAudioSynthesizer.Instance.PlayClick();
                        HorrorAudioSynthesizer.Instance.StopAlarm();
                    }
                    if (NightHorrorManager.Instance != null)
                    {
                        NightHorrorManager.Instance.SetAlarmTurnedOff(true);
                    }
                }
                else
                {
                    // Can toggle back on or just click
                    if (HorrorAudioSynthesizer.Instance != null)
                    {
                        HorrorAudioSynthesizer.Instance.PlayClick();
                    }
                }
                break;

            case InteractionType.StickyNote:
                if (HorrorAudioSynthesizer.Instance != null)
                {
                    HorrorAudioSynthesizer.Instance.PlayClick();
                }
                if (NightHorrorManager.Instance != null)
                {
                    NightHorrorManager.Instance.ToggleNotePopup();
                }
                break;
        }
    }

    public void UpdateVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(isStateActive);
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isStateActive);

        if (highlightSprite != null)
        {
            highlightSprite.color = isPlayerNearby ? highlightColor : normalColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<Player>() != null)
        {
            isPlayerNearby = true;
            UpdateVisuals();
            if (NightHorrorManager.Instance != null)
            {
                NightHorrorManager.Instance.ShowPrompt(promptText);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.GetComponent<Player>() != null)
        {
            isPlayerNearby = false;
            UpdateVisuals();
            if (NightHorrorManager.Instance != null)
            {
                NightHorrorManager.Instance.HidePrompt(promptText);
            }
        }
    }
}
