 /*
 
 import {
  FaceLandmarker,
  FilesetResolver,
  DrawingUtils
} from "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@latest/vision_bundle.js"

let faceLandmarker;
let video;
let canvasCtx;
let drawingUtils;
let lastVideoTime = -1;

const LEFT_EYE_INDICES = [
    362, // p1
    385, // p2
    387, // p3
    263, // p4
    373, // p5
    380  // p6
];
const RIGHT_EYE_INDICES = [
    33,  // p1
    160, // p2
    158, // p3
    133, // p4
    153, // p5
    144  // p6
];

const LEFT_IRIS_INDICES = [
    474,
    475,
    476,
    477
];
const RIGHT_IRIS_INDICES = [
    469,
    470,
    471,
    472
];

const WINK_THRESHOLD = 0.25;
const WINK_OPEN_THRESHOLD = WINK_THRESHOLD * 1.5;
const WINK_CONSECUTIVE_FRAMES = 3;
const GAZE_SMOOTHING_FACTOR = 0.1;
const YAW_THRESHOLD = 15.0;
const YAW_RESET_THRESHOLD = 5.0;

let leftWinkCounter = 0;
let rightWinkCounter = 0;

let leftWinkFired = false;
let rightWinkFired = false;

let smoothedGazeX = 0.5;
let smoothedGazeY = 0.5;

let isTurnedLeft = false;
let isTurnedRight = false;

const GAZE_X_OFFSET_MIN = -0.005;
const GAZE_X_OFFSET_MAX = 0.005;

// Vertical movement is less, so we use a smaller range.
const GAZE_Y_OFFSET_MIN = -0.01;
const GAZE_Y_OFFSET_MAX = 0.01;

// Boosts vertical sensitivity.
const GAZE_Y_BOOST = 1.5;

async function createFaceLandmarker() {
  console.log("Loading MeadiaPipe FaceLandmarker model...")
  const filesetResolver = await FilesetResolver.forVisionTasks(
    "https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@latest/wasm"
  );

  faceLandmarker = await FaceLandmarker.createFromOptions(
    filesetResolver, {
      baseOptions: {
        modelAssetPath: 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task'
      },
      runningMode: "VIDEO",
      numFaces: 1,
      outputFacialTransformationMatrixes: true,
      outputFaceBlendshapes: true
    });

    console.log("FaceLandmarker model loaded successfully");
    window.dispatchEvent(new CustomEvent("ai_ready"));
}

function init(videoElement, canvasElement) {
  video = videoElement;
  canvasCtx = canvasElement.getContext("2d");
  drawingUtils = new DrawingUtils(canvasCtx);
  console.log("AI Engine initialized with video and canvas.");
}

function startPredictionLoop() {
  console.log("Starting prediction loop...")
  predictWebcam();
}

function predictWebcam() {
  const startTimeMs = performance.now();

  if(video.currentTime !== lastVideoTime) {
    lastVideoTime = video.currentTime;

    const results = faceLandmarker.detectForVideo(video, startTimeMs)

    canvasCtx.clearRect(0, 0, canvasCtx.canvas.width, canvasCtx.canvas.height);

    if(results.faceLandmarks && results.faceLandmarks.length > 0) {

      const landmarks = results.faceLandmarks[0];

      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_TESSELATION,
        {color: "#C0C0C070", lineWidth: 1}
      );

      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_RIGHT_EYE,
        { color: "#FF3030" } // Red
      );
      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_LEFT_EYE,
        { color: "#30FF30" } // Green
      );
      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_FACE_OVAL,
        { color: "#E0E0E0" } // White
      );

      
      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_LEFT_IRIS,
        { color: "#30FF30", lineWidth: 2 } // Green
      );

      drawingUtils.drawConnectors(
        landmarks,
        FaceLandmarker.FACE_LANDMARKS_RIGHT_IRIS,
        { color: "#FF3030", lineWidth: 2 } // Red
      );
      
      detectWinks(landmarks);
      trackGaze(landmarks);

      if (results.facialTransformationMatrixes && results.facialTransformationMatrixes.length > 0) {
        detectRotation(results.facialTransformationMatrixes[0]);
      }
    }
  }

  window.requestAnimationFrame(predictWebcam);
}

function calculateEAR(landmarks, eyeIndices) {
    // Get the landmark points from the landmarks array
    const p1 = landmarks[eyeIndices[0]];
    const p2 = landmarks[eyeIndices[1]];
    const p3 = landmarks[eyeIndices[2]];
    const p4 = landmarks[eyeIndices[3]];
    const p5 = landmarks[eyeIndices[4]];
    const p6 = landmarks[eyeIndices[5]];

    // Calculate the vertical distances
    const verticalDist1 = getEuclideanDistance(p2, p6);
    const verticalDist2 = getEuclideanDistance(p3, p5);

    // Calculate the horizontal distance
    const horizontalDist = getEuclideanDistance(p1, p4);

    // Calculate EAR
    // The formula is: EAR = (||p2-p6|| + ||p3-p5||) / (2 * ||p1-p4||)
    const ear = (verticalDist1 + verticalDist2) / (2.0 * horizontalDist);
    return ear;
}

function detectWinks(landmarks) {
  const leftEAR = calculateEAR(landmarks, LEFT_EYE_INDICES);
  const rightEAR = calculateEAR(landmarks, RIGHT_EYE_INDICES);

  if (rightEAR < WINK_THRESHOLD && leftEAR > WINK_OPEN_THRESHOLD) {   
      rightWinkCounter++;

  } else {
    // Else, the eye is open.
    // Check if the counter passed the threshold AND we haven't fired the event yet
    if (rightWinkCounter > WINK_CONSECUTIVE_FRAMES && !rightWinkFired) {
        // --- ACTION: This is a confirmed wink! ---
        console.log("ACTION: Right Wink (Select)");
        window.dispatchEvent(new CustomEvent('ai_action', { detail: 'select' }));
        
        // Set the latch so we don't fire again
        rightWinkFired = true;
    }
    
    // Reset the counter and latch
    rightWinkCounter = 0;
    rightWinkFired = false;
  }

    // --- 2. Left Wink (Deselect) Logic ---
    // Check if the left eye is closed AND the right eye is open
    if (leftEAR < WINK_THRESHOLD && rightEAR > WINK_OPEN_THRESHOLD) {
        // If so, increment the counter
        leftWinkCounter++;
    } else {
        // Else, the eye is open.
        // Check if the counter passed the threshold AND we haven't fired the event yet
        if (leftWinkCounter > WINK_CONSECUTIVE_FRAMES && !leftWinkFired) {
            // --- ACTION: This is a confirmed wink! ---
            console.log("ACTION: Left Wink (Deselect)");
            window.dispatchEvent(new CustomEvent('ai_action', { detail: 'deselect' }));

            // Set the latch so we don't fire again
            leftWinkFired = true;
        }

        // Reset the counter and latch
        leftWinkCounter = 0;
        leftWinkFired = false;
    }
}

function getEuclideanDistance(p1, p2) {
    return Math.sqrt(
        Math.pow(p1.x - p2.x, 2) +
        Math.pow(p1.y - p2.y, 2) +
        Math.pow(p1.z - p2.z, 2)
    );
}

/*
function trackGaze(landmarks) {
  const leftEyeCenter = getAveragePoint(landmarks, LEFT_IRIS_INDICES);
    const rightEyeCenter = getAveragePoint(landmarks, RIGHT_IRIS_INDICES);

    const rawGazeX = (leftEyeCenter.x + rightEyeCenter.x) / 2;
    const rawGazeY = (leftEyeCenter.y + rightEyeCenter.y) / 2;
 
    const normalizedX = 1.0 - rawGazeX;
    const normalizedY = rawGazeY;
   
    smoothedGazeX = smoothedGazeX + (normalizedX - smoothedGazeX) * GAZE_SMOOTHING_FACTOR;
    smoothedGazeY = smoothedGazeY + (normalizedY - smoothedGazeY) * GAZE_SMOOTHING_FACTOR;
  
    window.dispatchEvent(new CustomEvent('ai_gaze', {
        detail: { x: smoothedGazeX, y: smoothedGazeY }
    }));
}
*/

