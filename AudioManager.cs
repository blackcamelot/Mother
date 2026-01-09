using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public bool loop = false;
        public bool isMusic = false;
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        
        [HideInInspector]
        public AudioSource source;
    }
    
    [Header("Audio Settings")]
    public List<Sound> sounds = new List<Sound>();
    
    [Header("Volume Controls")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    
    [Header("Audio Sources")]
    public int maxConcurrentSFX = 10;
    
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    private Queue<AudioSource> availableSFXSources = new Queue<AudioSource>();
    private List<AudioSource> activeSFXSources = new List<AudioSource>();
    private AudioSource musicSource;
    
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void InitializeAudioSources()
    {
        GameObject musicObject = new GameObject("MusicSource");
        musicObject.transform.parent = transform;
        musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        
        for(int i = 0; i < maxConcurrentSFX; i++)
        {
            GameObject sfxObject = new GameObject($"SFXSource_{i}");
            sfxObject.transform.parent = transform;
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            availableSFXSources.Enqueue(source);
        }
        
        foreach(Sound sound in sounds)
        {
            soundDictionary[sound.name] = sound;
            
            if(sound.isMusic)
            {
                sound.source = musicSource;
            }
        }
    }
    
    private void Update()
    {
        UpdateVolumes();
        CleanupFinishedSFX();
    }
    
    public void Play(string soundName)
    {
        if(soundDictionary.ContainsKey(soundName))
        {
            Sound sound = soundDictionary[soundName];
            
            if(sound.isMusic)
            {
                PlayMusic(sound);
            }
            else
            {
                PlaySFX(sound);
            }
        }
        else
        {
            Debug.LogWarning($"Sound not found: {soundName}");
        }
    }
    
    private void PlayMusic(Sound music)
    {
        if(musicSource.isPlaying)
            musicSource.Stop();
            
        musicSource.clip = music.clip;
        musicSource.volume = music.volume * masterVolume * musicVolume;
        musicSource.pitch = music.pitch;
        musicSource.loop = music.loop;
        musicSource.Play();
    }
    
    private void PlaySFX(Sound sfx)
    {
        if(availableSFXSources.Count == 0)
        {
            Debug.LogWarning("No available SFX sources!");
            return;
        }
        
        AudioSource source = availableSFXSources.Dequeue();
        activeSFXSources.Add(source);
        
        source.clip = sfx.clip;
        source.volume = sfx.volume * masterVolume * sfxVolume;
        source.pitch = sfx.pitch;
        source.loop = sfx.loop;
        source.Play();
        
        if(!sfx.loop)
        {
            StartCoroutine(ReturnSFXSource(source, sfx.clip.length));
        }
    }
    
    private System.Collections.IEnumerator ReturnSFXSource(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if(source != null)
        {
            source.Stop();
            activeSFXSources.Remove(source);
            availableSFXSources.Enqueue(source);
        }
    }
    
    public void StopMusic()
    {
        if(musicSource.isPlaying)
            musicSource.Stop();
    }
    
    public void StopAllSFX()
    {
        foreach(AudioSource source in activeSFXSources.ToList())
        {
            source.Stop();
            activeSFXSources.Remove(source);
            availableSFXSources.Enqueue(source);
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
        SaveVolumeSettings();
    }
    
    private void UpdateVolumes()
    {
        if(musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        
        foreach(AudioSource source in activeSFXSources)
        {
            if(source != null)
            {
                string clipName = source.clip.name;
                Sound sound = sounds.Find(s => s.clip.name == clipName);
                if(sound != null)
                {
                    source.volume = sound.volume * masterVolume * sfxVolume;
                }
            }
        }
    }
    
    private void CleanupFinishedSFX()
    {
        for(int i = activeSFXSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeSFXSources[i];
            
            if(source != null && !source.isPlaying && !source.loop)
            {
                source.Stop();
                activeSFXSources.RemoveAt(i);
                availableSFXSources.Enqueue(source);
            }
        }
    }
    
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }
    
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }
    
    public void PlayButtonClick()
    {
        Play("ui_button");
    }
    
    public void PlayTerminalTyping()
    {
        Play("terminal_type");
    }
    
    public void PlayHackSuccess()
    {
        Play("terminal_success");
    }
}