using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Routing")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerSnapshot normalSnapshot;
    [SerializeField] private AudioMixerSnapshot frightenedSnapshot;

    [Header("Music Clips")]
    public AudioClip bootLoop;
    public AudioClip introLoop;
    public AudioClip normalLoop;
    public AudioClip powerLoop;
    public AudioClip killLoop;
    public AudioClip levelClear;
    public AudioClip deathLoop;

    [Header("Crossfade")]
    [Range(0f, 5f)] public float defaultFade = 0.6f;

    private AudioSource a;
    private AudioSource b;
    private AudioSource stinger;
    private Coroutine xfadeCo;
    private GameState currentState = (GameState)(-1);

    // SFX
    [System.Serializable]
    public class SfxDef
    {
        public SfxEvent eventType;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.98f, 1.02f);
        public float minInterval = 0.03f;
        public bool spatial = false;
    }

    [Header("SFX")]
    [SerializeField] private SfxDef[] sfxDefs;
    [SerializeField] private int sfxVoices = 8;
    [SerializeField] private float sfx3DSpatialBlend = 1f;

    private readonly Dictionary<SfxEvent, SfxDef> sfxMap = new();
    private readonly Dictionary<SfxEvent, float> lastPlay = new();
    private AudioSource[] sfxPool;
    private int sfxIndex;
    void Start()
    {
        OnGameStateChanged(GameState.Boot);
    }
    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Music + stinger
        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        stinger = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { a, b })
        {
            src.outputAudioMixerGroup = FindGroup("Music");
            src.loop = true;
            src.playOnAwake = false;
        }
        stinger.outputAudioMixerGroup = FindGroup("SFX");
        stinger.playOnAwake = false;

        // SFX setup
        foreach (var def in sfxDefs) if (def != null) sfxMap[def.eventType] = def;

        sfxPool = new AudioSource[Mathf.Max(1, sfxVoices)];
        for (int i = 0; i < sfxPool.Length; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = FindGroup("SFX");
            src.playOnAwake = false;
            sfxPool[i] = src;
        }
    }

    AudioMixerGroup FindGroup(string name)
    {
        if (mixer == null) return null;
        var groups = mixer.FindMatchingGroups(name);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }

    public void OnGameStateChanged(GameState next)
    {
        if (currentState == next) return;
        currentState = next;

        AudioClip clip = null;
        AudioMixerSnapshot snap = normalSnapshot;

        switch (next)
        {
            case GameState.Boot:
                clip = bootLoop; break;
            case GameState.Intro:
                StopMusic();
                PlayStinger(introLoop);
                if (introLoop != null)
                    StartCoroutine(CoAfterIntro(introLoop.length));
                break;
            case GameState.Playing:
                clip = normalLoop; break;
            case GameState.PowerMode:
                clip = powerLoop; snap = frightenedSnapshot; break;
            case GameState.AlienDead:
                clip = killLoop; break;
            case GameState.LevelCleared:
                StopMusic();
                PlayStinger(levelClear);
                return;
            case GameState.Dying:
                StopMusic();
                PlayStinger(deathLoop);
                return;
        }

        if (snap != null) snap.TransitionTo(0.1f);
        if (clip != null) CrossfadeTo(clip, defaultFade);
    }

    public void PlayStinger(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        stinger.loop = false;
        stinger.clip = clip;
        stinger.volume = volume;
        stinger.Play();
    }

    void CrossfadeTo(AudioClip nextClip, float fade)
    {
        if (nextClip == null) return;
        if (xfadeCo != null) StopCoroutine(xfadeCo);
        xfadeCo = StartCoroutine(CoCrossfade(nextClip, fade));
    }

    IEnumerator CoCrossfade(AudioClip nextClip, float fade)
    {
        var next = b; var cur = a;
        next.clip = nextClip;
        next.time = 0f;
        next.volume = 0f;
        next.loop = true;
        next.Play();

        float t = 0f;
        float dur = Mathf.Max(0.01f, fade);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            next.volume = k;
            cur.volume = 1f - k;
            yield return null;
        }
        cur.Stop();
        next.volume = 1f;

        var tmp = a; a = b; b = tmp;
        xfadeCo = null;
    }

    private void StopMusic()
    {
        if (a.isPlaying) a.Stop();
        if (b.isPlaying) b.Stop();
        if (xfadeCo != null) { StopCoroutine(xfadeCo); xfadeCo = null; }
    }

    // Public SFX Method
    public void PlaySfx(SfxEvent evt, GameObject caller = null, Vector3? worldPos = null, float volMul = 1f)
    {
        if (caller && caller.TryGetComponent<AudioToggle>(out var toggle) && toggle.muteAudio)
            return;

        if (!sfxMap.TryGetValue(evt, out var def) || def.clips == null || def.clips.Length == 0) return;

        float now = Time.unscaledTime;
        if (lastPlay.TryGetValue(evt, out float last) && now - last < def.minInterval) return;
        lastPlay[evt] = now;

        var clip = def.clips[Random.Range(0, def.clips.Length)];
        var src = sfxPool[sfxIndex];
        sfxIndex = (sfxIndex + 1) % sfxPool.Length;

        src.spatialBlend = def.spatial ? sfx3DSpatialBlend : 0f;
        src.pitch = Random.Range(def.pitchRange.x, def.pitchRange.y);
        src.PlayOneShot(clip, def.volume * Mathf.Clamp01(volMul));
    }
    IEnumerator CoAfterIntro(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnGameStateChanged(GameState.Playing);
    }

    // Volume helpers
    public void SetMusicDb(float db) => mixer?.SetFloat("MusicVolume", db);
    public void SetSfxDb(float db) => mixer?.SetFloat("SfxVolume", db);
}
