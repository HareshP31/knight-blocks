// lab-main.js

import {
  init, 
  createFaceLandmarker,
  startPredictionLoop,
  startCalibration
} from "./AIEngine.js"; // <-- Path corrected for deployment

// --- UI Elements are ONLY used for local testing in lab.html ---
// They are kept here but their associated event listeners are removed/modified.
const video = document.getElementById('webcam-feed');
const canvas = document.getElementById('debug-canvas');
const startButton = document.getElementById('start-button');
const statusText = document.getElementById('status-text');
const aiStatusIndicator = document.getElementById('ai-status-indicator');
const gazeDot = document.getElementById('gaze-dot');
const winkLeftIndicator = document.getElementById('wink-left-indicator');
const winkRightIndicator = document.getElementById('wink-right-indicator');
const rotateIndicator = document.getElementById('rotate-indicator');
const rotateValue = document.getElementById('rotate-value');
const statusLog = document.getElementById('status-log'); // Assuming you have this for logging

// --- AI READY LISTENER (ONLY for local sandbox testing) ---
window.addEventListener('ai_ready', () => {
    statusText.textContent = "AI Model Ready. Click Start.";
    startButton.disabled = false;
    console.log("AI Model is ready.");
});

// --- RENAMED TO startAIAssistant for global access by the .jslib ---
async function startAISystem() { 
    // ... (Your existing logic for getting webcam, initializing, and starting calibration) ...
    
    // Disable button to prevent double-clicks
    startButton.disabled = true;
    statusText.textContent = "Requesting webcam access...";

    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            video: { width: 1280, height: 720 }, audio: false
        });

        statusText.textContent = "Initializing AI Engine...";
        video.srcObject = stream;

        video.addEventListener('loadeddata', () => {
            init(video, canvas);

            // Set UI status for CALIBRATION
            statusText.textContent = "Calibrating... Look at the screen.";
            aiStatusIndicator.classList.remove('bg-gray-500');
            aiStatusIndicator.classList.add('bg-yellow-500');
            startButton.textContent = "Calibrating...";

            startCalibration();
            startPredictionLoop();
        });

    } catch (error) {
        console.error("Error starting AI system:", error);
        statusText.textContent = "Error: Webcam access denied.";
        startButton.disabled = false;
    }
}


// --------------------------------------------------------------------------------
// --- NEW BRIDGE IMPLEMENTATION: Listen for AIEngine events and call C# methods ---
// --------------------------------------------------------------------------------

window.addEventListener('ai_gaze', (e) => {
    const gazeData = e.detail;
    
    // 1. CALL C# BRIDGE: Pass normalized coordinates to Unity
    // We must check if the function exists, as it won't in the local lab.html test.
    if (typeof GazeEventReceiver === 'function') {
        GazeEventReceiver(gazeData.x, gazeData.y);
    }

    // 2. SANDBOX UI UPDATE: Keep this for lab.html testing
    gazeDot.style.left = `${gazeData.x * 100}%`;
    gazeDot.style.top = `${gazeData.y * 100}%`;
});


window.addEventListener('ai_action', (e) => {
    const action = e.detail;
    
    // 1. CALL C# BRIDGE: Pass action type to Unity
    if (typeof ActionReceiver === 'function') {
        ActionReceiver(action);
    }
    
    // 2. SANDBOX UI UPDATE: Keep this for lab.html testing
    let indicatorElement = action === 'select' ? winkRightIndicator : winkLeftIndicator;
    if (indicatorElement) {
        indicatorElement.classList.add('active');
        setTimeout(() => indicatorElement.classList.remove('active'), 200);
    }
});


window.addEventListener('ai_rotate', (e) => {
    const direction = e.detail;
    
    // 1. CALL C# BRIDGE: Pass rotation direction to Unity
    if (typeof RotationReceiver === 'function') {
        RotationReceiver(direction);
    }

    // 2. SANDBOX UI UPDATE: Keep this for lab.html testing
    rotateValue.textContent = direction.charAt(0).toUpperCase() + direction.slice(1);
    rotateIndicator.classList.add('active');
    setTimeout(() => {
        rotateIndicator.classList.remove('active');
        rotateValue.textContent = "--";
    }, 500); 
});

window.addEventListener('ai_calibration_complete', () => {
    console.log("Lab received: Calibration Complete!");

    // 1. CALL C# BRIDGE: Notify Unity that the system is ready
    if (typeof CalibrationCompleteReceiver === 'function') {
        CalibrationCompleteReceiver();
    }
    
    // 2. SANDBOX UI UPDATE: Final status update for lab.html testing
    statusText.textContent = "AI Running!";
    aiStatusIndicator.classList.remove('bg-yellow-500');
    aiStatusIndicator.classList.add('bg-green-500');
    startButton.textContent = "AI Active";
    gazeDot.style.display = 'block';
});


// --------------------------------------------------------------------------------
// --- GLOBAL EXPORTS ---
// --------------------------------------------------------------------------------

// 1. Make the start function global for the AIBridge.jslib to call
window.startAIAssistant = startAISystem;

// 2. Remove the HTML click listener since Unity will now call the system start
// If you want to keep the local sandbox click-to-start working, keep the line below:
startButton.addEventListener('click', startAISystem); // (Kept for lab.html)

// 3. Kick off the (async) model loading process immediately (unchanged)
statusText.textContent = "Loading AI Model (this may take a moment)...";
createFaceLandmarker();