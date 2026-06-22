using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameBackgroundMusic : MonoBehaviour
{
    private enum TrackId
    {
        None,
        SlowStride,
        CalmLoop,
        LevelLoop
    }

    private const float MusicVolume = 0.3f;

    private static readonly IReadOnlyDictionary<string, TrackId> SceneTracks =
        new Dictionary<string, TrackId>
        {
            { "MainMenu", TrackId.LevelLoop },
            { "Day1_Hospital", TrackId.SlowStride },
            { "Day2_Home", TrackId.LevelLoop },
            { "Day2_LancingStep", TrackId.LevelLoop },
            { "Day2_Stage1_PenAssembly", TrackId.LevelLoop },
            { "Day2_Stage2_InsertStrip", TrackId.LevelLoop },
            { "Day2_Stage3_Puncture", TrackId.LevelLoop },
            { "Day2_Stage4_SpaceMiniGame", TrackId.LevelLoop },
            { "Day2_DietMatch_Lunch", TrackId.LevelLoop },
            { "Day3_Home", TrackId.CalmLoop },
            { "Day3_DietMatch_Breakfast", TrackId.LevelLoop },
            { "Day4_Home", TrackId.LevelLoop },
            { "Day4_Stage1_PenAssembly", TrackId.LevelLoop },
            { "Day4_Stage2_InsertStrip", TrackId.LevelLoop },
            { "Day4_Stage3_Puncture", TrackId.LevelLoop },
            { "Day4_Stage4_SpaceMiniGame", TrackId.LevelLoop },
            { "Day4_Runner", TrackId.CalmLoop },
            { "Day4_DietMatch_Dinner", TrackId.LevelLoop },
            { "Day5_Rhythm", TrackId.CalmLoop },
            { "Result", TrackId.LevelLoop },
            { "SampleScene", TrackId.LevelLoop }
        };

    private AudioSource musicSource;
    private AudioListener fallbackListener;
    private AudioClip slowStride;
    private AudioClip calmLoop;
    private AudioClip levelLoop;
    private TrackId currentTrack;
    private bool initialized;

    public static GameBackgroundMusic Ensure(GameObject owner)
    {
        GameBackgroundMusic music = owner.GetComponent<GameBackgroundMusic>();
        if (music == null)
        {
            music = owner.AddComponent<GameBackgroundMusic>();
        }

        music.Initialize();
        return music;
    }

    private void Initialize()
    {
        if (initialized)
        {
            PlayForScene(SceneManager.GetActiveScene().name);
            return;
        }

        initialized = true;
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = MusicVolume;
        musicSource.spatialBlend = 0f;
        musicSource.priority = 128;

        fallbackListener = GetComponent<AudioListener>();
        if (fallbackListener == null)
        {
            fallbackListener = gameObject.AddComponent<AudioListener>();
        }

        slowStride = Resources.Load<AudioClip>("Audio/Music/SlowStride");
        calmLoop = Resources.Load<AudioClip>("Audio/Music/CalmLoop");
        levelLoop = Resources.Load<AudioClip>("Audio/Music/LevelLoop");

        SceneManager.sceneLoaded += HandleSceneLoaded;
        PlayForScene(SceneManager.GetActiveScene().name);
        RefreshFallbackListener();
    }

    private void OnDestroy()
    {
        if (initialized)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Update()
    {
        RefreshFallbackListener();

        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
        RefreshFallbackListener();
    }

    private void PlayForScene(string sceneName)
    {
        TrackId desiredTrack = GetTrackForScene(sceneName);
        AudioClip desiredClip = GetClip(desiredTrack);
        if (desiredClip == null || musicSource == null)
        {
            Debug.LogError("BGM clip is missing for scene: " + sceneName);
            return;
        }

        if (currentTrack == desiredTrack && musicSource.clip == desiredClip)
        {
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
            return;
        }

        currentTrack = desiredTrack;
        musicSource.clip = desiredClip;
        musicSource.Play();
    }

    private static TrackId GetTrackForScene(string sceneName)
    {
        if (SceneTracks.TryGetValue(sceneName, out TrackId track))
        {
            return track;
        }

        Debug.LogWarning("No explicit BGM mapping for scene '" + sceneName + "'. Using LevelLoop.");
        return TrackId.LevelLoop;
    }

    private AudioClip GetClip(TrackId track)
    {
        switch (track)
        {
            case TrackId.SlowStride:
                return slowStride;
            case TrackId.CalmLoop:
                return calmLoop;
            case TrackId.LevelLoop:
                return levelLoop;
            default:
                return null;
        }
    }

    private void RefreshFallbackListener()
    {
        if (fallbackListener == null)
        {
            return;
        }

        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        bool hasSceneListener = false;
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener != fallbackListener && listener.enabled && listener.gameObject.activeInHierarchy)
            {
                hasSceneListener = true;
                break;
            }
        }

        fallbackListener.enabled = !hasSceneListener;
    }
}
