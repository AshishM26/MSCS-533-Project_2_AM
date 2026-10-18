# Validation record — October 18, 2026

This record distinguishes implementation, automated checks, and actual device evidence. The submission is not yet complete without the final map-output screenshot.

## Environment audit and installation

| Item | Observed result |
|---|---|
| OS / CPU | macOS 26.7.1, Apple Silicon arm64 |
| VS Code / CLI | 1.142.0, arm64; `code` available |
| Git | 2.50.1 |
| .NET SDK / runtime | Stable SDK 10.0.302 / runtime 10.0.10 |
| MAUI workload | Installed and repaired; workload set 10.0.303.1, MAUI manifest 10.0.20 |
| MAUI application packages | Microsoft.Maui.Controls and Controls.Maps 10.0.100 |
| Templates | Official Microsoft.Maui.Templates.net10 10.0.110 |
| Microsoft extensions | MAUI 1.17.289, C# Dev Kit 3.40.210, C# 2.160.4 installed and verified |
| Android | SDK API 36 and build/platform tools installed through `InstallAndroidDependencies` |
| Java | Existing Oracle JDK 24.0.2; dependency installer provisioned Microsoft OpenJDK 17.0.14, used for the successful Android build |
| Emulator | Android emulator 37.2.12; dedicated Pixel 7 / Google APIs API 36 ARM64 device |
| Xcode / iOS simulator | Full Xcode absent at last verification; only Command Line Tools available; `simctl` unavailable |
| Repository | Existing `main` branch and origin preserved; selected URL confirmed by the student |

No .NET 11 preview SDK is used by the project. The first MAUI workload installation left missing packs; `dotnet workload repair` restored them, and the official .NET 10 template was registered separately.

## Commands and results

Commands below omit machine-specific JDK paths. Set `JAVA_HOME` to the compatible local Microsoft JDK.

| Command | Result |
|---|---|
| `dotnet restore LocationTracker.slnx` | Passed |
| `dotnet test LocationTracker.Tests/LocationTracker.Tests.csproj` | 16 passed, 0 failed |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-android -p:JavaSdkDirectory="$JAVA_HOME"` | Passed; 0 warnings, 0 errors |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64` | Blocked: valid Xcode installation not found |
| `dotnet build LocationTracker.slnx -p:JavaSdkDirectory="$JAVA_HOME"` | Android/core/tests compile; iOS/Mac Catalyst fail because Xcode is missing |
| `dotnet format LocationTracker.slnx --no-restore` | Passed |
| `python3 scripts/check_secrets.py` | Passed before final staging; repeat with `--staged` before every commit |

The Android package embeds managed assemblies so a Debug APK can be installed directly using `adb`. A default fast-deployment APK was initially unable to start when installed outside the IDE; the project configuration was corrected before runtime acceptance.

## Runtime evidence

Runtime acceptance is incomplete. The initial Android APK installed but exited because it expected IDE fast deployment. After embedding managed assemblies, the rebuilt APK compiled with zero warnings/errors. Reinstallation stalled in the emulator package service (`Broken pipe (32)`); a reboot and non-streaming retry then returned `Can't find service: package`. The rebuilt application has not been verified running. Tracking, on-device persistence, centering, clear-history behavior, and native map rendering remain unchecked. Portable SQLite and tracking tests passed separately.

Android map tiles also require a valid key, which is not configured. The student selected iOS for the final map demonstration and screenshot; iOS build/run awaits full Xcode and a simulator runtime. Do not substitute an emulator home-screen capture, blank map, generated mockup, or assignment reference image for the final application output.

## Deliverables and Git history

- Source and tests: GitHub commit `25a2972` (`feat: implement MAUI location tracking with SQLite and density maps`).
- Word report: `MSCS533_Final_Project_LocationTracker_AM.docx`, generated and reopened to verify all 10 required sections. It is marked DRAFT and intentionally contains no fabricated screenshot.
- Final image path reserved: `screenshots/location-tracker-running.png`; file not yet present.
- README, runtime guide, rubric checklist, reproducible Word generator, and staged secret scanner included.
- Repository: https://github.com/AshishM26/MSCS-533-Project_2_AM (main).

## Rubric audit

| Area | Evidence and remaining work | Provisional readiness |
|---|---|---|
| Application requirements (25) | C#/MAUI source, SQLite, foreground tracking, native circle density layer, controls, and platform permissions implemented. Final native map output remains to be verified. | 23/25 |
| Coding standards (50) | Focused layers, MVVM, dependency injection, async cancellation, naming/formatting, explanatory comments, error handling, 16 behavioral tests, and secret exclusions. | 47/50 |
| Runtime/correctness/submission (50) | Restore, Android build, and automated tests pass. Full application runtime acceptance, iOS execution, final screenshot, and final Word image insertion remain pending. | 25/50 |

These are conservative readiness estimates, not a predicted instructor grade. Reassess after completing the runtime checklist.

## Smallest remaining action

Install full Xcode, open it once to complete setup, and install an iOS simulator runtime. Select it with `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer` and run `sudo xcodebuild -runFirstLaunch`. Then follow `RUNTIME_GUIDE.md`, capture the real screenshot, update this record and `validation.json`, and regenerate the Word file. Android's Google Maps key is not needed for that iOS path.
