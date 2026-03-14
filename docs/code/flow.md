Single scene: Main

Flow: Main scene loads → Global persists data → GameManager inits level → MainUI shows → Player interacts → Win/Lose → EndScreen → Next Level or Replay (reloads Main)

Global.cs (singleton, DontDestroyOnLoad):
- Stores CurrentLevel (1-3), MaxLevel, GameState
- SelectLevel(int) sets current level
- Survives scene reloads, duplicates destroyed in Awake

GameManager.cs (singleton, in Main scene):
- Awake: loads all LevelData from Resources
- Start: InitLevel() reads Global.CurrentLevel, finds matching LevelData
- StartLevel(): sets state to Playing (block interaction = TODO)
- TriggerWin() / TriggerLose(): sets state, shows EndScreenUI
- LoadNextLevel(): sets next level on Global, reloads Main
- ReloadCurrentLevel(): reloads Main

MainUI.cs (singleton, auto-wires by name):
- Start: displays Global.CurrentLevel in TxtSelectedLevel (TextMeshProUGUI)
- BtnLevel1/2/3: sets level on Global via SelectLevel, reloads Main scene
- BtnStart: calls GameManager.StartLevel() (state change)
- BtnForceWin / BtnForceLose: calls GameManager.TriggerWin/TriggerLose

EndScreenUI.cs (singleton, auto-wires by name):
- Hidden by default (Awake calls Hide)
- Show(bool isWin): displays PanelEndScreen, sets TxtResult (TextMeshProUGUI) to "Congratulations!" or "Game Over"
- BtnNextLevel: calls GameManager.LoadNextLevel()
- BtnReplay: calls GameManager.ReloadCurrentLevel()

LevelData.cs (ScriptableObject):
- Create via Assets > Create > Game > Level Data
- Fields: LevelNumber, LevelName, TimeLimitSeconds
- Stored in Assets/Resources/LevelData/

UI on Canvas (auto-wired by name, no Inspector dragging):
- MainUI children: BtnLevel1, BtnLevel2, BtnLevel3, BtnStart, BtnForceWin, BtnForceLose, TxtSelectedLevel
- EndScreenUI children: PanelEndScreen, TxtResult, BtnNextLevel, BtnReplay

All data stored on Global.cs
All gameplay logic under GameManager.cs
Scripts under Assets/Scripts
Scenes under Assets/Scenes