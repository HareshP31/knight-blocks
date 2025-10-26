/*
import {
  FaceLandmarker,
  FilesetResolver,
  DrawingUtils
} from "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.0/vision_bundle.js"

// --- Model Variables ---
let faceModel;      // Model 1: Fast, lightweight for Winks/Rotation
let irisModel;      // Model 2: Precision, includes Iris for Gaze
let video;
let canvasCtx;
let drawingUtils;
let lastVideoTime = -1;

// --- Landmark Indices ---
const LEFT_EYE_INDICES = [362, 385, 387, 263, 373, 380];
const RIGHT_EYE_INDICES = [33, 160, 158, 133, 153, 144];
const LEFT_IRIS_INDICES = [474, 475, 476, 477];
const RIGHT_IRIS_INDICES = [469, 470, 471, 472];

// --- Tuning Knobs ---
const WINK_EAR_THRESHOLD = 0.25;
const WINK_CONSECUTIVE_FRAMES = 3;
const GAZE_SMOOTHING_FACTOR = 0.1;
const YAW_THRESHOLD = 15.0;
const YAW_RESET_THRESHOLD = 5.0;
const GAZE_X_OFFSET_MIN = -0.001;
const GAZE_X_OFFSET_MAX = 0.001;
const GAZE_Y_OFFSET_MIN = -0.0025;
const GAZE_Y_OFFSET_MAX = 0.0025;
const GAZE_Y_BOOST = 1.5;

// --- State Variables ---
let leftWinkCounter = 0, rightWinkCounter = 0;
let leftWinkFired = false, rightWinkFired = false;
let smoothedGazeX = 0.5, smoothedGazeY = 0.5;
let isTurnedLeft = false, isTurnedRight = false;

// --- Helper Functions ---
function getEuclideanDistance(p1, p2) {
    // Calculates the 3D distance between two points
    if (!p1 || !p2) return 0; // Guard against missing landmarks
    return Math.sqrt(Math.pow(p1.x - p2.x, 2) + Math.pow(p1.y - p2.y, 2) + Math.pow(p1.z - p2.z, 2));
}
function getAveragePoint(landmarks, indices) {
    // Averages the XY coordinates of specified landmarks
    let sumX = 0, sumY = 0;
    let count = 0;
    for (const index of indices) {
        if (landmarks[index]) { // Check if landmark exists
           sumX += landmarks[index].x;
           sumY += landmarks[index].y;
           count++;
        }
    }
    if (count === 0) return { x: 0.5, y: 0.5 }; // Default to center if no landmarks found
    return { x: sumX / count, y: sumY / count };
}
function mapValue(value, fromMin, fromMax, toMin, toMax) {
  // Maps a value from one range to another, clamping the result
  if (fromMax - fromMin === 0) return toMin; // Avoid division by zero
  const normalized = (value - fromMin) / (fromMax - fromMin);
  const mapped = normalized * (toMax - toMin) + toMin;
  return Math.max(toMin, Math.min(toMax, mapped));
}
function calculateEAR(landmarks, eyeIndices) {
    // Calculates Eye Aspect Ratio
    if (eyeIndices.some(index => !landmarks[index])) return 0.5; // Return neutral if landmarks missing
    const p1 = landmarks[eyeIndices[0]], p2 = landmarks[eyeIndices[1]], p3 = landmarks[eyeIndices[2]], p4 = landmarks[eyeIndices[3]], p5 = landmarks[eyeIndices[4]], p6 = landmarks[eyeIndices[5]];
    const verticalDist1 = getEuclideanDistance(p2, p6);
    const verticalDist2 = getEuclideanDistance(p3, p5);
    const horizontalDist = getEuclideanDistance(p1, p4);
    if (horizontalDist === 0) return 0.5; // Avoid division by zero
    return (verticalDist1 + verticalDist2) / (2.0 * horizontalDist);
}


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
      outputFaceBlendshapes: true // Required for both models (enables iris in the heavy one)
    });
}


async function createFaceLandmarker() {
  console.log("Loading AI Engine models...");
  
  // --- 1. Load the Lightweight Model First ---
  try {
    const modelPath = 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task';
    faceModel = await createLandmarker(modelPath);
    console.log("AI Model (Lightweight - Gestures) loaded successfully.");
    window.dispatchEvent(new CustomEvent("ai_ready")); // Fire 'ready' for basic features
    
    // --- 2. Now, load the Precision Iris Model in the background ---
    loadIrisModel(); // Start loading, but don't wait for it to finish

  } catch (error) {
    console.error("CRITICAL ERROR: Could not load lightweight face model:", error);
    // Optionally, inform the user via a UI update or custom event
  }
}


async function loadIrisModel() {
  console.log("Loading AI Model (Attempting Iris via Lightweight + Flag) in background...");
  try {
    // --- TRYING LIGHTWEIGHT PATH + BLENDSHAPES FLAG ---
    const modelPath = 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task'; // Cache bust
    // --- END CHANGE ---

    // We still use createLandmarker helper, which correctly sets outputFaceBlendshapes: true
    irisModel = await createLandmarker(modelPath);

    console.log("AI Model (Secondary Load Attempt) loaded successfully.");
    // We still fire gaze_ready, assuming this model *might* provide iris data
    window.dispatchEvent(new CustomEvent("gaze_ready"));

  } catch (error) {
    console.error("Error loading secondary model (Gaze tracking likely disabled):", error);
    // Gaze tracking will simply not activate, but the app continues
  }
}

// --- Engine Initialization and Loop Control ---

function init(videoElement, canvasElement) {
  // Sets up video and canvas references
  video = videoElement;
  canvasCtx = canvasElement.getContext("2d");
  drawingUtils = new DrawingUtils(canvasCtx);
  console.log("AI Engine initialized with video and canvas.");
}

function startPredictionLoop() {
  // Kicks off the main processing loop
  console.log("Starting prediction loop...")
  predictWebcam();
}

// --- Main Prediction Loop ---
function predictWebcam() {
  const startTimeMs = performance.now();
  
  // Basic checks to ensure video is ready and time has advanced
  if (!video || !video.currentTime || video.currentTime === lastVideoTime || !canvasCtx) {
    window.requestAnimationFrame(predictWebcam);
    return;
  }
  lastVideoTime = video.currentTime;
  
  // Clear the canvas for drawing
  canvasCtx.clearRect(0, 0, canvasCtx.canvas.width, canvasCtx.canvas.height);

  let results;
  let landmarks;
  let transformationMatrix = null;
  let activeModel = null;

  // --- Choose the Best Available Model ---
  if (irisModel) {
    // --- Precision Mode (Gaze + Gestures) ---
    activeModel = irisModel;
    results = irisModel.detectForVideo(video, startTimeMs);
  } else if (faceModel) {
    // --- Lightweight Mode (Gestures Only) ---
    activeModel = faceModel;
    results = faceModel.detectForVideo(video, startTimeMs);
  } else {
    // No model loaded yet, skip processing
    window.requestAnimationFrame(predictWebcam);
    return;
  }
  
  // --- Process Results ---
  if (results && results.faceLandmarks && results.faceLandmarks.length > 0) {
    landmarks = results.faceLandmarks[0];
    
    console.log("Number of landmarks received:", landmarks.length)

    // Always run Wink and Rotation detection (if faceModel is loaded)
    if (faceModel && landmarks) {
      detectWinks(landmarks);
      if (results.facialTransformationMatrixes && results.facialTransformationMatrixes.length > 0) {
        transformationMatrix = results.facialTransformationMatrixes[0];
        detectRotation(transformationMatrix);
      }
    }
    
    // ONLY run Gaze tracking if the precision model is loaded
    if (irisModel && landmarks) {
      trackGaze(landmarks); 
    }
    
    // --- Draw Debug Mesh ---
    if (landmarks) {
      drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_TESSELATION, {color: "#C0C0C070", lineWidth: 1});
      drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_LEFT_EYE, { color: "#30FF30" });
      drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_RIGHT_EYE, { color: "#FF3030" });
      
      // Only draw iris if the precision model is loaded and landmarks exist
      if (irisModel && landmarks[LEFT_IRIS_INDICES[0]]) { // Check if iris landmarks are present
          drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_LEFT_IRIS, { color: "#0000FF-500", lineWidth: 2 });
          drawingUtils.drawConnectors(landmarks, FaceLandmarker.FACE_LANDMARKS_RIGHT_IRIS, { color: "#e1e50cff", lineWidth: 2 });
      }
    }
  }

  // Request the next frame
  window.requestAnimationFrame(predictWebcam);
}

// --- Detection Logic Functions ---

function detectWinks(landmarks) {
  // Calculates EAR and fires 'ai_action' events
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

function trackGaze(landmarks) {
    // Calculates relative iris offset and fires 'ai_gaze' events
    // Ensure landmarks exist before calculating
    if (!landmarks[LEFT_EYE_INDICES[0]] || !landmarks[RIGHT_EYE_INDICES[0]] || !landmarks[LEFT_IRIS_INDICES[0]] || !landmarks[RIGHT_IRIS_INDICES[0]]) {
        return; // Skip if essential landmarks are missing
    }

    const leftEyeCenter = getAveragePoint(landmarks, LEFT_EYE_INDICES);
    const rightEyeCenter = getAveragePoint(landmarks, RIGHT_EYE_INDICES);
    const leftIrisCenter = getAveragePoint(landmarks, LEFT_IRIS_INDICES);
    const rightIrisCenter = getAveragePoint(landmarks, RIGHT_IRIS_INDICES);
    
    // Check for valid iris centers (can be {x:0, y:0} if getAveragePoint failed)
    if (leftIrisCenter.x === 0.5 && leftIrisCenter.y === 0.5 && rightIrisCenter.x === 0.5 && rightIrisCenter.y === 0.5) {
        return; // Skip if iris landmarks weren't found
    }

    const irisOffsetX = ((leftIrisCenter.x - leftEyeCenter.x) + (rightIrisCenter.x - rightEyeCenter.x)) / 2;
    const irisOffsetY = ((leftIrisCenter.y - leftEyeCenter.y) + (rightIrisCenter.y - rightEyeCenter.y)) / 2;

    console.log(`Raw Vertical Offset (irisOffsetY): ${irisOffsetY.toFixed(5)}`);
    
    let normalizedX = mapValue(irisOffsetX, GAZE_X_OFFSET_MIN, GAZE_X_OFFSET_MAX, 0.0, 1.0);
    let normalizedY = mapValue(irisOffsetY, GAZE_Y_OFFSET_MIN, GAZE_Y_OFFSET_MAX, 0.0, 1.0);
    
    normalizedY = 0.5 + (normalizedY - 0.5) * GAZE_Y_BOOST;
    normalizedY = Math.max(0.0, Math.min(1.0, normalizedY));
    
    const finalX = 1.0 - normalizedX; // Invert X for mirrored video
    const finalY = normalizedY;
    
    smoothedGazeX += (finalX - smoothedGazeX) * GAZE_SMOOTHING_FACTOR;
    smoothedGazeY += (finalY - smoothedGazeY) * GAZE_SMOOTHING_FACTOR;

    console.log(`Smoothed Coords: x=${smoothedGazeX.toFixed(4)}, y=${smoothedGazeY.toFixed(4)}`);
    
    window.dispatchEvent(new CustomEvent('ai_gaze', { detail: { x: smoothedGazeX, y: smoothedGazeY } }));
}

function detectRotation(matrix) {
    // Calculates yaw and fires 'ai_rotate' events
    if (!matrix || !matrix.data) return; // Guard clause
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
    // Extracts yaw rotation from transformation matrix
    if (!matrixData || matrixData.length < 9) return 0; // Guard clause
    const yawRadians = Math.atan2(-matrixData[8], matrixData[0]); // Use element 8 for yaw in Mediapipe matrix
    return yawRadians * (180 / Math.PI); // Convert to degrees
}

// --- Exports ---
export {
  createFaceLandmarker, // Main entry point
  init,
  startPredictionLoop
}
*/