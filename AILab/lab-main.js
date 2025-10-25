import {
  init, 
  createFaceLandmarker,
  startPredictionLoop
} from "../AI/AIEngine.js";

// --- Task 1.3: Get references to all HTML elements ---
const video = document.getElementById('webcam-feed');
const canvas = document.getElementById('debug-canvas');
const startButton = document.getElementById('start-button');
const statusText = document.getElementById('status-text');
const aiStatusIndicator = document.getElementById('ai-status-indicator');

// --- "Fake" UI elements for testing ---
const gazeDot = document.getElementById('gaze-dot');
const winkLeftIndicator = document.getElementById('wink-left-indicator');
const winkRightIndicator = document.getElementById('wink-right-indicator');
const rotateIndicator = document.getElementById('rotate-indicator');
const rotateValue = document.getElementById('rotate-value');
// --- End Task 1.3 ---


// --- Main Sandbox Logic ---

// 1. Add a "listener" for the 'ai_ready' event from AIEngine.js
// This ensures we don't let the user click "Start" until the
// AI model is fully loaded.
window.addEventListener('ai_ready', () => {
    statusText.textContent = "AI Model Ready. Click Start.";
    startButton.disabled = false;
    console.log("AI Model is ready.");
});

// 2. Add the click listener to the start button
startButton.addEventListener('click', startAISystem);

// 3. Kick off the (async) model loading process immediately
// This function is defined in AIEngine.js
statusText.textContent = "Loading AI Model (this may take a moment)...";
createFaceLandmarker();


/**
 * --- Task 1.3: The "Bootloader" Function ---
 * This function is called when the user clicks the "Start" button.
 */
async function startAISystem() {
    // Disable button to prevent double-clicks
    startButton.disabled = true;
    statusText.textContent = "Requesting webcam access...";
    console.log("Requesting webcam access...");

    try {
        // --- 1. Get webcam permission ---
        const stream = await navigator.mediaDevices.getUserMedia({
            video: {
                width: 1280,
                height: 720
            },
            audio: false
        });

        // --- 2. Got permission. Link stream to video element ---
        console.log("Webcam access granted.");
        statusText.textContent = "Initializing AI Engine...";
        video.srcObject = stream;

        // --- 3. Wait for the video to start playing ---
        // This is a CRITICAL step. MediaPipe needs a playing
        // video element to get data from.
        video.addEventListener('loadeddata', () => {
            console.log("Video data loaded.");

            // --- 4. Initialize the AI Engine ---
            // This function is in AIEngine.js.
            // It tells the engine which video and canvas to use.
            init(video, canvas);

            // --- 5. Start the AI Prediction Loop ---
            // This function is in AIEngine.js.
            // It starts the requestAnimationFrame() loop.
            startPredictionLoop();

            // --- 6. Update UI ---
            statusText.textContent = "AI Running!";
            aiStatusIndicator.classList.remove('bg-gray-500');
            aiStatusIndicator.classList.add('bg-green-500');
            startButton.textContent = "AI Active";
            gazeDot.style.display = 'block';
        });

    } catch (error) {
        // Handle errors (e.g., user denied webcam)
        console.error("Error starting AI system:", error);
        statusText.textContent = "Error: Webcam access denied.";
        startButton.disabled = false;
    }
}


// --- Test & Debug Logic ---
// This is the second half of this file's job.
// We listen for the custom events from AIEngine.js
// and update our "fake" UI to prove the AI is working.

window.addEventListener('ai_action', (e) => {
    const action = e.detail;
    console.log("Lab received action:", action);

    let indicatorElement;

    if (action === 'select') {
        indicatorElement = winkRightIndicator;
    } else if (action === 'deselect') {
        indicatorElement = winkLeftIndicator;
    }

    if (indicatorElement) {
        // Flash the indicator
        indicatorElement.classList.add('active');
        setTimeout(() => {
            indicatorElement.classList.remove('active');
        }, 200); // Flash for 200ms
    }
});

/**
 * Listen for the 'ai_gaze' event (cursor)
 */
window.addEventListener('ai_gaze', (e) => {
    const gazeData = e.detail; // { x: 0.x, y: 0.y }
    
    // Convert normalized coordinates (0.0 - 1.0) to percentages
    // The gaze dot is positioned relative to the video container.
    gazeDot.style.left = `${gazeData.x * 100}%`;
    gazeDot.style.top = `${gazeData.y * 100}%`;
});

/**
 * Listen for the 'ai_rotate' event (head turn)
 */
window.addEventListener('ai_rotate', (e) => {
    const direction = e.detail;
    console.log("Lab received rotation:", direction);

    // Update the text
    rotateValue.textContent = direction.charAt(0).toUpperCase() + direction.slice(1);

    // Flash the indicator
    rotateIndicator.classList.add('active');
    setTimeout(() => {
        rotateIndicator.classList.remove('active');
        rotateValue.textContent = "--";
    }, 500); // Hold for 500ms
});