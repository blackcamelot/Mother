using UnityEngine;
using System.Collections.Generic;
using System;

private bool isDestroying = false; 

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public bool loop = false;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        
        [HideInInspector]
        public AudioSource source;
    }

    [SerializeField] private Sound[] sounds;
    private Dictionary<string, Sound> soundDictionary;
    
    [SerializeField, Range(0f, 1f)] 
    private float masterVolume = 1f;
    
    private const string VOLUME_KEY = "MasterVolume";

    private void Awake()
    {
        // Implementazione Singleton robusta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persistenza tra scene
        
        InitializeAudioManager();
    }

    private void InitializeAudioManager()
    {
        // Carica volume salvato
        masterVolume = PlayerPrefs.GetFloat(VOLUME_KEY, 0.7f);
        
        // Inizializza dizionario
        soundDictionary = new Dictionary<string, Sound>();
        
        // Crea AudioSource per ogni suono
        foreach (Sound sound in sounds)
        {
            if (string.IsNullOrEmpty(sound.name) || sound.clip == null)
            {
                Debug.LogWarning($"AudioManager: Sound config invalida - nome: {sound.name}");
                continue;
            }
            
            if (soundDictionary.ContainsKey(sound.name))
            {
                Debug.LogWarning($"AudioManager: Duplicato nome sound: {sound.name}");
                continue;
            }
            
            GameObject soundObject = new GameObject($"Sound_{sound.name}");
            soundObject.transform.SetParent(transform);
            
            AudioSource source = soundObject.AddComponent<AudioSource>();
            source.clip = sound.clip;
            source.volume = sound.volume * masterVolume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;
            source.playOnAwake = false;
            
            sound.source = source;
            soundDictionary[sound.name] = sound;
        }
        
        Debug.Log($"AudioManager inizializzato con {soundDictionary.Count} suoni");
    }

    public void Play(string soundName)
   {
      if (isDestroying) return; // Previene azioni durante la distruzione
        {
            Debug.LogWarning($"AudioManager: Sound non trovato - {soundName}");
            return;
        }
        
        if (sound.source == null)
        {
            Debug.LogError($"AudioManager: AudioSource null per - {soundName}");
            return;
        }
        
        sound.source.Play();
    }

    public void Stop(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound))
        {
            sound.source.Stop();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Aggiorna volume di tutti i suoni
        foreach (Sound sound in soundDictionary.Values)
        {
            if (sound.source != null)
            {
                sound.source.volume = sound.volume * masterVolume;
            }
        }
        
        // Salva preferenza
        PlayerPrefs.SetFloat(VOLUME_KEY, masterVolume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume() => masterVolume;

    // Metodi per gestione avanzata
    public void PlayOneShot(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound sound) && sound.source != null)
        {
            sound.source.PlayOneShot(sound.clip, sound.volume * masterVolume);
        }
    }

    public void PauseAll()
    {
        foreach (Sound sound in soundDictionary.Values)
        {
            if (sound.source != null && sound.source.isPlaying)
            {
                sound.source.Pause();
            }
        }
    }

   public void ResumeAll()
  {
      foreach (Sound sound in soundDictionary.Values)
      {
          if (sound.source != null && !sound.source.isPlaying)
          {
              sound.source.Play(); // Sostituisce UnPause()
          }
      }
  }

    public void StopAll()
    {
        foreach (Sound sound in soundDictionary.Values)
        {
            if (sound.source != null)
            {
                sound.source.Stop();
            }
        }
    }

    private void OnDestroy()
  {
      isDestroying = true; // Imposta il flag
      if (Instance == this)
      {
          StopAll();
          Instance = null;
      }
  }

}
