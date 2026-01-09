#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class AndroidBuildConfig : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    
    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            ConfigureAndroidBuild();
        }
    }
    
    private void ConfigureAndroidBuild()
    {
        Debug.Log("Configuring Android build settings...");
        
        // Player Settings
        PlayerSettings.companyName = "MotherHackingStudio";
        PlayerSettings.productName = "Mother Hacking Simulation";
        PlayerSettings.bundleIdentifier = "com.motherhacking.simulation";
        PlayerSettings.version = "1.0.0";
        
        // Android Settings
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        
        // Graphics Settings
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new UnityEngine.Rendering.GraphicsDeviceType[] {
            UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3,
            UnityEngine.Rendering.GraphicsDeviceType.Vulkan
        });
        
        // Other Settings
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.PreferExternal;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        
        // Splash Screen (optional)
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        
        // IL2CPP Configuration
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        
        Debug.Log("Android build configuration complete");
    }
}

[InitializeOnLoad]
public class AndroidConfigEditor
{
    static AndroidConfigEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Optional: Reset settings when exiting play mode
        }
    }
}
#endif