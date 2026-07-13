# Varjo Eye-Tracking Setup

This document is only needed if you are running the experiment on a **Varjo headset**
(Aero, VR-3, XR-3) with eye-tracking enabled. Desktop mode and Meta Quest 2/3 users do
not need anything on this page — the project builds and runs without it.

## 1. Add the Varjo/iMotions packages

The integration packages are not distributed with this repository (licensing/size).
Obtain the `com.threespacelab.imotions.varjointegration@a64044b3a4dc` folder and place
it in `LocalPackages/` (see `LocalPackages/README.txt`).

## 2. Register the packages in the project manifest

Add the following three entries to `Packages/manifest.json`, inside `"dependencies"`:

```json
"com.threespacelab.imotions.core": "file:../LocalPackages/com.threespacelab.imotions.varjointegration@a64044b3a4dc/Packages~/com.threespacelab.imotions.core-1.0.3.tgz",
"com.threespacelab.imotions.varjointegration": "file:../LocalPackages/com.threespacelab.imotions.varjointegration@a64044b3a4dc",
"com.varjo.xr": "file:../LocalPackages/com.threespacelab.imotions.varjointegration@a64044b3a4dc/Packages~/com.varjo.xr-2.3.0.tgz",
```

Save the file and let Unity re-resolve packages (reopen the project, or use
**Window > Package Manager** to trigger a refresh).

## 3. Enable the Varjo XR loader

In **Project Settings > XR Plug-in Management**, check **Varjo** under the loader list.

## 4. Gaze look source

`GazeLookSource.cs` (`Assets/Scripts/LLM/Focus/Look Sources/Varjo/`) is the only script
that depends on the Varjo/iMotions packages. It lives in its own assembly
(`AttentionIntegration.Varjo.asmdef`), which is automatically excluded from compilation
when the `com.varjo.xr` package is not installed (via `defineConstraints`/`versionDefines`).
Without the packages installed, the rest of the project (Desktop, Meta Quest) compiles
and runs with **zero errors** — this assembly is simply skipped, not compiled-and-failed.

Attach `GazeLookSource` to a `LookTracker` source list only when targeting Varjo
hardware; for Desktop/Meta builds use `CrosshairLookSource`, `HeadGazeLookSource`, or
`ControllerLookSource` instead.
