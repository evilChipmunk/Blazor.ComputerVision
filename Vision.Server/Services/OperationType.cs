using System.ComponentModel;

namespace Vision.Server.Services
{ 
    public enum OperationType
    {
        [Description("None")]
        None,

        [Description("Grayscale")]
        Grayscale,

        [Description("Optical Flow")]
        OpticalFlow,

        [Description("Facial Detection")]
        FacialDetection,

        [Description("ORB")]
        Orb,

        [Description("ORB Top 10")]
        OrbBestFeatures,

        [Description("Heatmap")]
        Heatmap,

        [Description("Threshold")]
        Threshold,

        [Description("Canny")]
        Canny,

        [Description("Facial Recognition")]
        FacialRecognition,

        [Description("A-Kaze")]
        Akaze,

        [Description("FFT")]
        FFT,

        [Description("YOLO Object Identification")]
        Yolo,

        [Description("Particle Filter")]
        Particle,

        [Description("Convolution Filter")]
        ConvolutionFilter,

        [Description("OpenCV Object Tracking")]
        ObjectTrackingOpenCV,

        [Description("YOLO Multi-Object Tracking")]
        ObjectTrackingYOLO,

        [Description("Seam Carving")]
        SeamCarving,

        [Description("Corner Detection")]
        CornerDetection,
    }
} 
