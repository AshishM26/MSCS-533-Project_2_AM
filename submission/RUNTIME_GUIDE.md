# Runtime verification and screenshot capture

Record actual results in `VALIDATION.md`; leave untested items unchecked in `SUBMISSION_CHECKLIST.md`.

## iOS simulator (preferred for final output)

1. Install full Xcode and an iOS simulator runtime. Complete Xcode's first launch and license steps.
2. Select Xcode with `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer` and run `sudo xcodebuild -runFirstLaunch`.
3. Check `xcodebuild -version` and `xcrun simctl list devices available`.
4. Build: `dotnet build LocationTracker/LocationTracker.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64` (use `iossimulator-x64` on Intel).
5. Select a simulator in VS Code and run with F5. Alternatively boot a simulator and install/launch the built app:

```bash
xcrun simctl boot DEVICE_UDID
open -a Simulator
xcrun simctl install booted LocationTracker/bin/Debug/net10.0-ios/iossimulator-arm64/LocationTracker.app
xcrun simctl launch booted edu.cumberlands.ashish.locationtracker
```

6. Tap Start Tracking and allow location while using the app. In Simulator choose **Features > Location > Custom Location**, or use the command below.

```bash
xcrun simctl location booted set 36.7370,-84.1640
```

7. Leave the app in the foreground for at least 30 seconds. Move through a few nearby simulated locations, allowing at least 10 seconds for each. Dwell at one location for 40 seconds to give cells different counts.

```bash
xcrun simctl location booted set 36.7380,-84.1640
xcrun simctl location booted set 36.7390,-84.1630
xcrun simctl location booted set 36.7400,-84.1620
```

Run those commands separately with the dwell time between them so the app records each position through its location service.

## Required acceptance checks

| Check | Expected behavior |
|---|---|
| Initial empty launch | Zero samples, Ready status, Start enabled; Center/Clear/Stop disabled. |
| Start and grant permission | Tracking status and increasing sample count; no overlapping GPS requests. |
| Map output | Map tiles, latest pin, and density circles visible near configured coordinates. |
| Center | After panning away, Center returns to the latest recorded sample. |
| Stop | Count stops increasing for at least two sample intervals. |
| Relaunch | Same stored sample count and overlays return. Tracking does not restart automatically. |
| Clear > Cancel | Count, history, and overlays remain. |
| Clear > Delete | Count becomes zero; circles and latest pin disappear. |
| Relaunch after clear | Zero samples remain. |
| Background while tracking | Sampling stops; returning to the app requires Start. |
| Deny permission | Useful denial/settings message; no records saved; app remains usable. |
| GPS disabled / no simulator location | Useful status; no crash or saved sample. |

Capture the screenshot before deleting the demonstration history, or repeat the short route afterward.

```bash
xcrun simctl io booted screenshot submission/screenshots/location-tracker-running.png
```

If automatic capture is unavailable, use Simulator **File > Save Screen** and save/copy the resulting PNG to that exact path. Inspect it for a visible map, samples, circles, status, title, and controls. Update validation notes, then run `.venv/bin/python scripts/create_submission.py` to embed it in the Word report. Reopen the report and confirm the image is present.

## Android alternative

Copy `LocationTracker/GoogleMaps.local.props.example` to the ignored `LocationTracker/GoogleMaps.local.props` and replace the placeholder with a restricted Maps SDK for Android key for application ID `edu.cumberlands.ashish.locationtracker` and the signing certificate SHA-1. Enable Maps SDK for Android in that Google Cloud project. Use a Google APIs ARM64 emulator on Apple Silicon. After launching the app, grant permission and set emulator coordinates through Extended Controls > Location or `adb emu geo fix LONGITUDE LATITUDE` (longitude comes first).

```bash
adb emu geo fix -84.1640 36.7370
adb exec-out screencap -p > submission/screenshots/location-tracker-running.png
```

Run the same acceptance checks. A blank Android map with an invalid/placeholder key does not satisfy the final map-output requirement.

Before installing an APK, cold-boot the dedicated test emulator and confirm `adb shell getprop sys.boot_completed` returns `1` and `adb shell pm list packages` works. Install the actual signed Debug APK using `adb install -r --no-streaming APK_PATH`. On the validation machine, API 36 cold boot with `-no-snapshot -gpu host` restored package-service responsiveness.
