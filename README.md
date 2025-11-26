# KnightBlocks

> **Build LEGOs hands-free using facial gestures.** > *A Hackathon project powered by Unity, WebGL, and MediaPipe.*

## 📖 Overview

**KnightBlocks** is an accessible virtual building environment designed to bring the joy of LEGOs to individuals with limited motor function, such as those with arthritis. We created a hands-free interface that allows users to select, move, and place bricks using only their face.

The application runs entirely in the browser, using computer vision to track head movements for cursor control and facial gestures (like winking) for interactions.

## ✨ Features

* **Hands-Free Control:** Control the cursor simply by moving your head.
* **Gesture Interaction:** "Wink" to place a brick or interact with UI buttons.
* **Browser-Based:** Fully accessible via a web link with no specialized hardware required—just a webcam.
* **Virtual Sandbox:** A dedicated environment to build and create without physical limitations.

## 🛠️ Tech Stack

* **Engine:** Unity (Universal Render Pipeline)
* **Platform:** WebGL (Browser-based)
* **Computer Vision:** Google MediaPipe (Face Landmark Detection)
* **Language:** C#
* **Key Algorithms:** *Face Landmark Tracking* for cursor mapping.
    * *Eye Aspect Ratio (EAR)* logic to detect winks/blinks.

## ⚙️ How It Works

1.  **Face Tracking:** We utilize **MediaPipe's Face Landmark Detection** model to track 478 3D landmarks on the user's face in real-time.
2.  **Cursor Mapping:** The nose and forehead landmarks are mapped to the screen's coordinate system, acting as a virtual mouse.
3.  **Gesture Recognition:** The system monitors the **Eye Aspect Ratio (EAR)**—the vertical distance between the upper and lower eyelids. When this ratio drops below a specific threshold (indicating a wink), the system registers a "click" event to place a brick or press a button.

## 🚀 Getting Started

### Prerequisites
* Unity Hub and Unity Editor (Version 2022.3 or later recommended).
* A webcam.

### Installation
1.  **Clone the repo**
 
2.  **Open in Unity:**
    * Add the project folder to Unity Hub.
    * Open the project.
3.  **Build & Run:**
    * *Note: You must allow webcam permissions in your browser when the page loads.*

## 🔮 What's Next

* **Save & Share:** Implementing a backend to allow users to save their builds and share them via a gallery.
* **AI Instructions:** Integrating an LLM to generate step-by-step building instructions for user ideas.
* **Optimization:** Smoothing out the cursor movement for even finer control.
