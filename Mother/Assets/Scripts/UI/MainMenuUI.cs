using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainPanel;
    public GameObject loadPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject newGamePanel;
    
    [Header("Main Menu Buttons")]
    public Button newGameButton;
    public Button loadGameButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;
    
    [Header("New Game Options")]
    public InputField playerNameInput;
    public Dropdown difficultyDropdown;
    public Button startGameButton;
    public Button cancelNewGameButton;
    
    [Header("Load Game Slots")]
    public SaveSlot[] saveSlots;
    
    [System.Serializable]
    public class SaveSlot
    {
        public GameObject slotPanel;
        public Text slotInfoText;
        public Button loadButton;
        public Button deleteButton;
        public int slotIndex;
    }
    
    [Header("Settings")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;
    public Toggle vibrationToggle;
    public Dropdown resolutionDropdown;
    public Dropdown qualityDropdown;
    public Button applySettingsButton;
    public Button resetSettingsButton;
    
    [Header("Credits")]
    public ScrollRect creditsScroll;
    public Text creditsText;
    public float scrollSpeed = 20f;
    public Button backFromCreditsButton;
    
    [Header("Visual Effects")]
    public Image background;
    public Animator titleAnimator;
    public ParticleSystem menuParticles;
    public AudioSource menuMusic;
    
    [Header("Transitions")]
    public Animator transitionAnimator;
    public float transitionTime = 1f;
    
    private Resolution[] resolutions;
    private bool isTransitioning = false;
    
    private void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        LoadSettings();
        
        if(menuMusic != null && !menuMusic.isPlaying)
            menuMusic.Play();
    }
    
    private void Update()
    {
        if(creditsPanel.activeSelf && creditsScroll != null)
        {
            creditsScroll.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
            
            if(creditsScroll.verticalNormalizedPosition <= 0)
            {
                creditsScroll.verticalNormalizedPosition = 1;
            }
        }
        
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }
    
    private void InitializeUI()
    {
        mainPanel.SetActive(true);
        loadPanel.SetActive(false);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        newGamePanel.SetActive(false);
        
        InitializeResolutionDropdown();
        InitializeQualityDropdown();
        UpdateSaveSlots();
    }
    
    private void SetupButtonListeners()
    {
        newGameButton.onClick.AddListener(ShowNewGamePanel);
        loadGameButton.onClick.AddListener(ShowLoadPanel);
        settingsButton.onClick.AddListener(ShowSettingsPanel);
        creditsButton.onClick.AddListener(ShowCreditsPanel);
        quitButton.onClick.AddListener(QuitGame);
        
        startGameButton.onClick.AddListener(StartNewGame);
        cancelNewGameButton.onClick.AddListener(HideNewGamePanel);
        
        applySettingsButton.onClick.AddListener(ApplySettings);
        resetSettingsButton.onClick.AddListener(ResetSettings);
        
        backFromCreditsButton.onClick.AddListener(HideCreditsPanel);
    }
    
    private void InitializeResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        int currentResolutionIndex = 0;
        List<string> options = new List<string>();
        
        for(int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);
            
            if(resolutions[i].width == Screen.currentResolution.width &&
               resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }
    
    private void InitializeQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        
        string[] qualityLevels = QualitySettings.names;
        List<string> options = new List<string>(qualityLevels);
        
        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }
    
    private void UpdateSaveSlots()
    {
        foreach(SaveSlot slot in saveSlots)
        {
            string savePath = Application.persistentDataPath + $"/save_{slot.slotIndex}.dat";
            
            if(System.IO.File.Exists(savePath))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(savePath);
                slot.slotInfoText.text = $"Slot {slot.slotIndex}\n" +
                                        $"Date: {fileInfo.LastWriteTime:MM/dd/yyyy}\n" +
                                        $"Size: {fileInfo.Length / 1024} KB";
                
                slot.loadButton.onClick.RemoveAllListeners();
                slot.loadButton.onClick.AddListener(() => LoadGame(slot.slotIndex));
                slot.loadButton.interactable = true;
                
                slot.deleteButton.onClick.RemoveAllListeners();
                slot.deleteButton.onClick.AddListener(() => DeleteSave(slot.slotIndex));
                slot.deleteButton.interactable = true;
            }
            else
            {
                slot.slotInfoText.text = $"Empty Slot {slot.slotIndex}";
                slot.loadButton.interactable = false;
                slot.deleteButton.interactable = false;
            }
        }
    }
    
    public void ShowNewGamePanel()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        newGamePanel.SetActive(true);
        playerNameInput.text = "Hacker_" + Random.Range(1000, 9999);
    }
    
    public void HideNewGamePanel()
    {
        PlayButtonSound();
        newGamePanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    
    public void ShowLoadPanel()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        loadPanel.SetActive(true);
        UpdateSaveSlots();
    }
    
    public void HideLoadPanel()
    {
        PlayButtonSound();
        loadPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    
    public void ShowSettingsPanel()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public void HideSettingsPanel()
    {
        PlayButtonSound();
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    
    public void ShowCreditsPanel()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
        
        if(creditsScroll != null)
            creditsScroll.verticalNormalizedPosition = 1;
    }
    
    public void HideCreditsPanel()
    {
        PlayButtonSound();
        creditsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    
    public void StartNewGame()
    {
        if(string.IsNullOrWhiteSpace(playerNameInput.text))
            return;
        
        PlayButtonSound();
        
        PlayerPrefs.SetString("PlayerName", playerNameInput.text);
        PlayerPrefs.SetInt("Difficulty", difficultyDropdown.value);
        PlayerPrefs.Save();
        
        StartCoroutine(LoadGameScene());
    }
    
    public void LoadGame(int slotIndex)
    {
        PlayButtonSound();
        
        PlayerPrefs.SetInt("CurrentSaveSlot", slotIndex);
        PlayerPrefs.Save();
        
        StartCoroutine(LoadGameScene());
    }
    
    public void DeleteSave(int slotIndex)
    {
        PlayButtonSound();
        
        string savePath = Application.persistentDataPath + $"/save_{slotIndex}.dat";
        
        if(System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
            UpdateSaveSlots();
        }
    }
    
    private IEnumerator LoadGameScene()
    {
        if(isTransitioning) yield break;
        
        isTransitioning = true;
        
        if(transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
        }
        
        StartCoroutine(FadeOutMusic(transitionTime));
        
        yield return new WaitForSeconds(transitionTime);
        
        SceneManager.LoadScene("Gameplay");
    }
    
    private void LoadSettings()
    {
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        
        fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        vibrationToggle.isOn = PlayerPrefs.GetInt("Vibration", 1) == 1;
        
        ApplyAudioSettings();
    }
    
    public void ApplySettings()
    {
        PlayButtonSound();
        
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("Vibration", vibrationToggle.isOn ? 1 : 0);
        
        Resolution resolution = resolutions[resolutionDropdown.value];
        Screen.SetResolution(resolution.width, resolution.height, fullscreenToggle.isOn);
        
        QualitySettings.SetQualityLevel(qualityDropdown.value);
        
        PlayerPrefs.Save();
        ApplyAudioSettings();
        ShowNotification("Settings applied!");
    }
    
    public void ResetSettings()
    {
        PlayButtonSound();
        
        musicSlider.value = 0.7f;
        sfxSlider.value = 0.8f;
        fullscreenToggle.isOn = true;
        vibrationToggle.isOn = true;
        resolutionDropdown.value = resolutions.Length - 1;
        qualityDropdown.value = QualitySettings.names.Length - 1;
        
        ApplySettings();
    }
    
    private void ApplyAudioSettings()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if(audioManager != null)
        {
            audioManager.SetMusicVolume(musicSlider.value);
            audioManager.SetSFXVolume(sfxSlider.value);
        }
        
        if(menuMusic != null)
        {
            menuMusic.volume = musicSlider.value;
        }
    }
    
    private void HandleBackButton()
    {
        if(loadPanel.activeSelf)
        {
            HideLoadPanel();
        }
        else if(settingsPanel.activeSelf)
        {
            HideSettingsPanel();
        }
        else if(creditsPanel.activeSelf)
        {
            HideCreditsPanel();
        }
        else if(newGamePanel.activeSelf)
        {
            HideNewGamePanel();
        }
        else
        {
            QuitGame();
        }
    }
    
    public void QuitGame()
    {
        PlayButtonSound();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void PlayButtonSound()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if(audioManager != null)
        {
            audioManager.PlayButtonClick();
        }
    }
    
    private IEnumerator FadeOutMusic(float duration)
    {
        if(menuMusic == null) yield break;
        
        float startVolume = menuMusic.volume;
        float time = 0f;
        
        while(time < duration)
        {
            time += Time.deltaTime;
            menuMusic.volume = Mathf.Lerp(startVolume, 0, time / duration);
            yield return null;
        }
        
        menuMusic.Stop();
        menuMusic.volume = startVolume;
    }
    
    private void ShowNotification(string message)
    {
        Debug.Log(message);
    }
    
    public void OnTitleAnimationComplete()
    {
        if(menuParticles != null && !menuParticles.isPlaying)
            menuParticles.Play();
    }
}