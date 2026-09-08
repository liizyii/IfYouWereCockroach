# Release Play Instructions

This repository contains the Unity source project. Players do not need Unity, Blender, Git, or Visual Studio Code when they use a release build.

## Windows Build

Build with:

```powershell
& "D:\Unity\Editors\Editor\Unity.exe" -batchmode -quit -projectPath "D:\Unity\IfYouWereCockroach" -executeMethod IfYouWereCockroach.EditorTools.BuildRelease.BuildWindowsReleaseFromCommandLine -outputPath "Builds\Windows\IfYouWereCockroach.exe"
```

Zip the `Builds/Windows` folder as `Releases/IfYouWereCockroach-试玩版-Windows.zip`. Players only need to extract the zip and double-click `IfYouWereCockroach.exe`.

## Web Version

Build with:

```powershell
& "D:\Unity\Editors\Editor\Unity.exe" -batchmode -quit -buildTarget WebGL -projectPath "D:\Unity\IfYouWereCockroach" -executeMethod IfYouWereCockroach.EditorTools.BuildRelease.BuildWebGLReleaseFromCommandLine -outputPath "Builds\WebGL试玩版"
```

Publish the contents of `Builds/WebGL试玩版` to the repository's `gh-pages` branch. Players can open the browser version at:

https://liizyii.github.io/IfYouWereCockroach/

## Controls

- `WASD`: move
- Mouse: turn
- `Shift`: sprint
- `Space`: jump
- `E`: lay eggs while hidden
- `R`: restart
- `Q`: quit the release build
- `Esc`: release mouse cursor
