# VR Attention Integration

This project is a Unity-based VR environment designed to analyze user attention in a retail setting. It integrates high-precision eye-tracking (Varjo Aero) and standard VR headsets (Meta Quest 2/3) to track gaze, generate heatmaps, and log interaction data for behavioral research.

## 🚀 Features

* **Multi-Headset Support:** Seamless switching between Varjo Aero (Eye-tracking enabled) and Meta Quest 2/3.
* **Attention Tracking:** Real-time calculation of gaze duration on specific shelves and products using raycasting.
* **Heatmap Generation:** Visual representation of user attention intensity on 3D objects.
* **Data Logging:** Automatic CSV export of experiment data (Total Look Time, Glances, timestamps).
* **Experiment Management:** UI-based flow for calibration and session control.

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

* **Unity Version:** 2021.3.7f1
* **Platforms:** Windows 10/11 (Required for Varjo Base)
* **Software:**
    * [Varjo Base](https://varjo.com/downloads/) (If using Varjo headsets)
    * [Meta Quest Link](https://www.meta.com/quest/setup/) (If using Oculus/Meta headsets)
    * SteamVR

## 🛠 Installation & Setup

1.  **Clone the Repository**
    ```bash
    git clone [https://github.com/YOUR_USERNAME/unity-attention-integration.git](https://github.com/YOUR_USERNAME/unity-attention-integration.git)
    ```

2.  **Open in Unity**
    * Add the project to Unity Hub.
    * Open the project. This is all that's needed for **Desktop** or **Meta Quest 2/3** mode.

3.  **Varjo eye-tracking only:** if you are targeting a Varjo headset, the eye-tracking
    integration relies on iMotions/Varjo packages that are **not included** in this
    repository (licensing/size constraints) and must be added manually. See
    [`docs/VARJO_SETUP.md`](docs/VARJO_SETUP.md) for the steps. Desktop and Meta Quest
    users can skip this entirely.

## 🥽 Hardware Configuration

This project is configured to work with two main XR setups. You must enable the correct loader in **Project Settings > XR Plug-in Management**.

### 1. Varjo Aero (Eye-Tracking Mode)
* **Requirement:** Varjo Base software must be running.
* **Unity Setting:** Check **Varjo** under XR Plug-in Management.
* **Calibration:** Press `C` on the keyboard or use the UI "Calibrate" button to start the 5-point gaze calibration.

### 2. Meta Quest 2 / 3 (Simulation Mode)
* **Requirement:** Quest Link (Air Link or Cable) must be active.
* **Unity Setting:** Check **Oculus** under XR Plug-in Management.
* **Note:** Eye-tracking features (Heatmaps based on gaze) will utilize head pose approximation or will be disabled depending on the script configuration.

## 📂 Project Structure & Key Scripts

* **`ExperimentManager.cs`**: The central brain of the simulation. Handles UI transitions (Start/End), timer management, and data export triggers.
* **`GazeRays.cs`**: Manages the raycasting logic. It draws visual rays from the user's eyes (or camera center) to detect focused objects.
* **`ShelfAttentionManager.cs`**: Attached to interactable objects (shelves). Calculates `TotalLookTime` and `GlanceCount`.
* **`HeatmapGenerator.cs`**: Generates texture-based heatmaps on object surfaces based on gaze accumulation.
* **`UIGazeController.cs`**: Allows the user to interact with World Space UI elements using their gaze or controllers.

## 🎮 How to Run an Experiment

1.  **Launch:** Play the `MainScene` in the Unity Editor or build the executable.
2.  **Device Setup:** Ensure your VR headset is active and recognized (Varjo Base or Quest Link).
3.  **Calibration:** Click the **"Calibrate"** button (essential for Varjo accuracy).
4.  **Start:** Click **"Start Experiment"**. The recording of gaze data begins immediately.
5.  **Interaction:** Navigate the environment. The system logs attention data in the background.
6.  **Finish:** Click **"Finish Experiment"**.
    * Data is saved to: `Assets/ExperimentData/` (CSV format).
    * Heatmaps (if enabled) are generated and saved.

## 📄 License

---
*Developed for the CoDE by Ali Yağız Canıgüroğlu
