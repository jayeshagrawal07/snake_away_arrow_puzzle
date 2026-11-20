using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class LegacyAudioFeedback : MonoBehaviour
{
    private const int SampleRate = 22050;
    private AudioSource audioSource;
    private AudioClip buttonClip;
    private AudioClip blockedClip;
    private AudioClip escapeClip;
    private AudioClip levelCompleteClip;
    private bool isEnabled = true;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.24f;

        buttonClip = CreateTone("UI button", new[] { 740f }, 0.045f);
        blockedClip = CreateHiss("Blocked snake", 0.2f, 1701u, 0.56f);
        escapeClip = CreateHiss("Snake escape", 0.34f, 9387u, 0.68f);
        levelCompleteClip = CreateTone("Level complete", new[] { 523f, 659f, 784f }, 0.34f);
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (!isEnabled && audioSource != null)
            audioSource.Stop();
    }

    public void PlayButton()
    {
        Play(buttonClip);
    }

    public void PlayBlocked()
    {
        Play(blockedClip);
    }

    public void PlayEscape()
    {
        Play(escapeClip);
    }

    public void PlayLevelComplete()
    {
        Play(levelCompleteClip);
    }

    void OnDestroy()
    {
        DestroyClip(buttonClip);
        DestroyClip(blockedClip);
        DestroyClip(escapeClip);
        DestroyClip(levelCompleteClip);
    }

    private void Play(AudioClip clip)
    {
        if (isEnabled && audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private static AudioClip CreateTone(string clipName, float[] notes, float duration)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        int samplesPerNote = Mathf.Max(1, sampleCount / notes.Length);
        double phase = 0d;

        for (int i = 0; i < sampleCount; i++)
        {
            int noteIndex = Mathf.Min(i / samplesPerNote, notes.Length - 1);
            int noteSample = i - noteIndex * samplesPerNote;
            int noteSampleCount = noteIndex == notes.Length - 1
                ? sampleCount - noteIndex * samplesPerNote
                : samplesPerNote;
            float noteProgress = noteSample / (float)Mathf.Max(1, noteSampleCount);
            float attack = Mathf.Clamp01(noteProgress * 18f);
            float release = Mathf.Clamp01((1f - noteProgress) * 10f);
            float envelope = attack * release;
            float frequency = notes[noteIndex];
            phase += 2d * Mathf.PI * frequency / SampleRate;

            float fundamental = Mathf.Sin((float)phase);
            float harmonic = Mathf.Sin((float)(phase * 2d)) * 0.22f;
            float overtone = Mathf.Sin((float)(phase * 3d)) * 0.08f;
            samples[i] = (fundamental + harmonic + overtone) * envelope * 0.34f;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateHiss(string clipName, float duration, uint seed, float intensity)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];
        uint randomState = seed;
        float lowPassBody = 0f;
        float lowPassMid = 0f;
        float lowPassAir = 0f;
        float slowRoughness = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            randomState ^= randomState << 13;
            randomState ^= randomState >> 17;
            randomState ^= randomState << 5;
            float noise = ((randomState >> 8) * (1f / 16777215f)) * 2f - 1f;

            lowPassBody += (noise - lowPassBody) * 0.07f;
            lowPassMid += (noise - lowPassMid) * 0.38f;
            lowPassAir += (noise - lowPassAir) * 0.76f;
            slowRoughness += (noise - slowRoughness) * 0.012f;

            float progress = i / (float)Mathf.Max(1, sampleCount - 1);
            float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 9f));
            float release = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - progress) * 3.5f));
            float breath = 0.91f + Mathf.Sin(progress * Mathf.PI) * 0.09f;
            float envelope = attack * release * breath;

            float body = lowPassMid - lowPassBody;
            float airy = lowPassAir - lowPassMid;
            float rasp = noise - lowPassAir;
            float texture = body * 0.2f + airy * 0.66f + rasp * 0.14f + slowRoughness * 0.12f;
            samples[i] = texture * envelope * intensity;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void DestroyClip(AudioClip clip)
    {
        if (clip != null)
            Destroy(clip);
    }
}
