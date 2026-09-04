using UnityEngine;

public class HorrorAudioSynthesizer : MonoBehaviour
{
    public static HorrorAudioSynthesizer Instance { get; private set; }

    private AudioSource sfxSource;
    private AudioSource loopSource;
    private AudioSource ambientSource;

    private AudioClip alarmClip;
    private AudioClip chainClip;
    private AudioClip doorBangClip;
    private AudioClip monsterGrowlClip;
    private AudioClip jumpscareClip;
    private AudioClip clickClip;
    private AudioClip heartbeatClip;
    private AudioClip morningChimeClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.volume = 0.4f;

        GenerateClips();
    }

    private void GenerateClips()
    {
        alarmClip = CreateAlarmClip();
        chainClip = CreateChainClip();
        doorBangClip = CreateDoorBangClip();
        monsterGrowlClip = CreateMonsterGrowlClip();
        jumpscareClip = CreateJumpscareClip();
        clickClip = CreateClickClip();
        heartbeatClip = CreateHeartbeatClip();
        morningChimeClip = CreateMorningChimeClip();
    }

    public void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip, 0.7f);
    }

    public void PlayChainLock()
    {
        if (sfxSource != null && chainClip != null)
            sfxSource.PlayOneShot(chainClip, 0.9f);
    }

    public void PlayDoorBang()
    {
        if (sfxSource != null && doorBangClip != null)
            sfxSource.PlayOneShot(doorBangClip, 1.0f);
    }

    public void PlayMonsterGrowl()
    {
        if (sfxSource != null && monsterGrowlClip != null)
            sfxSource.PlayOneShot(monsterGrowlClip, 0.9f);
    }

    public void PlayJumpscare()
    {
        StopAlarm();
        if (sfxSource != null && jumpscareClip != null)
            sfxSource.PlayOneShot(jumpscareClip, 1.0f);
    }

    public void PlayHeartbeat()
    {
        if (sfxSource != null && heartbeatClip != null)
            sfxSource.PlayOneShot(heartbeatClip, 0.8f);
    }

    public void PlayMorningChime()
    {
        StopAlarm();
        if (sfxSource != null && morningChimeClip != null)
            sfxSource.PlayOneShot(morningChimeClip, 0.8f);
    }

    public void StartAlarm()
    {
        if (loopSource != null && alarmClip != null)
        {
            loopSource.clip = alarmClip;
            loopSource.volume = 0.9f;
            if (!loopSource.isPlaying)
                loopSource.Play();
        }
    }

    public void StopAlarm()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }

    public void SetAmbientNight(bool isNight)
    {
        if (ambientSource == null) return;
        if (isNight)
        {
            if (ambientSource.clip == null)
                ambientSource.clip = CreateNightAmbientClip();
            if (!ambientSource.isPlaying)
                ambientSource.Play();
        }
        else
        {
            if (ambientSource.isPlaying)
                ambientSource.Stop();
        }
    }

    // --- Audio Generators ---

    private AudioClip CreateAlarmClip()
    {
        int sampleRate = 44100;
        float duration = 1.0f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            bool inBurst1 = t >= 0.05f && t <= 0.35f;
            bool inBurst2 = t >= 0.55f && t <= 0.85f;

            if (inBurst1 || inBurst2)
            {
                float freq = 1200f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                wave += 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                samples[i] = wave * 0.5f;
            }
            else
            {
                samples[i] = 0f;
            }
        }

        AudioClip clip = AudioClip.Create("AlarmBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateChainClip()
    {
        int sampleRate = 44100;
        float duration = 0.6f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 8f);
            float metal = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * 1420f * t) * 0.3f
                        + Mathf.Sin(2f * Mathf.PI * 2300f * t) * 0.2f
                        + (Random.value * 2f - 1f) * 0.2f;
            if (t > 0.15f)
            {
                float t2 = t - 0.15f;
                float decay2 = Mathf.Exp(-t2 * 10f);
                metal += (Mathf.Sin(2f * Mathf.PI * 1100f * t2) + (Random.value * 2f - 1f) * 0.3f) * decay2 * 0.4f;
            }
            samples[i] = metal * decay * 0.6f;
        }

        AudioClip clip = AudioClip.Create("ChainRattle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateDoorBangClip()
    {
        int sampleRate = 44100;
        float duration = 0.8f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 6f);
            float thud = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.6f
                       + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.4f
                       + (Random.value * 2f - 1f) * 0.3f;
            samples[i] = thud * decay;
        }

        AudioClip clip = AudioClip.Create("DoorBang", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMonsterGrowlClip()
    {
        int sampleRate = 44100;
        float duration = 1.5f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(Mathf.PI * (t / duration));
            float freq = 65f + 25f * Mathf.Sin(2f * Mathf.PI * 4f * t);
            float noise = (Random.value * 2f - 1f) * 0.4f;
            float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f;
            samples[i] = (sub + noise) * env * 0.7f;
        }

        AudioClip clip = AudioClip.Create("MonsterGrowl", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateJumpscareClip()
    {
        int sampleRate = 44100;
        float duration = 1.8f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 2.5f);
            float pitchDrop = Mathf.Lerp(1600f, 200f, Mathf.Clamp01(t * 3f));
            float screech = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * pitchDrop * t)) * 0.4f;
            screech += Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (pitchDrop * 1.5f) * t)) * 0.25f;
            float bass = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f;
            float noise = (Random.value * 2f - 1f) * 0.35f;

            samples[i] = Mathf.Clamp((screech + bass + noise) * decay, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("JumpscareScreech", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateClickClip()
    {
        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 60f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 900f * t) * decay * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ClickSound", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHeartbeatClip()
    {
        int sampleRate = 44100;
        float duration = 0.8f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float beat1 = (t >= 0.0f && t <= 0.2f) ? Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-t * 15f) : 0f;
            float t2 = t - 0.25f;
            float beat2 = (t >= 0.25f && t <= 0.45f) ? Mathf.Sin(2f * Mathf.PI * 45f * t2) * Mathf.Exp(-t2 * 18f) * 0.8f : 0f;
            samples[i] = (beat1 + beat2) * 0.9f;
        }

        AudioClip clip = AudioClip.Create("Heartbeat", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateMorningChimeClip()
    {
        int sampleRate = 44100;
        float duration = 2.0f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;
            for (int k = 0; k < freqs.Length; k++)
            {
                float noteTime = t - (k * 0.2f);
                if (noteTime > 0f)
                {
                    float decay = Mathf.Exp(-noteTime * 2.5f);
                    val += Mathf.Sin(2f * Mathf.PI * freqs[k] * noteTime) * decay * 0.25f;
                }
            }
            samples[i] = Mathf.Clamp(val, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("MorningChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateNightAmbientClip()
    {
        int sampleRate = 44100;
        float duration = 3.0f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float lowDrone = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.3f
                           + Mathf.Sin(2f * Mathf.PI * 43f * t) * 0.2f;
            float windNoise = (Random.value * 2f - 1f) * 0.1f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.3f * t));
            samples[i] = (lowDrone + windNoise) * 0.4f;
        }

        AudioClip clip = AudioClip.Create("NightAmbient", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
