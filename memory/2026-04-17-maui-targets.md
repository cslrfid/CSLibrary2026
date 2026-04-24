# Session: 2026-04-17 03:05:56 UTC

- **Session Key**: agent:githubproject-cslibrary2026:main
- **Session ID**: 221c916d-c15b-44a6-bbc4-93d39b82ebf9
- **Source**: webchat

## Conversation Summary

assistant: The `develop` branch already has MAUI targets after the PR #2 merge. Let me give you the real answer:

## Short answer: **No, .NET Standard 2.0 is NOT sufficient alone.**

## Why explicit MAUI targets matter

### 1. Platform-Specific APIs
`netstandard2.0` is purely portable — it **cannot** reference platform APIs directly:
```csharp
// netstandard2.0 — ❌ This won't compile
using Android.Runtime;
[assembly: Android.Runtime.UsesPermission(...)]

// net10.0-android — ✅ This compiles
using Android.Runtime;
[assembly: Android.Runtime.UsesPermission(...)]
```

The `AndroidPermissions.cs` file **only works** because it's guarded with `#if ANDROID`. But even that guard only activates when you compile for a specific Android TFM.

### 2. IntelliSense & IDE Support
With `netstandard2.0` only:
- Visual Studio can't know your app targets Android → no Android-specific IntelliSense
- No Android asset selection in the NuGet package

With explicit `net10.0-android`, `net10.0-ios`, etc.:
- IDE knows exactly which platform → platform-aware IntelliSense
- NuGet delivers per-platform assets correctly

### 3. P/Invoke and Native Interop
`netstandard2.0` has restrictions on certain native interop patterns that `net10.0-*` targets allow freely.

### 4. Plugin.BLE NuGet
Plugin.BLE 3.0.0 targets specific MAUI platforms. Having matching TFMs in your library ensures the correct NuGet assets and binding are used.

## Practical Recommendation

| Scenario | Enough? |
|----------|---------|
| Library used by Xamarin.Forms / older .NET Core | `netstandard2.0` alone ✅ |
| Modern MAUI app (VS2022+ with MAUI workload) | Explicit MAUI targets ✅ |
| App needs Android permissions in the library itself | `net10.0-android` required ✅ |
| App needs iOS/Android-specific code paths | Platform TFMs needed |

**Conclusion:** For a MAUI app that works today with `netstandard2.0` — you're fine. But as the library grows (like adding Android permissions, platform-specific guards), `netstandard2.0` alone becomes a ceiling, not a floor.
user: Sender (untrusted metadata):
```json
{
  "label": "openclaw-control-ui",
  "id": "openclaw-control-ui"
}
```

[Fri 2026-04-17 10:39 GMT+8] Is using .NET Standard 2.0 sufficient for placing the project on the Google Play Store? Apple Store? Microsoft Store?
assistant: ## No — `netstandard2.0` is a library specification, not an app specification.

The stores require **apps**, not libraries. Here's the breakdown:

---

## How it actually works

```
App Store submission requires:
├── Android → needs .apk built with net10.0-android (or net8.0-android)
├── iOS     → needs .ipa built with net10.0-ios
└── Windows → needs .msix/.exe built with net10.0-windows

Your library: CSLibrary2026 (netstandard2.0)
└── This is referenced BY the app, not submitted directly
```

`netstandard2.0` is a **contract** — it guarantees your library works when consumed by any app that supports netstandard2.0. But the **app itself** must target a platform-specific TFM to produce a store-submittable artifact.

---

## What each store actually requires

| Store | App target | Library compatibility |
|-------|-----------|----------------------|
| **Google Play** | `net10.0-android` (MAUI app) | ✅ `netstandard2.0` compatible |
| **Apple App Store** | `net10.0-ios` (MAUI app) | ✅ `netstandard2.0` compatible |
| **Microsoft Store** | `net10.0-windows` (WinUI3/MAUI) | ✅ `netstandard2.0` compatible |

The library being `netstandard2.0` is **not a problem** — it's actually the right call. The app that *consumes* it targets the platform TFM.

---

## What matters for store submission

| Store | What matters |
|-------|-------------|
| **Google Play** | App targets `net10.0-android` ✅, min SDK 24+, signed APK/AAB |
| **Apple App Store** | App targets `net10.0-ios` ✅, signed with Apple cert (requires Mac) |
| **Microsoft Store** | App targets `net10.0-windows` ✅, packaged as MSIX |

---

## TL;DR

- **`netstandard2.0`** = correct for the *library* ✅
- **App stores** = require the *app* to target platform-specific TFMs ✅
- **CSLibrary2026** as a `netstandard2.0` library = fully compatible with all three stores ✅
- **PR #1** (explicit MAUI TFMs) = makes the library a better citizen in the MAUI ecosystem, especially for IDE tooling and NuGet asset selection, but `netstandard2.0` already works
user: Sender (untrusted metadata):
```json
{
  "label": "openclaw-control-ui",
  "id": "openclaw-control-ui"
}
```

[Fri 2026-04-17 10:43 GMT+8] Okay, since .netstandard 2.0 already does enough, let's just use .netstandard 2.0 and not .NET 10.

Please add an issue and ask OpenCode to change the code.
assistant: Issue #5 created. Now writing task file and delegating:

assistant: OpenCode found a **pre-existing build error** — `OnSearchCompleted` declared twice in `ClassDeviceFinder.cs`. This was likely masked before by multi-target builds. I need to authorize OpenCode to fix this CS file:

assistant: Found the issue. `OnSearchCompleted` is defined in two different partial classes of `CSLibrary.DeviceFinder`:

| File | Event type | Currently guarded? |
|------|-----------|-------------------|
| `TCPIP/ClassDeviceFinder.cs` | `DeviceFinderArgs` | ❌ No |
| `Plugin.BLE/DeviceFinder.cs` | `SearchCompletedEventArgs` | ❌ No |

Both compile simultaneously because neither has a `#if` guard. The fix is to add `#if TCP` to the TCPIP version. Let me update the task and redelegate:

assistant: The issue is that **both** `TCPIP/ClassDeviceFinder.cs` and `Plugin.BLE/ClassDeviceFinder.cs` define `OnSearchCompleted` with the same `DeviceFinderArgs` type — and neither has proper `#if` guards. The `#if TCP` fix alone isn't enough because BLE's version is unguarded.

The cleanest solution: **exclude** `Plugin.BLE/ClassDeviceFinder.cs` for netstandard2.0 (it has no BLE guards). Let me update the task:

assistant: The changes look correct. Let me verify the build manually:

assistant: Both files look correct. Let me run the build:

user: Sender (untrusted metadata):
```json
{
  "label": "openclaw-control-ui",
  "id": "openclaw-control-ui"
}
```

[Fri 2026-04-17 10:43 GMT+8] Okay, since .netstandard 2.0 already does enough, let's just use .netstandard 2.0 and not .NET 10.

Please add an issue and ask OpenCode to change the code.
assistant: 

Let me check the current state and fix the exclusion properly:

assistant: 

Build is still failing with the duplicate error. Let me check what's actually happening:

assistant: 

The problem is clear: the csproj never actually excluded `Plugin.BLE/ClassDeviceFinder.cs`. The earlier attempt had a typo (`__MwwmCrossPluginBLE` instead of `BLE`). Let me fix the csproj to properly exclude it for netstandard2.0.

assistant: 

Clean state on develop. The cleanest fix: add `#if BLE` guards in Plugin.BLE's `DeviceFinder.cs` for its `OnSearchCompleted` event (uses `SearchCompletedEventArgs`, different signature), and set `BLE` define in csproj.

Let me write a proper task and delegate:

