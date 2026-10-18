# Validation record — October 18, 2026

All results below were observed on this date. Location readings came from simulator or emulator GPS through the app's normal location service. No rows were inserted into SQLite directly.

## Environment

| Item | Version |
|---|---|
| OS / CPU | macOS 26.7.1, Apple Silicon arm64 |
| VS Code / Git | 1.142.0 / 2.50.1 |
| VS Code extensions | .NET MAUI 1.17.289, C# Dev Kit 3.40.210, C# 2.160.4 |
| .NET SDK / runtime | 10.0.302 / 10.0.10 |
| MAUI workload | Workload set 10.0.303.1 (MAUI 10.0.20, .NET for iOS 26.5) |
| App packages | Microsoft.Maui.Controls and Controls.Maps 10.0.100, sqlite-net-pcl 1.11.285 |
| iOS | Xcode 27.1, iOS 27.1 simulator (iPhone Duo), Apple Maps |
| Android | SDK API 36, Pixel 7 Google APIs ARM64 emulator, Microsoft OpenJDK 21 |

## Build and test results

| Command | Result |
|---|---|
| `dotnet restore LocationTracker.slnx` | Passed |
| `dotnet test LocationTracker.Tests/LocationTracker.Tests.csproj` | 16 passed, 0 failed |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-android` | 0 warnings, 0 errors |
| `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-ios -r iossimulator-arm64 -p:ValidateXcodeVersion=false` | 0 warnings, 0 errors |
| `dotnet format LocationTracker.slnx --no-restore --verify-no-changes` | Passed |
| `python3 scripts/check_secrets.py` | No secrets or personal paths found |

The installed .NET for iOS workload targets Xcode 26.6. Xcode 27.1 was installed, so the iOS build skipped the version check with `ValidateXcodeVersion=false`. Microsoft documents this combination as unsupported. Here the app built and ran without errors.

## iOS simulator acceptance (final demonstration platform)

| Check | Observed result |
|---|---|
| Launch and UI | App opened with the title, status, sample count, Apple Maps tiles, density legend, and all four controls. |
| Permission | iOS prompt showed the app's purpose text. Allowing it while in use started tracking. |
| Tracking and SQLite | A reading was saved about every 8 seconds. The final total was 124 rows in `locations.db3` across 12 positions. |
| Heat map | Circles appeared along the route. The longer stops produced red (45 samples), amber (28 and 17), and blue (1–14) cells. |
| Center | After the map was panned away, Center on Current Location moved it back to the latest recorded position. |
| Stop | No new rows for 20 seconds after Stop (two or more sample intervals). |
| Force-stop and relaunch | Status read "Ready — saved history restored", with the same 76 samples and circles. Tracking did not restart (count unchanged after 18 seconds). |
| Background | Opening Settings stopped tracking. The count stayed at 124 for 24 seconds in the background. |
| Return to app | Tracking did not resume. The count stayed at 124 for another 24 seconds. |

## Android emulator acceptance

| Check | Observed result |
|---|---|
| Install and launch | The signed Debug APK installed after a cold boot. The app launched and showed the permission prompt. |
| Tracking and SQLite | Emulator GPS readings were saved and restored after force-stop and relaunch. Tracking did not restart automatically. |
| Clear History | A confirmation dialog appeared. Cancel kept the records. Delete removed all records, the pin, and the circles. |
| Relaunch after delete | Zero samples. Center and Clear were disabled. |
| Map tiles | Not shown. Android needs a Google Maps key, and none was configured. The iOS run covers map output. |

Permission denial was not tested live. An automated test covers it and confirms the status message, the stop, and that nothing is saved.

## Application output

`screenshots/location-tracker-running.png` is the unedited iOS simulator capture embedded in the Word report.
