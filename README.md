# Circuit Forge

A native Windows embedded-learning IDE prototype built with WPF and .NET.

## What it does

Circuit Forge is a Windows desktop app for learning embedded development with a cleaner IDE-style interface over PlatformIO. It includes:

- A starter Arduino/PlatformIO project
- A code editor panel for `src/main.cpp`, `platformio.ini`, and notes
- Device-family board selection backed by `pio boards --json-output`, with a small built-in fallback list while the full PlatformIO catalog loads
- Real PlatformIO-backed Verify, Upload, and Serial buttons
- A release icon for the app and executable

## Requirements

- Windows
- .NET 8 SDK for development
- PlatformIO Core for build/upload/serial actions

PlatformIO is discovered from `PATH` or the common Python Store install location used on this machine.

## Build

```powershell
dotnet build
```

If you are using the local SDK installed during setup:

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet-home"
C:\tmp\dotnet\dotnet.exe build
```

## Publish

```powershell
.\scripts\publish.ps1
```

The generated app bundle is written to:

```text
Release\CircuitForge.exe
Release\AppIcon.ico
Release\AppIcon.png
Release\Projects\StarterKit
```

`Release/` is intentionally ignored by Git. Commit the source files, then attach the generated exe to a GitHub release when you want to distribute it.

## Project Data

When launched, the app creates or uses this project beside the exe:

```text
Projects\StarterKit
```

The starter source lives in `StarterKit/` in the repo.

## Git Notes

The repo tracks source, icons, and the starter project. It ignores build outputs, local SDK state, PlatformIO caches, and generated release binaries.
