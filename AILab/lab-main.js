import {
  createFaceLandmarker,
  init,
  startPredictionLoop,
  startCalibration // The new WebGazer calibration function
} from '../AI/AIEngine.js';

// --- 2. Get All DOM Elements ---
// Main controls
const startButton = document.getElementById('start-button');
const video = document.getElementById('webcam-feed');
const canvas = document.getElementById('debug-canvas');
const gazeDot = document.getElementById('gaze-dot');

// Indicators
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
    const maxAttempts = 30000; // Wait for max 5 seconds (50 * 100ms)
    
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
  aiStatusIndicator.style.backgroundColor = '#f59e0b'; // yellow
  statusText.textContent = 'Gestures Ready';
  logToPanel("MediaPipe model loaded.");
});

window.addEventListener('gaze_ready', () => {
  aiStatusIndicator.style.backgroundColor = '#10b981'; // green
  statusText.textContent = 'All Systems Active';
  logToPanel("WebGazer calibration complete.");
});

// --- Gaze Listener ---
window.addEventListener('ai_gaze', (e) => {
  // e.detail.x and e.detail.y are normalized (0.0 to 1.0)
  const { x, y } = e.detail;

  // We need to move the dot relative to the video feed's *display size*
  // not its real pixel size.
  const videoRect = video.getBoundingClientRect();
  
  // Calculate the pixel position
  const pixelX = videoRect.width * x;
  const pixelY = videoRect.height * y;
  
  // Move the dot
  gazeDot.style.left = `${pixelX}px`;
  gazeDot.style.top = `${pixelY}px`;
});

// --- Action Listener (Winks) ---
window.addEventListener('ai_action', (e) => {
  const action = e.detail;
  logToPanel(`Action: ${action}`);

  if (action === 'select') { // Right Wink
    winkRightIndicator.classList.add('active');
    // Remove 'active' after a short delay for a "flash" effect
    setTimeout(() => winkRightIndicator.classList.remove('active'), 300);
  } 
  else if (action === 'deselect') { // Left Wink
    winkLeftIndicator.classList.add('active');
    setTimeout(() => winkLeftIndicator.classList.remove('active'), 300);
  }
});

// --- Rotation Listener ---
window.addEventListener('ai_rotate', (e) => {
  const direction = e.detail;
  logToPanel(`Rotate: ${direction}`);
  
  rotateValue.textContent = direction.toUpperCase();
  rotateIndicator.classList.add('active');
  setTimeout(() => rotateIndicator.classList.remove('active'), 300);
});


/**
 * Helper function to log messages to the status panel
 */
function logToPanel(message, level = 'info') {
  const p = document.createElement('p');
  p.textContent = `> ${message}`;
  if (level === 'error') {
    p.style.color = '#f87171'; // text-red-400
  }
  
  statusLog.appendChild(p);
  // Scroll to the bottom
  statusLog.scrollTop = statusLog.scrollHeight;
}