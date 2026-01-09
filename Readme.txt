MOTHER HACKING SIMULATION - COMPLETE PROJECT
=============================================

PROJECT STRUCTURE:
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   ├── SaveSystem.cs
│   │   │   ├── AudioManager.cs
│   │   │   └── GameInitializer.cs
│   │   ├── Hacking/
│   │   │   ├── HackingManager.cs
│   │   │   ├── TerminalController.cs
│   │   │   ├── CommandParser.cs
│   │   │   ├── FileSystem.cs
│   │   │   ├── NetworkNode.cs
│   │   │   └── NetworkNodeGenerator.cs
│   │   ├── UI/
│   │   │   ├── UIManager.cs
│   │   │   ├── TerminalUI.cs
│   │   │   ├── MainMenuUI.cs
│   │   │   ├── DialogueSystem.cs
│   │   │   └── MobileKeyboardController.cs
│   │   ├── Missions/
│   │   │   └── MissionManager.cs
│   │   └── Utilities/
│   │   |   ├── SceneCleanup.cs
│   │   |   └── AndroidBuildConfig.cs
|   |    |---Economy/
|   |        |---BlackMarket.cs

│   ├── Scenes/
│   │   ├── MainMenu.unity (to create)
│   │   └── Gameplay.unity (to create)
│   └── Resources/
│       └── (Add fonts, sounds, prefabs as needed)

Assets/Scripts/UI/EconomyUI.cs



SETUP INSTRUCTIONS:
1. Create new Unity project (2021.3 LTS or higher)
2. Copy all scripts to respective folders
3. Create two scenes: MainMenu and Gameplay
4. In each scene, create necessary GameObjects:
   - MainMenu: Add MainMenuUI component to Canvas
   - Gameplay: Add HackingManager, TerminalController, UIManager
5. Configure UI elements in Inspector
6. Set up Build Settings for Android
7. Import TMP Essentials when prompted

KEY FEATURES:
✓ Terminal-based hacking interface
✓ Network node generation system
✓ File system simulation
✓ Mission system with objectives
✓ Save/load functionality
✓ Mobile-optimized UI with virtual keyboard
✓ Android build configuration

BUILD FOR ANDROID:
1. File → Build Settings → Android → Switch Platform
2. Configure Player Settings:
   - Company: MotherHackingStudio
   - Product: Mother Hacking Simulation
   - Package: com.motherhacking.simulation
   - Version: 1.0.0
3. Minimum SDK: Android 5.1 (API 22)
4. Build and Run

TESTING COMMANDS:
> help
> scan
> connect 192.168.1.50
> crack target-pc
> ls
> cat readme.txt
> ports
> whoami
> clear

TROUBLESHOOTING:
- If UI elements don't appear: Check Canvas settings
- If commands don't work: Verify HackingManager references
- If build fails: Check Android SDK/NDK installation
- If performance issues: Reduce number of network nodes

DEVELOPMENT NOTES:
- Extend MissionManager for more mission types
- Add minigames to CommandParser
- Create more network node types
- Implement multiplayer features
- Add story progression system

CONTROLS:
- Mouse/Touch: UI interaction
- Enter: Submit command
- Arrow keys: Command history
- Escape: Toggle terminal

CREDITS:
Game Design: Your Name
Programming: AI-Assisted Development
Assets: Unity Standard Assets
Font: Use monospace font for terminal

LICENSE:
Educational use only. Not for commercial distribution.

VERSION: 1.0.0
LAST UPDATED: 2024