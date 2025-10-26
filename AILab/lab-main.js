
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

// Status Panel
const aiStatusIndicator = document.getElementById('ai-status-indicator');
const statusText = document.getElementById('status-text');
const statusLog = document.getElementById('status-log');

function waitForWebGazer() {
  logToPanel("Loading gaze tracking library...");
  return new Promise((resolve, reject) => {
    let attempts = 0;
    const maxAttempts = 300; // Wait for max 5 seconds (50 * 100ms)
    
    const check = () => {
      // Check if the main object AND the function we need exist
      if (window.webgazer && typeof window.webgazer.setVideoElement === 'function') {
        logToPanel("WebGazer.js library is ready.");
        resolve();
      } else if (attempts > maxAttempts) {
        reject(new Error("WebGazer.js failed to load in time."));
      } else {
        // Wait and check again
        attempts++;
        setTimeout(check, 100); 
      }
    };
    check(); // Start checking
  });
}

// --- 3. Main App "Start" Logic ---
startButton.addEventListener('click', async () => {
  logToPanel("Initializing Sandbox...");
  startButton.disabled = true;

  try {
    // 1. Start the webcam feed
    await startWebcam();
    logToPanel("Webcam activated.");

    startButton.textContent = "Loading Gaze Library...";
    await waitForWebGazer();

    // 2. Load MediaPipe model (for winks/rotation)
    startButton.textContent = "Loading Gesture Model...";
    await createFaceLandmarker(); // This fires 'ai_ready'
    
    // 3. Init & Start MediaPipe
    // This connects the engine to the video and canvas elements
    init(video, canvas);
    startPredictionLoop(); // This starts winks/rotation
    logToPanel("Gesture detection (winks, rotate) is active.");

    // 4. Start Hands-Free Gaze Calibration
    logToPanel("Starting hands-free gaze calibration...");
    startButton.textContent = "Calibrating... Look at the dots!";
    
    // This is the async function from AIEngine.js
    // It will take ~30 seconds
    await startCalibration(); 
    
    // 5. All Done!
    logToPanel("Calibration complete! Gaze tracking is now active.");
    startButton.textContent = "All Systems Active";
    startButton.style.display = 'none'; // Hide the start button
    gazeDot.style.display = 'block'; // Show the gaze dot

  } catch (err) {
    console.error(err);
    logToPanel(`ERROR: ${err.message}`, 'error');
    startButton.textContent = "Error - Refresh Page";
  }
});

/**
 * Helper function to start the webcam
 */
async function startWebcam() {
  if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
    throw new Error('Webcam access is not supported by this browser.');
  }

  const constraints = { video: { width: 1280, height: 720 } };
  const stream = await navigator.mediaDevices.getUserMedia(constraints);
  
  video.srcObject = stream;
  
  return new Promise((resolve) => {
    video.onloadedmetadata = () => {
      // Set canvas size to match video feed
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      resolve();
    };
  });
}

// --- 4. Event Listeners ---
// These listeners wait for your AIEngine to fire custom events

// --- Status Listeners ---
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


window.addEventListener('ai_gaze', (e) => {
    const gazeData = e.detail; // { x: 0.x, y: 0.y }
    
    // Convert normalized coordinates (0.0 - 1.0) to percentages
    // The gaze dot is positioned relative to the video container.
    gazeDot.style.left = `${gazeData.x * 100}%`;
    gazeDot.style.top = `${gazeData.y * 100}%`;
});


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