/*
function mapValue(value, fromMin, fromMax, toMin, toMax) {
  // 1. Normalize the value (get it between 0.0 and 1.0)
  const normalized = (value - fromMin) / (fromMax - fromMin);
  
  // 2. Map the normalized value to the new range
  const mapped = normalized * (toMax - toMin) + toMin;

  // 3. Clamp the value to the new range
  return Math.max(toMin, Math.min(toMax, mapped));
}

/*
function trackGaze(landmarks) {
  const leftEye = getAveragePoint(landmarks, LEFT_EYE_INDICES);
  const rightEye = getAveragePoint(landmarks, RIGHT_EYE_INDICES);
  const leftIris = getAveragePoint(landmarks, LEFT_IRIS_INDICES);
  const rightIris = getAveragePoint(landmarks, RIGHT_IRIS_INDICES);

  // Calculate relative offset of each iris within its eye
  const leftOffsetX = (leftIris.x - leftEye.x);
  const rightOffsetX = (rightIris.x - rightEye.x);
  const leftOffsetY = (leftIris.y - leftEye.y);
  const rightOffsetY = (rightIris.y - rightEye.y);

  // Average both eyes
  let gazeX = (leftOffsetX + rightOffsetX) / 2;
  let gazeY = (leftOffsetY + rightOffsetY) / 2;

  // Scale to exaggerate the movement (tune between 10–40)
  const GAZE_SCALE_X = 35;
  const GAZE_SCALE_Y = 50;

  gazeX *= GAZE_SCALE_X;
  gazeY *= GAZE_SCALE_Y;

  // Flip X if your camera feed is mirrored
  gazeX = -gazeX;

  // Smooth
  smoothedGazeX += (gazeX - smoothedGazeX) * 0.25;
  smoothedGazeY += (gazeY - smoothedGazeY) * 0.25;

  // Center around 0.5 (middle of screen)
  const normalizedX = 0.5 + smoothedGazeX;
  const normalizedY = 0.5 + smoothedGazeY;

  window.dispatchEvent(new CustomEvent('ai_gaze', {
    detail: { x: normalizedX, y: normalizedY }
  }));
}
*/

