# Validation record — October 18, 2026

Android compilation and 16 automated tests passed again. Installation, launch, GPS saving, and restart persistence are now verified; full map acceptance and the final screenshot remain incomplete.

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

A cold boot using host graphics resolved the previous package-service installation failure. Android reported `sys.boot_completed=1`, and its package service responded before the standalone Debug APK was installed successfully. The application launched and rendered its controls and location permission prompt. Two emulator-provided GPS readings were saved in SQLite and restored after force-stop/relaunch. Tracking did not restart automatically. Clear History displayed its confirmation dialog. Cancel retained both records; Delete removed them from SQLite, and another relaunch showed zero samples with Center/Clear disabled.

Map tiles remain blank because no local Google Maps key is configured. Route density, map centering, stop stability, permission-denial, and background behavior still require complete acceptance checks. Diagnostic captures remain outside the submission output; they are not evidence of a working map. Full Xcode is unavailable, so iOS was not run.

Formatting verification passed with the compatible JDK available.

## Rubric coverage

| Area | Completed | Remaining |
|---|---|---|
| Application requirements — 25 points | C#/MAUI source, SQLite, foreground tracking, density layer, controls, platform permissions | Verify the features on a running device or simulator |
| Coding standards — 50 points | MVVM, focused services, dependency injection, asynchronous cancellation, comments, formatting, 16 behavioral tests | Review any fixes arising from device validation |
| Runtime, correctness, submission — 50 points | Restore, Android build, automated tests, source/manifest documentation, GitHub repository | Successful device run, screenshot, completed Word report, Blackboard submission |

## Remaining submission steps

1. Configure a restricted Android Maps key in the ignored `LocationTracker/GoogleMaps.local.props`, rebuild, and complete the runtime checklist. Xcode is optional for this Android demonstration.
2. Capture `screenshots/location-tracker-running.png` with map tiles and several recorded samples.
3. Record the results in `validation.json`, regenerate `MSCS533_Final_Project_LocationTracker_AM.docx`, and review the embedded screenshot.
4. Publish the final report and screenshot to GitHub, then submit the Word document and repository URL in Blackboard.

The current Word document contains the required ten sections, a C# excerpt, the Android manifest, and the repository link. It remains a draft until the runtime results and screenshot are included.
