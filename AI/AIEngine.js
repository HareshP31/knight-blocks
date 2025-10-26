import {
  FaceLandmarker,
  FilesetResolver,
  DrawingUtils
} from "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.0/vision_bundle.js"

// --- Model Variables ---
let faceModel;      // Model 1: Fast, lightweight for Winks/Rotation
// let irisModel;   // --- WebGazer Integration --- REMOVED
let video;
let canvasCtx;
let drawingUtils;
let lastVideoTime = -1;

// --- Landmark Indices ---
const LEFT_EYE_INDICES = [362, 385, 387, 263, 373, 380];
const RIGHT_EYE_INDICES = [33, 160, 158, 133, 153, 144];
// --- WebGazer Integration --- REMOVED IRIS INDICES

// --- Tuning Knobs ---
const WINK_EAR_THRESHOLD = 0.25;
const WINK_CONSECUTIVE_FRAMES = 3;
const YAW_THRESHOLD = 15.0;
const YAW_RESET_THRESHOLD = 5.0;
// --- WebGazer Integration ---
const GAZE_SMOOTHING_FACTOR = 0.2; // Smoothing for WebGazer's raw output

// --- State Variables ---
let leftWinkCounter = 0, rightWinkCounter = 0;
let leftWinkFired = false, rightWinkFired = false;
let isTurnedLeft = false, isTurnedRight = false;
// --- WebGazer Integration ---
let smoothedGazeX = 0.5, smoothedGazeY = 0.5; // Start in center
let calibrationDot = null; // --- WebGazer Integration --- DOM element for calibration

// --- Helper Functions ---
function getEuclideanDistance(p1, p2) {
    if (!p1 || !p2) return 0;
    return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2) + Math.pow(p1.z - p2.z, 2));
}
// --- WebGazer Integration --- REMOVED getAveragePoint and mapValue
function calculateEAR(landmarks, eyeIndices) {
    if (eyeIndices.some(index => !landmarks[index])) return 0.5;
    const p1 = landmarks[eyeIndices[0]], p2 = landmarks[eyeIndices[1]], p3 = landmarks[eyeIndices[2]], p4 = landmarks[eyeIndices[3]], p5 = landmarks[eyeIndices[4]], p6 = landmarks[eyeIndices[5]];
    const verticalDist1 = getEuclideanDistance(p2, p6);
    const verticalDist2 = getEuclideanDistance(p3, p5);
    const horizontalDist = getEuclideanDistance(p1, p4);
    if (horizontalDist === 0) return 0.5;
    return (verticalDist1 + verticalDist2) / (2.0 * horizontalDist);
}
// --- WebGazer Integration --- Helper for calibration
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));

// --- MediaPipe Model Loader ---
async function createLandmarker(modelPath) {
  const filesetResolver = await FilesetResolver.forVisionTasks(
    "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@latest/wasm"
  );
  return await FaceLandmarker.createFromOptions(
    filesetResolver, {
      baseOptions: { modelAssetPath: modelPath },
      runningMode: "VIDEO",
      numFaces: 1,
      outputFacialTransformationMatrixes: true,
      outputFaceBlendshapes: true
    });
}

// --- WebGazer Integration --- SIMPLIFIED MediaPipe loader
async function createFaceLandmarker() {
  console.log("Loading AI Engine (MediaPipe Gestures)...");
  
  // --- 1. Load the Lightweight Model ---
  try {
    const modelPath = 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task';
    faceModel = await createLandmarker(modelPath);
    console.log("AI Model (Lightweight - Gestures) loaded successfully.");
    window.dispatchEvent(new CustomEvent("ai_ready")); // Fire 'ready' for basic features
    
  } catch (error) {
    console.error("CRITICAL ERROR: Could not load lightweight face model:", error);
  }
}

// --- WebGazer Integration --- DELETED loadIrisModel function

// --- Engine Initialization and Loop Control ---

function init(videoElement, canvasElement) {
  video = videoElement;
  canvasCtx = canvasElement.getContext("2d");
  drawingUtils = new DrawingUtils(canvasCtx);
  console.log("AI Engine initialized with video and canvas.");

  // --- WebGazer Integration ---
  calibrationDot = document.getElementById('calibration-dot');
  initWebGazer();
}

function startPredictionLoop() {
  console.log("Starting prediction loop (MediaPipe)...");
  predictWebcam();
}