/*
function trackGaze(landmarks) {
    // 1. Get Eye Centers (the 'sockets')
    const leftEyeCenter = getAveragePoint(landmarks, LEFT_EYE_INDICES);
    const rightEyeCenter = getAveragePoint(landmarks, RIGHT_EYE_INDICES);

    // 2. Get Iris Centers (the 'pupils')
    const leftIrisCenter = getAveragePoint(landmarks, LEFT_IRIS_INDICES);
    const rightIrisCenter = getAveragePoint(landmarks, RIGHT_IRIS_INDICES);

    // 3. Calculate the *Offset* (This is the real gaze direction)
    const irisOffsetX = ((leftIrisCenter.x - leftEyeCenter.x) + (rightIrisCenter.x - rightEyeCenter.x)) / 2;
    const irisOffsetY = ((leftIrisCenter.y - leftEyeCenter.y) + (rightIrisCenter.y - rightEyeCenter.y)) / 2;

    // 4. Map the raw offset to screen coordinates (0.0 - 1.0)
    // This is where we use our "static calibration" magic numbers
    let normalizedX = mapValue(irisOffsetX, GAZE_X_OFFSET_MIN, GAZE_X_OFFSET_MAX, 0.0, 1.0);
    let normalizedY = mapValue(irisOffsetY, GAZE_Y_OFFSET_MIN, GAZE_Y_OFFSET_MAX, 0.0, 1.0);

    // 5. Apply the vertical boost
    // We "center" the value (e.g., 0.6 -> 0.1), apply boost (0.1 -> 0.15), then "un-center" (0.15 -> 0.65)
    normalizedY = 0.5 + (normalizedY - 0.5) * GAZE_Y_BOOST;
    normalizedY = Math.max(0.0, Math.min(1.0, normalizedY)); // Re-clamp

    // 6. Invert X-axis
    // Your old code (1.0 - rawGazeX) was correct because the video feed is mirrored. We must do the same here.
    const finalX = 1.0 - normalizedX;
    const finalY = normalizedY;

    // 7. Smooth the final values and dispatch the event
    smoothedGazeX = smoothedGazeX + (finalX - smoothedGazeX) * GAZE_SMOOTHING_FACTOR;
    smoothedGazeY = smoothedGazeY + (finalY - smoothedGazeY) * GAZE_SMOOTHING_FACTOR;
 
    window.dispatchEvent(new CustomEvent('ai_gaze', {
        detail: { x: smoothedGazeX, y: smoothedGazeY }
    }));
}

function getAveragePoint(landmarks, indices) {
    let sumX = 0;
    let sumY = 0;
    
    for (const index of indices) {
        sumX += landmarks[index].x;
        sumY += landmarks[index].y;
    }
    
    return {
        x: sumX / indices.length,
        y: sumY / indices.length
    };
}

function detectRotation(matrix) {

    const yaw = getHeadYaw(matrix.data);

    if (yaw > YAW_THRESHOLD && !isTurnedRight) {

        console.log("ACTION: Head Turn Right");
        window.dispatchEvent(new CustomEvent('ai_rotate', { detail: 'right' }));
        
        // Set the latches
        isTurnedRight = true;
        isTurnedLeft = false; // Allow turning back left
    } 

    else if (yaw < -YAW_THRESHOLD && !isTurnedLeft) {
        // --- ACTION: This is a confirmed turn! ---
        console.log("ACTION: Head Turn Left");
        window.dispatchEvent(new CustomEvent('ai_rotate', { detail: 'left' }));
        
        // Set the latches
        isTurnedLeft = true;
        isTurnedRight = false; // Allow turning back right
    }
  
    else if (Math.abs(yaw) < YAW_RESET_THRESHOLD) {
        // Reset both latches
        isTurnedLeft = false;
        isTurnedRight = false;
    }

}

function getHeadYaw(matrixData) {
    const yawRadians = Math.atan2(-matrixData[8], matrixData[0]);
    
    // Convert radians to degrees
    return yawRadians * (180 / Math.PI);
}


export {
  createFaceLandmarker,
  init,
  startPredictionLoop
}
*/