# Location Tracker — .NET MAUI

Ashish Bhanudas Mahajan · MSCS-533-A01 · University of the Cumberlands

A C# app that records foreground device locations in SQLite and visualizes their relative density using native .NET MAUI Maps circles.

[GitHub repository](https://github.com/AshishM26/MSCS-533-Project_2_AM) · [Word report](submission/MSCS533_Final_Project_LocationTracker_AM.docx) · [Validation results](submission/VALIDATION.md)

## Features and design

- Start/stop tracking, sample count, status, latest reading, map centering, and confirmed history deletion.
- Saved history and heat-map overlays reload when the app reopens. Tracking stops in the background.
- Roughly 75-meter geographic cells become blue, amber, or red according to relative sample density. Circle size and opacity increase with density; at most 400 cells are drawn.
- MVVM and dependency injection separate the MAUI UI (`LocationTracker`), portable tracking/data/aggregation services (`LocationTracker.Core`), and behavioral tests (`LocationTracker.Tests`).
- Stable .NET 10, MAUI Controls/Maps 10.0.100, and sqlite-net-pcl 1.11.285. Android and iOS targets are included; Mac Catalyst is available on macOS.

## Run locally

1. Install the .NET SDK specified in `global.json`, the MAUI workload, and VS Code with Microsoft's MAUI and C# extensions.
2. Install an Android SDK/JDK and emulator, or full Xcode with an iOS simulator on macOS.
3. For Android maps, copy `LocationTracker/GoogleMaps.local.props.example` to `LocationTracker/GoogleMaps.local.props` and set your restricted Maps SDK for Android key in that ignored file. iOS maps do not require this key.
4. Open `LocationTracker.slnx`, restore packages, select the device in VS Code, and run with **F5**. Allow location while using the app, then tap **Start Tracking**.

See the [runtime guide](submission/RUNTIME_GUIDE.md) for build commands, simulated locations, and acceptance checks. Run automated tests with `dotnet test LocationTracker.Tests/LocationTracker.Tests.csproj`.

## Permissions and privacy

Android requests coarse/fine location; iOS declares `NSLocationWhenInUseUsageDescription`. No background-location permission is requested. History stays in the app's local, unencrypted SQLite file and can be deleted with **Clear History**. Native map providers use network services. Local keys, databases, and build outputs are excluded from Git.

## Submission status

Android compilation and automated tests have passed. Final device acceptance, a screenshot with working map tiles, and the screenshot embedded in the Word report remain required. See the [submission checklist](submission/SUBMISSION_CHECKLIST.md) for current evidence. Regenerate the report using `scripts/create_submission.py` after completing those checks.