// --- WebGazer Integration --- All new functions below ---

/**
 * Sets up the WebGazer listener but does NOT start it.
 */
function initWebGazer() {
  if (!webgazer) {
    console.error("WebGazer.js not loaded. Please add the script to your HTML.");
    return;
  }

  // Set up the listener that will fire events
  webgazer.setGazeListener((data, elapsedTime) => {
    if (data == null) {
      return; // Not getting a prediction
    }

    const normalizedX = data.x / window.innerWidth;
    const normalizedY = data.y / window.innerHeight;

    // Invert X to match your original code (for mirrored video)
    const finalX = 1.0 - normalizedX;
    const finalY = normalizedY;

    // Apply smoothing
    smoothedGazeX += (finalX - smoothedGazeX) * GAZE_SMOOTHING_FACTOR;
    smoothedGazeY += (finalY - smoothedGazeY) * GAZE_SMOOTHING_FACTOR;

    // Fire the event for Unity
    window.dispatchEvent(new CustomEvent('ai_gaze', { detail: { x: smoothedGazeX, y: smoothedGazeY } }));
  });

  // Use a standard regression model
  webgazer.setRegression('ridge');
}

/**
 * Shows the calibration dot at a specific screen position
 */
function showCalibrationDot(x, y) {
  if (!calibrationDot) return;
  calibrationDot.style.display = 'block';
  // We use percentages for flexible screen size
  calibrationDot.style.left = `${x}%`;
  calibrationDot.style.top = `${y}%`;
}

/**
 * Hides the calibration dot
 */
function hideCalibrationDot() {
  if (!calibrationDot) return;
  calibrationDot.style.display = 'none';
}

/**
 * Runs the hands-free, view-only calibration sequence.
 * This should be triggered by the user (e.g., on-screen button "Start Calibration")
 */
async function startCalibration() {
  console.log("Starting hands-free calibration...");
  if (!webgazer) {
    console.error("WebGazer is not ready.");
    return;
  }

  // Clear any old calibration data
  await webgazer.clearData();

  // Use the same video element as MediaPipe
  webgazer.setVideoElement(video);
  
  // Hide default WebGazer UI
  webgazer.showVideo(false);
  webgazer.showPredictionPoints(false);
  
  // Start WebGazer's prediction engine (but we'll pause it)
  await webgazer.begin();
  webgazer.pause();
  console.log("WebGazer engine started, beginning calibration points...");

  // Define our 9 calibration points (as percentages of screen)
  const points = [
    [50, 50], // Center
    [10, 10], // Top-left
    [90, 10], // Top-right
    [10, 90], // Bottom-left
    [90, 90], // Bottom-right
    [10, 50], // Mid-left
    [90, 50], // Mid-right
    [50, 10], // Mid-top
    [50, 90]  // Mid-bottom
  ];

  const TIME_TO_SACCADE = 1500; // 1.5 sec: Time for user to look at the new dot
  const TIME_PER_POINT = 2000;  // 2 sec: Time to record data at that dot
  const SAMPLES_PER_POINT = 20; // How many data points to feed WebGazer

  for (const [xPct, yPct] of points) {
    const pixelX = window.innerWidth * (xPct / 100);
    const pixelY = window.innerHeight * (yPct / 100);

    // Show dot using percentages
    showCalibrationDot(xPct, yPct);
    await sleep(TIME_TO_SACCADE); // Wait for user to look

    // Now, record data for this point
    for (let i = 0; i < SAMPLES_PER_POINT; i++) {
      // This tells WebGazer: "Right now, the user is looking at (pixelX, pixelY)"
      // We use 'fixation' type instead of 'click'
      await webgazer.recordScreenPosition(pixelX, pixelY, 'fixation');
      await sleep(TIME_PER_POINT / SAMPLES_PER_POINT);
    }

    hideCalibrationDot();
    await sleep(500); // Pause before next dot
  }

  // --- Calibration Finished ---
  hideCalibrationDot();
  console.log("Calibration complete!");
  
  // Now, save the model and resume the gaze listener
  webgazer.resume();
  window.dispatchEvent(new CustomEvent("gaze_ready"));
}

