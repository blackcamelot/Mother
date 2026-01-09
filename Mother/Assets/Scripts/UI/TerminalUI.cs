using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TerminalUI : MonoBehaviour
{
    private List<string> messageHistory = new List<string>();
    private const int MAX_HISTORY = 50;
    
    // Implementa singleton se necessario
    public static TerminalUI Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    [Header("Main Components")]
    public TerminalController terminalController;
    public CanvasGroup terminalCanvasGroup;
    public RectTransform terminalWindow;
    
    [Header("Visual Elements")]
    public Image background;
    public Color backgroundColor = new Color(0, 0.1f, 0, 0.9f);
    public Color textColor = Color.green;
    public Font terminalFont;
    
    [Header("Animations")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 0.3f;
    public bool isVisible = true;
    
    [Header("Effects")]
    public GameObject cursorBlinker;
    public float cursorBlinkRate = 0.5f;
    public bool showScanLines = true;
    public Image scanLinesOverlay;
    public float scanLineSpeed = 50f;
    
    [Header("Mobile Keyboard")]
    public TouchScreenKeyboard mobileKeyboard;
    public bool useMobileKeyboard = true;
    public Button showKeyboardButton;
    public Button hideKeyboardButton;
    
    [Header("Quick Commands")]
    public GameObject quickCommandsPanel;
    public List<Button> quickCommandButtons;
    
    }

    private Dictionary<string, string> quickCommands = new Dictionary<string, string>()
    {
        {"scan", "scan"},
        {"help", "help"},
        {"clear", "clear"},
        {"connect", "connect "},
        {"crack", "crack "}
    };
    
    private void Start()
    {
        InitializeUI();
        SetupQuickCommands();
        
        if(useMobileKeyboard && Application.isMobilePlatform)
        {
            SetupMobileKeyboard();
        }
    }
    
    private void InitializeUI()
    {
        if(background != null)
            background.color = backgroundColor;
        
        if(scanLinesOverlay != null)
            scanLinesOverlay.gameObject.SetActive(showScanLines);
        
        if(cursorBlinker != null)
            InvokeRepeating("ToggleCursor", 0f, cursorBlinkRate);
        
        if(terminalFont != null && terminalController.terminalOutput != null)
        {
            terminalController.terminalOutput.font = terminalFont;
        }
    }
    
    private void SetupQuickCommands()
    {
        if(quickCommandsPanel == null || quickCommandButtons.Count == 0)
            return;
            
        for(int i = 0; i < quickCommandButtons.Count && i < quickCommands.Count; i++)
        {
            int index = i;
            string commandKey = new List<string>(quickCommands.Keys)[i];
            string commandValue = quickCommands[commandKey];
            
            Text buttonText = quickCommandButtons[i].GetComponentInChildren<Text>();
            if(buttonText != null)
                buttonText.text = commandKey.ToUpper();
            
            quickCommandButtons[i].onClick.AddListener(() => ExecuteQuickCommand(commandValue));
        }
    }
    
    private void SetupMobileKeyboard()
    {
        if(showKeyboardButton != null)
        {
            showKeyboardButton.onClick.AddListener(ShowMobileKeyboard);
            showKeyboardButton.gameObject.SetActive(true);
        }
        
        if(hideKeyboardButton != null)
        {
            hideKeyboardButton.onClick.AddListener(HideMobileKeyboard);
            hideKeyboardButton.gameObject.SetActive(false);
        }
    }


    public void AddMessage(string message) 
    {
        messageHistory.Add(message);
        if (messageHistory.Count > MAX_HISTORY) 
        {
            messageHistory.RemoveAt(0);
        }
        UpdateDisplay();
    }
    
    public void ClearConsole() 
    {
        messageHistory.Clear();
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Implementa l'aggiornamento della UI
        if (terminalController != null && terminalController.terminalOutput != null)
        {
            terminalController.terminalOutput.text = string.Join("\n", messageHistory);
        }
    }

    
    private void Update()
    {
        if(showScanLines && scanLinesOverlay != null)
        {
            Vector2 offset = scanLinesOverlay.material.mainTextureOffset;
            offset.y += Time.deltaTime * scanLineSpeed;
            scanLinesOverlay.material.mainTextureOffset = offset;
        }
        
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleTerminal();
        }
    }
    
    public void ToggleTerminal()
    {
        isVisible = !isVisible;
        
        if(isVisible)
        {
            ShowTerminal();
        }
        else
        {
            HideTerminal();
        }
    }
    
    public void ShowTerminal()
    {
        isVisible = true;
        StartCoroutine(FadeTerminal(1f, fadeInTime));
        terminalController.commandInput.ActivateInputField();
        
        if(useMobileKeyboard && Application.isMobilePlatform)
        {
            ShowMobileKeyboard();
        }
    }
    
    public void HideTerminal()
    {
        isVisible = false;
        StartCoroutine(FadeTerminal(0f, fadeOutTime));
        
        if(mobileKeyboard != null)
        {
            mobileKeyboard.active = false;
        }
    }
    
    private System.Collections.IEnumerator FadeTerminal(float targetAlpha, float duration)
    {
        if(terminalCanvasGroup == null)
            yield break;
            
        float startAlpha = terminalCanvasGroup.alpha;
        float time = 0f;
        
        while(time < duration)
        {
            time += Time.deltaTime;
            terminalCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        
        terminalCanvasGroup.alpha = targetAlpha;
        terminalCanvasGroup.interactable = targetAlpha > 0.5f;
        terminalCanvasGroup.blocksRaycasts = targetAlpha > 0.5f;
    }
    
    private void ToggleCursor()
    {
        if(cursorBlinker != null)
            cursorBlinker.SetActive(!cursorBlinker.activeSelf);
    }
    
    public void ExecuteQuickCommand(string command)
    {
        if(!string.IsNullOrEmpty(command) && terminalController != null)
        {
            terminalController.commandInput.text = command;
            
            if(command.EndsWith(" "))
            {
                terminalController.commandInput.ActivateInputField();
                terminalController.commandInput.caretPosition = command.Length;
            }
            else
            {
                terminalController.OnCommandEntered(command);
            }
        }
    }

    public void ProcessInput()
    {
        // Implementa la logica per processare l'input
        if (terminalController != null)
        {
            terminalController.OnCommandEntered(terminalController.commandInput.text);
        }
    }
    
    public void AppendToInput(string text)
    {
        if (terminalController != null && terminalController.commandInput != null)
        {
            terminalController.commandInput.text += text;
        }
    }
    
    public void ShowMobileKeyboard()
    {
        if(TouchScreenKeyboard.isSupported && terminalController != null)
        {
            mobileKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            
            if(showKeyboardButton != null)
                showKeyboardButton.gameObject.SetActive(false);
                
            if(hideKeyboardButton != null)
                hideKeyboardButton.gameObject.SetActive(true);
        }
    }
    
    public void HideMobileKeyboard()
    {
        if(mobileKeyboard != null)
        {
            mobileKeyboard.active = false;
        }
        
        if(showKeyboardButton != null)
            showKeyboardButton.gameObject.SetActive(true);
            
        if(hideKeyboardButton != null)
            hideKeyboardButton.gameObject.SetActive(false);
    }
    
    public void ToggleQuickCommands()
    {
        if(quickCommandsPanel != null)
        {
            quickCommandsPanel.SetActive(!quickCommandsPanel.activeSelf);
        }
    }
    
    public void SetTerminalTheme(Color bgColor, Color txtColor)
    {
        backgroundColor = bgColor;
        textColor = txtColor;
        
        if(background != null)
            background.color = backgroundColor;
            
        if(terminalController.terminalOutput != null)
            terminalController.terminalOutput.color = textColor;
    }
    
    public void ApplyMatrixTheme()
    {
        SetTerminalTheme(new Color(0, 0.05f, 0, 0.95f), Color.green);
    }
    
    public void ApplyDarkTheme()
    {
        SetTerminalTheme(new Color(0.1f, 0.1f, 0.1f, 0.95f), Color.white);
    }
    
    public void ApplyBlueTheme()
    {
        SetTerminalTheme(new Color(0, 0, 0.1f, 0.95f), Color.cyan);
    }
    
    public void PulseTerminal(float intensity = 1.5f, float duration = 0.1f)
    {
        StartCoroutine(PulseEffect(intensity, duration));
    }
    
    private System.Collections.IEnumerator PulseEffect(float intensity, float duration)
    {
        if(terminalWindow == null) yield break;
        
        Vector3 originalScale = terminalWindow.localScale;
        Vector3 targetScale = originalScale * intensity;
        
        float time = 0f;
        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            terminalWindow.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        time = 0f;
        while(time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            terminalWindow.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        terminalWindow.localScale = originalScale;
    }
}