
let blazorComponent;
let captureInterval;
 
function registerComponent(dotNetReference) {
    blazorComponent = dotNetReference;
}


async function startWebcam() {
    try {
        const videoElement = document.querySelector('video');
        const stream = await navigator.mediaDevices.getUserMedia({ video: true });
        videoElement.srcObject = stream;
        // Wait for the video stream to load and play it
        videoElement.onloadedmetadata = () => {
            videoElement.play();
            // Optionally, start continuous capture after a short delay
            setTimeout(() => {
                startContinuousCapture(500); // Adjust intervalMs as needed
            }, 500); // Delay in milliseconds before starting capture
        };
    } catch (err) {
        console.error('Error accessing the webcam', err);
    }
}


function startContinuousCapture(intervalMs) {
    const videoElement = document.querySelector('video');
    captureInterval = setInterval(() => {
        const canvas = document.createElement('canvas');
        canvas.width = videoElement.videoWidth;
        canvas.height = videoElement.videoHeight;
        canvas.getContext('2d').drawImage(videoElement, 0, 0, canvas.width, canvas.height);
        const dataUrl = canvas.toDataURL('image/jpeg', 0.5); // Adjust for quality
        blazorComponent.invokeMethodAsync('ReceiveImage', dataUrl);
    }, intervalMs);

    // Provide a way to clear this interval to prevent memory leaks and performance issues
    window.stopContinuousCapture = function () {
        clearInterval(captureInterval);
    };
}


function stopWebcam() {
    const videoElement = document.querySelector('video');
    if (videoElement.srcObject) {
        videoElement.srcObject.getTracks().forEach(track => track.stop()); // Stop each track on the stream
        videoElement.srcObject = null; // Clear the video source
    }
    stopContinuousCapture(); // Also stop the continuous capture if it's running
}

function stopContinuousCapture() {
    if (captureInterval) {
        clearInterval(captureInterval);
    }
}
 