// --- Main Prediction Loop (MediaPipe ONLY) ---
function predictWebcam() {
  const startTimeMs = performance.now();
  
  if (!video || !video.currentTime || video.currentTime === lastVideoTime || !canvasCtx) {
    window.requestAnimationFrame(predictWebcam);
    return;
  }
  lastVideoTime = video.currentTime;
  
  canvasCtx.clearRect(0, 0, canvasCtx.canvas.width, canvasCtx.canvas.height);

  let results;

  // --- Lightweight Mode (Gestures Only) ---
  if (!faceModel) {
    window.requestAnimationFrame(predictWebcam);
    return;
  }
  
  results = faceModel.detectForVideo(video, startTimeMs);
  
  // --- Process Results ---
  if (results && results.faceLandmarks && results.faceLandmarks.length > 0) {
    const landmarks = results.faceLandmarks[0];
    
    // console.log("Number of landmarks received:", landmarks.length) // Should be 468

    // 1. Detect Winks
    detectWinks(landmarks);
    
    // 2. Detect Rotation
    if (results.facialTransformationMatrixes && results.facialTransformationMatrixes.length > 0) {
      detectRotation(results.facialTransformationMatrixes[0]);
    }
    
    // --- WebGazer Integration --- REMOVED trackGaze(landmarks);
    
    // --- Draw Debug Mesh ---
    drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_TESSELATION, {color: "#C0C0C070", lineWidth: 1});
    drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_LEFT_EYE, { color: "#30FF30" });
    drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_RIGHT_EYE, { color: "#FF3030" });
    
    // --- WebGazer Integration --- REMOVED iris drawing
  }

  // Request the next frame
  window.requestAnimationFrame(predictWebcam);
}

// --- Detection Logic Functions ---

function detectWinks(landmarks) {
  // (This function is unchanged)
  const leftEAR = calculateEAR(landmarks, LEFT_EYE_INDICES);
  const rightEAR = calculateEAR(landmarks, RIGHT_EYE_INDICES);
  const isLeftOpen = leftEAR > WINK_EAR_THRESHOLD;
  const isRightOpen = rightEAR > WINK_EAR_THRESHOLD;

  // Right Wink (Select)
  if (!isRightOpen && isLeftOpen) { rightWinkCounter++; } else {
    if (rightWinkCounter > WINK_CONSECUTIVE_FRAMES && !rightWinkFired) {
      console.log("ACTION: Right Wink (Select)");
      window.dispatchEvent(new CustomEvent('ai_action', { detail: 'select' }));
      rightWinkFired = true;
    }
    rightWinkCounter = 0;
    rightWinkFired = false;
  }
  // Left Wink (Deselect)
  if (!isLeftOpen && isRightOpen) { leftWinkCounter++; } else {
    if (leftWinkCounter > WINK_CONSECUTIVE_FRAMES && !leftWinkFired) {
      console.log("ACTION: Left Wink (Deselect)");
      window.dispatchEvent(new CustomEvent('ai_action', { detail: 'deselect' }));
      leftWinkFired = true;
    }
    leftWinkCounter = 0;
    leftWinkFired = false;
  }
}

// --- WebGazer Integration --- DELETED the trackGaze function

function detectRotation(matrix) {
  // (This function is unchanged)
    if (!matrix || !matrix.data) return;
    const yaw = getHeadYaw(matrix.data);
    
    if (yaw > YAW_THRESHOLD && !isTurnedRight) {
        console.log("ACTION: Head Turn Right");
        window.dispatchEvent(new CustomEvent('ai_rotate', { detail: 'right' }));
        isTurnedRight = true; isTurnedLeft = false;
    } else if (yaw < -YAW_THRESHOLD && !isTurnedLeft) {
        console.log("ACTION: Head Turn Left");
        window.dispatchEvent(new CustomEvent('ai_rotate', { detail: 'left' }));
        isTurnedLeft = true; isTurnedRight = false;
    } else if (Math.abs(yaw) < YAW_RESET_THRESHOLD) {
        isTurnedLeft = false; isTurnedRight = false;
    }
}

function getHeadYaw(matrixData) {
  // (This function is unchanged)
    if (!matrixData || matrixData.length < 9) return 0;
    const yawRadians = Math.atan2(-matrixData[8], matrixData[0]);
    return yawRadians * (180 / Math.PI);
}

// --- Exports ---
export {
  createFaceLandmarker, // Main entry point
  init,
  startPredictionLoop,
  startCalibration // --- WebGazer Integration --- NEW export
}