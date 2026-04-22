using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip[] gameplayTracks;

    [Header("Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource source;
    private int lastTrackIndex = -1;
    private bool inGameplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.volume = volume;
    }

    private void Start()
    {
        PlayMenuMusic();
    }

    private void Update()
    {
        if (inGameplay && !source.isPlaying) PlayRandomGameplayTrack();
    }

    public void PlayMenuMusic()
    {
        inGameplay = false;

        if (menuMusic == null) return;
        if (source.clip == menuMusic && source.isPlaying) return;

        source.clip = menuMusic;
        source.loop = true;
        source.volume = volume;
        source.Play();
    }

    public void PlayGameplayMusic()
    {
        inGameplay = true;
        source.loop = false;
        PlayRandomGameplayTrack();
    }

    public void StopMusic()
    {
        inGameplay = false;
        source.Stop();
    }

    private void PlayRandomGameplayTrack()
    {
        if (gameplayTracks == null || gameplayTracks.Length == 0) return;

        int index;
        if (gameplayTracks.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, gameplayTracks.Length); }
            while (index == lastTrackIndex);
        }

        lastTrackIndex = index;
        source.clip = gameplayTracks[index];
        source.volume = volume;
        source.Play();
    }
}
