# Release Play Instructions

This repository contains the Unity source project. Players do not need Unity, Blender, Git, or Visual Studio Code when they use a release build.

## Windows Build

Build with:

```powershell
& "D:\Unity\Editors\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity\IfYouWereCockroach" -executeMethod IfYouWereCockroach.EditorTools.BuildRelease.BuildWindowsReleaseFromCommandLine -outputPath "Builds\Windows\IfYouWereCockroach.exe"
```

Zip the `Builds/Windows` folder as `Releases/IfYouWereCockroach-试玩版-Windows.zip`. Players only need to extract the zip and double-click `IfYouWereCockroach.exe`.

## Web Version

A browser-playable WebGL version is the best public link for casual players. The current local Unity installation only has Windows Standalone support installed. Install **WebGL Build Support** for Unity `2022.3.62f3c1` in Unity Hub before making the web build.

## Controls

- `WASD`: move
- Mouse: turn
- `Shift`: sprint
- `Space`: jump
- `E`: lay eggs while hidden
- `R`: restart
- `Q`: quit the release build
- `Esc`: release mouse cursor