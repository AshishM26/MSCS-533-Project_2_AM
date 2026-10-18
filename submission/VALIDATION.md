# Validation record — October 18, 2026

Android compilation and automated tests pass. Device acceptance and the application screenshot remain incomplete.

## Environment

| Item | Version or status |
|---|---|
| OS / CPU | macOS 26.7.1, Apple Silicon arm64 |
| VS Code / Git | 1.142.0 / 2.50.1 |
| .NET SDK / runtime | 10.0.302 / 10.0.10 |
| MAUI workload | Workload set 10.0.303.1, MAUI manifest 10.0.20 |
| MAUI application packages | Microsoft.Maui.Controls and Controls.Maps 10.0.100 |
| Microsoft extensions | MAUI 1.17.289, C# Dev Kit 3.40.210, C# 2.160.4 |
| Android / Java | SDK API 36; Microsoft OpenJDK 17.0.14 used for the build |
| Emulator | Android emulator 37.2.12; Pixel 7 / Google APIs API 36 ARM64 |
| Xcode / iOS simulator | Full Xcode and `simctl` unavailable; Command Line Tools only |

## Build and test results

Set `JAVA_HOME` to the compatible local Microsoft JDK when using these commands.

| Command | Result |
|---|---|
| `dotnet restore LocationTracker.slnx` | Passed |
| `dotnet test LocationTracker.Tests/LocationTracker.Tests.csproj` | 16 passed, 0 failed |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-android -p:JavaSdkDirectory="$JAVA_HOME"` | Passed; 0 warnings, 0 errors |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64` | Blocked by missing Xcode |
| `dotnet build LocationTracker.slnx -p:JavaSdkDirectory="$JAVA_HOME"` | Android/core/tests compile; Apple targets require Xcode |
| `dotnet format LocationTracker.slnx --no-restore` | Passed |
| `python3 scripts/check_secrets.py` | Passed |

## Device validation

The application has not completed device acceptance. Android installation encountered emulator package-service errors (`Broken pipe (32)` and `Can't find service: package`). Android map tiles also require a configured Google Maps key.

The selected iOS demonstration requires full Xcode and an iOS simulator runtime. Tracking, native map rendering, centering, restart persistence, confirmed deletion, permission handling, and background-stop behavior still need the checks in [RUNTIME_GUIDE.md](RUNTIME_GUIDE.md).

## Rubric coverage

| Area | Completed | Remaining |
|---|---|---|
| Application requirements — 25 points | C#/MAUI source, SQLite, foreground tracking, density layer, controls, platform permissions | Verify the features on a running device or simulator |
| Coding standards — 50 points | MVVM, focused services, dependency injection, asynchronous cancellation, comments, formatting, 16 behavioral tests | Review any fixes arising from device validation |
| Runtime, correctness, submission — 50 points | Restore, Android build, automated tests, source/manifest documentation, GitHub repository | Successful device run, screenshot, completed Word report, Blackboard submission |

## Remaining submission steps

1. Install full Xcode and an iOS simulator runtime, then complete the runtime checklist.
2. Capture `screenshots/location-tracker-running.png` with map tiles and several recorded samples.
3. Record the results in `validation.json`, regenerate `MSCS533_Final_Project_LocationTracker_AM.docx`, and review the embedded screenshot.
4. Publish the final report and screenshot to GitHub, then submit the Word document and repository URL in Blackboard.

The current Word document contains the required ten sections, a C# excerpt, the Android manifest, and the repository link. It remains a draft until the runtime results and screenshot are included.
