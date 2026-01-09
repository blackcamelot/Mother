using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileKeyboardController : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public InputField targetInputField;
    public TouchScreenKeyboard keyboard;
    
    [Header("Settings")]
    public TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
    public bool autocorrection = true;
    public bool multiline = false;
    public bool secure = false;
    public bool alert = false;
    public string textPlaceholder = "Enter command...";
    
    [Header("Visual")]
    public Image keyboardButtonImage;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.white;
    
    private bool isKeyboardActive = false;
    
    private void Start()
    {
        if (targetInputField == null)
        {
            targetInputField = GetComponent<InputField>();
        }
        
        if (keyboardButtonImage != null)
        {
            keyboardButtonImage.color = inactiveColor;
        }
    }
    
    private void Update()
    {
        if (isKeyboardActive && keyboard != null)
        {
            if (!keyboard.active)
            {
                CloseKeyboard();
            }
            else if (targetInputField != null)
            {
                targetInputField.text = keyboard.text;
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleKeyboard();
    }
    
    public void ToggleKeyboard()
    {
        if (isKeyboardActive)
        {
            CloseKeyboard();
        }
        else
        {
            OpenKeyboard();
        }
    }
    
    public void OpenKeyboard()
    {
        if (!TouchScreenKeyboard.isSupported)
        {
            Debug.LogWarning("TouchScreenKeyboard is not supported on this device.");
            return;
        }
        
        string initialText = targetInputField != null ? targetInputField.text : "";
        
        keyboard = TouchScreenKeyboard.Open(
            initialText,
            keyboardType,
            autocorrection,
            multiline,
            secure,
            alert,
            textPlaceholder
        );
        
        isKeyboardActive = true;
        
        if (keyboardButtonImage != null)
        {
            keyboardButtonImage.color = activeColor;
        }
        
        Debug.Log("Mobile keyboard opened");
    }
    
    public void CloseKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.active = false;
        }
        
        isKeyboardActive = false;
        
        if (keyboardButtonImage != null)
        {
            keyboardButtonImage.color = inactiveColor;
        }
        
        if (targetInputField != null)
        {
            targetInputField.ActivateInputField();
        }
        
        Debug.Log("Mobile keyboard closed");
    }
    
    public void ForceCloseKeyboard()
    {
        if (keyboard != null)
        {
            keyboard.active = false;
            keyboard = null;
        }
        
        isKeyboardActive = false;
        
        if (keyboardButtonImage != null)
        {
            keyboardButtonImage.color = inactiveColor;
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isKeyboardActive)
        {
            ForceCloseKeyboard();
        }
    }
    
    private void OnDestroy()
    {
        ForceCloseKeyboard();
    }
    
    public bool IsKeyboardActive()
    {
        return isKeyboardActive;
    }
    
    public void SetTargetInputField(InputField newInputField)
    {
        if (isKeyboardActive)
        {
            ForceCloseKeyboard();
        }
        
        targetInputField = newInputField;
    }
    
    public void SubmitText()
    {
        if (targetInputField != null && !string.IsNullOrEmpty(targetInputField.text))
        {
            // Trigger the input field's submit event
            targetInputField.onEndEdit.Invoke(targetInputField.text);
        }
        
        CloseKeyboard();
    }
}