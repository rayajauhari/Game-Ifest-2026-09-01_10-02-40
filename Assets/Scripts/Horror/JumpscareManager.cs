using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager Instance { get; private set; }

    [Header("Monster Visuals")]
    public SpriteRenderer monsterJumpscareRenderer;
    public Sprite[] jumpscareSprites; // Monster A, B, C frames
    public Transform monsterStalker;   // Monster stalking in background/window/door
    public SpriteRenderer stalkerRenderer;

    [Header("Lighting / Camera Effects")]
    public Camera targetCamera;
    public UnityEngine.Rendering.Universal.Light2D redFlashLight;
    public UnityEngine.Rendering.Universal.Light2D globalLight;

    [Header("UI Canvas")]
    public Canvas jumpscareCanvas;
    public Image screenFlashImage;
    public Text deathReasonText;
    public Text restartPromptText;
    public GameObject notePopupPanel;

    private Vector3 initialCamPos;
    private bool isJumpscaring = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) initialCamPos = targetCamera.transform.position;

        if (monsterJumpscareRenderer != null)
            monsterJumpscareRenderer.gameObject.SetActive(false);

        if (screenFlashImage != null)
            screenFlashImage.color = new Color(0, 0, 0, 0);

        if (restartPromptText != null)
            restartPromptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isJumpscaring && restartPromptText != null && restartPromptText.gameObject.activeSelf)
        {
            bool restartPressed = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.R);
            if (restartPressed)
            {
                RestartGame();
            }
        }
    }

    public void TriggerJumpscare(string reason)
    {
        if (isJumpscaring) return;
        StartCoroutine(JumpscareRoutine(reason));
    }

    private IEnumerator JumpscareRoutine(string reason)
    {
        isJumpscaring = true;

        // Disable player controls
        Player p = FindAnyObjectByType<Player>();
        if (p != null)
        {
            p.enabled = false;
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 1. Sudden audio screech
        if (HorrorAudioSynthesizer.Instance != null)
        {
            HorrorAudioSynthesizer.Instance.PlayJumpscare();
        }

        // 2. Flash lights red & violent flicker
        if (redFlashLight != null)
        {
            redFlashLight.gameObject.SetActive(true);
            redFlashLight.intensity = 3f;
            redFlashLight.color = Color.red;
        }

        if (globalLight != null)
        {
            globalLight.intensity = 0.05f;
            globalLight.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        // 3. Show Monster in camera face
        if (monsterJumpscareRenderer != null)
        {
            monsterJumpscareRenderer.gameObject.SetActive(true);
            monsterJumpscareRenderer.transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 0f);
            monsterJumpscareRenderer.transform.localScale = Vector3.one * 8f; // Massive zoom
        }

        // Violent screen shake and sprite animation
        float elapsed = 0f;
        float duration = 1.6f;
        int frameIdx = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Camera shake
            float shakeMagnitude = (1f - (elapsed / duration)) * 0.7f;
            if (targetCamera != null)
            {
                targetCamera.transform.position = initialCamPos + (Vector3)(Random.insideUnitCircle * shakeMagnitude);
            }

            // Animate monster face
            if (monsterJumpscareRenderer != null && jumpscareSprites != null && jumpscareSprites.Length > 0)
            {
                frameIdx = (int)(elapsed * 12f) % jumpscareSprites.Length;
                monsterJumpscareRenderer.sprite = jumpscareSprites[frameIdx];

                // zoom closer
                monsterJumpscareRenderer.transform.localScale = Vector3.one * (8f + elapsed * 3f);
            }

            // Rapid Red/Black flicker
            if (screenFlashImage != null)
            {
                float flash = (Mathf.Sin(elapsed * 40f) > 0f) ? 0.4f : 0.0f;
                screenFlashImage.color = new Color(0.7f, 0f, 0f, flash);
            }

            yield return null;
        }

        // Reset camera
        if (targetCamera != null) targetCamera.transform.position = initialCamPos;

        // Hide jumpscare monster
        if (monsterJumpscareRenderer != null)
            monsterJumpscareRenderer.gameObject.SetActive(false);

        // Black screen with blood red death text
        if (screenFlashImage != null)
        {
            screenFlashImage.color = new Color(0f, 0f, 0f, 0.95f);
        }

        if (deathReasonText != null)
        {
            deathReasonText.gameObject.SetActive(true);
            deathReasonText.text = "KAMU TERTANGKAP!\n\n<color=#ff4444>" + reason + "</color>";
        }

        if (restartPromptText != null)
        {
            restartPromptText.gameObject.SetActive(true);
            restartPromptText.text = "Tekan [R] untuk Mencoba Lagi";
        }
    }

    public void TriggerNightVictory()
    {
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        if (HorrorAudioSynthesizer.Instance != null)
        {
            HorrorAudioSynthesizer.Instance.PlayMorningChime();
        }

        if (screenFlashImage != null)
        {
            // Morning warm fade
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime;
                screenFlashImage.color = new Color(1f, 0.95f, 0.8f, Mathf.PingPong(t * 2f, 0.5f));
                yield return null;
            }
            screenFlashImage.color = new Color(0, 0, 0, 0);
        }

        if (deathReasonText != null)
        {
            deathReasonText.gameObject.SetActive(true);
            deathReasonText.text = "<color=#ffe168>PAGI HARI TELAH TIBA!</color>\n\nKamu berhasil selamat dari teror monster karena mengunci rantai pintu dan mematikan alarm tepat waktu!";
        }

        yield return new WaitForSeconds(4f);

        if (deathReasonText != null)
        {
            deathReasonText.gameObject.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }
}
