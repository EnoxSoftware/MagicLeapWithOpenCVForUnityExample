
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

using MagicLeapWithOpenCVForUnity.UnityUtils.Helper;
using UnityEngine.XR.MagicLeap;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco MLCameraPreview Example
    /// An example of marker-based AR view and camera pose estimation using the aruco (ArUco Marker Detection) module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// </summary>
    [RequireComponent(typeof(MLCameraPreviewToMatHelper), typeof(ImageOptimizationHelper))]
    public class ArUcoMLCameraPreviewExample : MonoBehaviour
    {
        /// <summary>
        /// Determines if restores the camera parameters when the file exists.
        /// </summary>
        public bool useStoredCameraParameters = true;

        /// <summary>
        /// The marker type.
        /// </summary>
        public MarkerType markerType = MarkerType.CanonicalMarker;

        /// <summary>
        /// The marker type dropdown.
        /// </summary>
        public Dropdown markerTypeDropdown;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// Determines if shows rejected corners.
        /// </summary>
        public bool showRejectedCorners = false;

        /// <summary>
        /// The shows rejected corners toggle.
        /// </summary>
        public Toggle showRejectedCornersToggle;

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// Determines if refine marker detection. (only valid for ArUco boards)
        /// </summary>
        public bool refineMarkerDetection = true;

        /// <summary>
        /// The shows refine marker detection toggle.
        /// </summary>
        public Toggle refineMarkerDetectionToggle;

        [Space(10)]

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.1f;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject arGameObject;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;

        [Space(10)]

        /// <summary>
        /// Determines if request the AR camera moving.
        /// </summary>
        public bool shouldMoveARCamera = false;

        [Space(10)]

        /// <summary>
        /// Determines if enable low pass filter.
        /// </summary>
        public bool enableLowPassFilter;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle enableLowPassFilterToggle;

        /// <summary>
        /// The position low pass. (Value in meters)
        /// </summary>
        public float positionLowPass = 0.005f;

        /// <summary>
        /// The rotation low pass. (Value in degrees)
        /// </summary>
        public float rotationLowPass = 2f;

        /// <summary>
        /// The old pose data.
        /// </summary>
        PoseData oldPoseData;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        MLCameraPreviewToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// rgbaMat
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The identifiers.
        /// </summary>
        Mat ids;

        /// <summary>
        /// The corners.
        /// </summary>
        List<Mat> corners;

        /// <summary>
        /// The rejected corners.
        /// </summary>
        List<Mat> rejectedCorners;

        /// <summary>
        /// The rvecs.
        /// </summary>
        Mat rvecs;

        /// <summary>
        /// The tvecs.
        /// </summary>
        Mat tvecs;

        /// <summary>
        /// The rot mat.
        /// </summary>
        Mat rotMat;

        /// <summary>
        /// The detector parameters.
        /// </summary>
        DetectorParameters detectorParams;

        /// <summary>
        /// The dictionary.
        /// </summary>
        Dictionary dictionary;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        Mat rvec;
        Mat tvec;
        Mat recoveredIdxs;

        // for GridBoard.
        // number of markers in X direction
        const int gridBoradMarkersX = 5;
        // number of markers in Y direction
        const int gridBoradMarkersY = 7;
        // marker side length (normally in meters)
        const float gridBoradMarkerLength = 0.04f;
        // separation between two markers (same unit as markerLength)
        const float gridBoradMarkerSeparation = 0.01f;
        // id of first marker in dictionary to use on board.
        const int gridBoradMarkerFirstMarker = 0;
        GridBoard gridBoard;

        // for ChArUcoBoard.
        //  number of chessboard squares in X direction
        const int chArUcoBoradSquaresX = 5;
        //  number of chessboard squares in Y direction
        const int chArUcoBoradSquaresY = 7;
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        const int charucoMinMarkers = 2;
        Mat charucoCorners;
        Mat charucoIds;
        CharucoBoard charucoBoard;

        // for ChArUcoDiamondMarker.
        // size of the chessboard squares in pixels
        const float diamondSquareLength = 0.1f;
        // size of the markers in pixels.
        const float diamondMarkerLength = 0.06f;
        // identifiers for diamonds in diamond corners.
        const int diamondId1 = 45;
        const int diamondId2 = 68;
        const int diamondId3 = 28;
        const int diamondId4 = 74;
        List<Mat> diamondCorners;
        Mat diamondIds;

        Matrix4x4 invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));



        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// Determines if enable downscale.
        /// </summary>
        public bool enableDownScale;


        /// <summary>
        /// Determines if enable skipframe.
        /// </summary>
        public bool enableSkipFrame;

        CancellationTokenSource tokenSource = new CancellationTokenSource();

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            markerTypeDropdown.value = (int)markerType;
            dictionaryIdDropdown.value = (int)dictionaryId;
            showRejectedCornersToggle.isOn = showRejectedCorners;
            refineMarkerDetectionToggle.isOn = refineMarkerDetection;
            refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);
            enableLowPassFilterToggle.isOn = enableLowPassFilter;

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<MLCameraPreviewToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();
            Mat downscaleMat = imageOptimizationHelper.GetDownScaleMat(webCamTextureMat);

            if (enableDownScale)
            {
                texture = new Texture2D(downscaleMat.cols(), downscaleMat.rows(), TextureFormat.RGB24, false);
            }
            else
            {
                texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGB24, false);
            }

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            //gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            //Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("downscaleRaito", imageOptimizationHelper.downscaleRatio.ToString());
                fpsMonitor.Add("frameSkippingRatio", imageOptimizationHelper.frameSkippingRatio.ToString());
                fpsMonitor.Add("downscale_width", downscaleMat.width().ToString());
                fpsMonitor.Add("downscale_height", downscaleMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

#if PLATFORM_LUMIN && !UNITY_EDITOR

            MLCVCameraIntrinsicCalibrationParameters outParameters = new MLCVCameraIntrinsicCalibrationParameters();
            MLResult result = MLCamera.GetIntrinsicCalibrationParameters(out outParameters);
            if (result.IsOk)
            {
                //Imgproc.putText(rgbaMat, "Width : " + outParameters.Width, new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Height : " + outParameters.Height, new Point(20, 180), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "FocalLength : " + outParameters.FocalLength, new Point(20, 270), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "FOV : " + outParameters.FOV, new Point(20, 360), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "PrincipalPoint : " + outParameters.PrincipalPoint, new Point(20, 450), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Distortion [0] : " + outParameters.Distortion[0], new Point(20, 540), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Distortion [1] : " + outParameters.Distortion[1], new Point(20, 630), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Distortion [2] : " + outParameters.Distortion[2], new Point(20, 720), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Distortion [3] : " + outParameters.Distortion[3], new Point(20, 810), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(rgbaMat, "Distortion [4] : " + outParameters.Distortion[4], new Point(20, 900), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);

            }
            else
            {
                if (result.Code == MLResultCode.PrivilegeDenied)
                {
                    //Imgproc.putText(rgbaMat, "MLResultCode.PrivilegeDenied", new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                }
                else if (result.Code == MLResultCode.UnspecifiedFailure)
                {
                    //Imgproc.putText(rgbaMat, "MLResultCode.UnspecifiedFailure", new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                }
            }


            Matrix4x4 pm = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues(outParameters.FocalLength.x, outParameters.FocalLength.y,
    outParameters.PrincipalPoint.x, outParameters.PrincipalPoint.y, outParameters.Width, outParameters.Height, 0.3f, 1000f);
            Debug.Log("CalculateProjectionMatrix: " + pm.ToString());



            float width = outParameters.Width;
            float height = outParameters.Height;
            if (enableDownScale)
            {
                width /= imageOptimizationHelper.downscaleRatio;
                height /= imageOptimizationHelper.downscaleRatio;
            }

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            //if (widthScale < heightScale) {
            //    Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            //    imageSizeScale = (float)Screen.height / (float)Screen.width;
            //} else {
            //    Camera.main.orthographicSize = height / 2;
            //}


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + width + "x" + height;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (useStoredCameraParameters && File.Exists(loadPath))
            {
                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                camMatrix = param.GetCameraMatrix();
                distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                fx = param.camera_matrix[0];
                fy = param.camera_matrix[4];
                cx = param.camera_matrix[2];
                cy = param.camera_matrix[5];
                if (enableDownScale)
                {
                    cx /= imageOptimizationHelper.downscaleRatio;
                    cy /= imageOptimizationHelper.downscaleRatio;
                }

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

            }
            else
            {
                fx = outParameters.FocalLength.x;
                fy = outParameters.FocalLength.y;
                cx = outParameters.PrincipalPoint.x;
                cy = outParameters.PrincipalPoint.y;
                if (enableDownScale)
                {
                    cx /= imageOptimizationHelper.downscaleRatio;
                    cy /= imageOptimizationHelper.downscaleRatio;
                }

                camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                camMatrix.put(0, 0, fx);
                camMatrix.put(0, 1, 0);
                camMatrix.put(0, 2, cx);
                camMatrix.put(1, 0, 0);
                camMatrix.put(1, 1, fy);
                camMatrix.put(1, 2, cy);
                camMatrix.put(2, 0, 0);
                camMatrix.put(2, 1, 0);
                camMatrix.put(2, 2, 1.0f);

                distCoeffs = new MatOfDouble(outParameters.Distortion);

                Debug.Log("Created a dummy CameraParameters.");
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());


            //calibration camera matrix values.
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);


            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            // To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            // Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                arCamera.fieldOfView = (float)(fovx[0] * fovXScale);
            }
            else
            {
                arCamera.fieldOfView = (float)(fovy[0] * fovYScale);
            }
            // Display objects near the camera.
            arCamera.nearClipPlane = 0.01f;

#else
            float width = webCamTextureMat.width() / imageOptimizationHelper.downscaleRatio;
            float height = webCamTextureMat.height() / imageOptimizationHelper.downscaleRatio;
            if (enableDownScale)
            {
                width /= imageOptimizationHelper.downscaleRatio;
                height /= imageOptimizationHelper.downscaleRatio;
            }

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            //if (widthScale < heightScale)
            //{
            //    Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            //    imageSizeScale = (float)Screen.height / (float)Screen.width;
            //}
            //else
            //{
            //    Camera.main.orthographicSize = height / 2;
            //}


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + width + "x" + height;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (useStoredCameraParameters && File.Exists(loadPath))
            {
                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                camMatrix = param.GetCameraMatrix();
                distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                fx = param.camera_matrix[0];
                fy = param.camera_matrix[4];
                cx = param.camera_matrix[2];
                cy = param.camera_matrix[5];
                if (enableDownScale)
                {
                    cx /= imageOptimizationHelper.downscaleRatio;
                    cy /= imageOptimizationHelper.downscaleRatio;
                }

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

            }
            else
            {
                int max_d = (int)Mathf.Max(width, height);
                fx = max_d;
                fy = max_d;
                cx = width / 2.0f;
                cy = height / 2.0f;

                camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                camMatrix.put(0, 0, fx);
                camMatrix.put(0, 1, 0);
                camMatrix.put(0, 2, cx);
                camMatrix.put(1, 0, 0);
                camMatrix.put(1, 1, fy);
                camMatrix.put(1, 2, cy);
                camMatrix.put(2, 0, 0);
                camMatrix.put(2, 1, 0);
                camMatrix.put(2, 2, 1.0f);

                distCoeffs = new MatOfDouble(0, 0, 0, 0);

                Debug.Log("Created a dummy CameraParameters.");
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());


            // calibration camera matrix values.
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            // To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            // Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                arCamera.fieldOfView = (float)(fovx[0] * fovXScale);
            }
            else
            {
                arCamera.fieldOfView = (float)(fovy[0] * fovYScale);
            }
            // Display objects near the camera.
            arCamera.nearClipPlane = 0.01f;
#endif

            rgbaMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC4);
            if (enableDownScale)
            {
                rgbMat = new Mat(downscaleMat.rows(), downscaleMat.cols(), CvType.CV_8UC3);
            }
            else
            {
                rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            }
            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rvecs = new Mat();
            tvecs = new Mat();
            rotMat = new Mat(3, 3, CvType.CV_64FC1);


            detectorParams = DetectorParameters.create();
            dictionary = Aruco.getPredefinedDictionary((int)dictionaryId);

            rvec = new Mat();
            tvec = new Mat();
            recoveredIdxs = new Mat();

            gridBoard = GridBoard.create(gridBoradMarkersX, gridBoradMarkersY, gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary, gridBoradMarkerFirstMarker);

            charucoCorners = new Mat();
            charucoIds = new Mat();
            charucoBoard = CharucoBoard.create(chArUcoBoradSquaresX, chArUcoBoradSquaresY, chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);

            diamondCorners = new List<Mat>();
            diamondIds = new Mat(1, 1, CvType.CV_32SC4);
            diamondIds.put(0, 0, new int[] { diamondId1, diamondId2, diamondId3, diamondId4 });


            //// if WebCamera is frontFaceing, flip Mat.
            //if (webCamTextureToMatHelper.GetWebCamDevice ().isFrontFacing) {
            //    webCamTextureToMatHelper.flipHorizontal = true;
            //}


            //Main Process
            Process();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            //Cancel Task
            tokenSource.Cancel();

            if (rgbaMat != null)
                rgbaMat.Dispose();

            if (rgbMat != null)
                rgbMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();
            if (rvecs != null)
                rvecs.Dispose();
            if (tvecs != null)
                tvecs.Dispose();
            if (rotMat != null)
                rotMat.Dispose();

            if (rvec != null)
                rvec.Dispose();
            if (tvec != null)
                tvec.Dispose();
            if (recoveredIdxs != null)
                recoveredIdxs.Dispose();

            if (gridBoard != null)
                gridBoard.Dispose();

            if (charucoCorners != null)
                charucoCorners.Dispose();
            if (charucoIds != null)
                charucoIds.Dispose();
            if (charucoBoard != null)
                charucoBoard.Dispose();

            foreach (var item in diamondCorners)
            {
                item.Dispose();
            }
            diamondCorners.Clear();
            if (diamondIds != null)
                diamondIds.Dispose();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(MLCameraPreviewToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void EstimatePoseCanonicalMarker(Mat rgbMat)
        {
            Aruco.estimatePoseSingleMarkers(corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);

            for (int i = 0; i < ids.total(); i++)
            {
                using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Aruco.drawAxis(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    // This example can display the ARObject on only first detected marker.
                    if (i == 0)
                    {
                        UpdateARObjectTransform(rvec, tvec);
                    }
                }
            }
        }

        private void EstimatePoseGridBoard(Mat rgbMat)
        {
            int valid = Aruco.estimatePoseBoard(corners, ids, gridBoard, camMatrix, distCoeffs, rvec, tvec);

            // if at least one board marker detected
            if (valid > 0)
            {
                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                Aruco.drawAxis(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                UpdateARObjectTransform(rvec, tvec);
            }
        }

        private void EstimatePoseChArUcoBoard(Mat rgbMat)
        {
            // if at least one charuco corner detected
            if (charucoIds.total() > 0)
            {
                bool valid = Aruco.estimatePoseCharucoBoard(charucoCorners, charucoIds, charucoBoard, camMatrix, distCoeffs, rvec, tvec);

                // if at least one board marker detected
                if (valid)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Aruco.drawAxis(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    UpdateARObjectTransform(rvec, tvec);
                }
            }
        }

        private void EstimatePoseChArUcoDiamondMarker(Mat rgbMat)
        {
            Aruco.estimatePoseSingleMarkers(diamondCorners, diamondSquareLength, camMatrix, distCoeffs, rvecs, tvecs);

            for (int i = 0; i < rvecs.total(); i++)
            {
                using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Aruco.drawAxis(rgbMat, camMatrix, distCoeffs, rvec, tvec, diamondSquareLength * 0.5f);

                    // This example can display the ARObject on only first detected marker.
                    if (i == 0)
                    {
                        UpdateARObjectTransform(rvec, tvec);
                    }
                }
            }
        }

        private void UpdateARObjectTransform(Mat rvec, Mat tvec)
        {
            // Convert to unity pose data.
            double[] rvecArr = new double[3];
            rvec.get(0, 0, rvecArr);
            double[] tvecArr = new double[3];
            tvec.get(0, 0, tvecArr);

            if (enableDownScale)
            {
                tvecArr[2] /= imageOptimizationHelper.downscaleRatio;
            }

            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

            // Changes in pos/rot below these thresholds are ignored.
            if (enableLowPassFilter)
            {
                ARUtils.LowpassPoseData(ref oldPoseData, ref poseData, positionLowPass, rotationLowPass);
            }
            oldPoseData = poseData;

            // Convert to transform matrix.
            ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true, true);



            ARM = arCamera.cameraToWorldMatrix * invertZM * ARM;

            ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);

        }

        private void ResetObjectTransform()
        {
            // reset AR object transform.
            Matrix4x4 i = Matrix4x4.identity;
            ARUtils.SetTransformFromMatrix(arCamera.transform, ref i);
            ARUtils.SetTransformFromMatrix(arGameObject.transform, ref i);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (imageOptimizationHelper != null)
                imageOptimizationHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
        }

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)markerType != result)
            {
                markerType = (MarkerType)result;

                refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);

                ResetObjectTransform();

                if (webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;
                dictionary = Aruco.getPredefinedDictionary((int)dictionaryId);

                ResetObjectTransform();

                if (webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            showRejectedCorners = showRejectedCornersToggle.isOn;
        }

        /// <summary>
        /// Raises the refine marker detection toggle value changed event.
        /// </summary>
        public void OnRefineMarkerDetectionToggleValueChanged()
        {
            refineMarkerDetection = refineMarkerDetectionToggle.isOn;
        }


        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            if (enableLowPassFilterToggle.isOn)
            {
                enableLowPassFilter = true;
            }
            else
            {
                enableLowPassFilter = false;
            }
        }

        public enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            ChArUcoDiamondMarker
        }

        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Aruco.DICT_4X4_50,
            DICT_4X4_100 = Aruco.DICT_4X4_100,
            DICT_4X4_250 = Aruco.DICT_4X4_250,
            DICT_4X4_1000 = Aruco.DICT_4X4_1000,
            DICT_5X5_50 = Aruco.DICT_5X5_50,
            DICT_5X5_100 = Aruco.DICT_5X5_100,
            DICT_5X5_250 = Aruco.DICT_5X5_250,
            DICT_5X5_1000 = Aruco.DICT_5X5_1000,
            DICT_6X6_50 = Aruco.DICT_6X6_50,
            DICT_6X6_100 = Aruco.DICT_6X6_100,
            DICT_6X6_250 = Aruco.DICT_6X6_250,
            DICT_6X6_1000 = Aruco.DICT_6X6_1000,
            DICT_7X7_50 = Aruco.DICT_7X7_50,
            DICT_7X7_100 = Aruco.DICT_7X7_100,
            DICT_7X7_250 = Aruco.DICT_7X7_250,
            DICT_7X7_1000 = Aruco.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Aruco.DICT_ARUCO_ORIGINAL,
        }


        /// <summary>
        /// Process
        /// </summary>
        /// <returns></returns>
        private async void Process()
        {
            float DOWNSCALE_RATIO = 1.0f;

            while (true)
            {
                // Check TaskCancel
                if (tokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                rgbaMat = webCamTextureToMatHelper.GetMat();

                Mat downScaleRgbaMat = null;
                DOWNSCALE_RATIO = 1.0f;
                if (enableDownScale)
                {
                    downScaleRgbaMat = imageOptimizationHelper.GetDownScaleMat(rgbaMat);
                    DOWNSCALE_RATIO = imageOptimizationHelper.downscaleRatio;
                }
                else
                {
                    downScaleRgbaMat = rgbaMat;
                    DOWNSCALE_RATIO = 1.0f;
                }
                Imgproc.cvtColor(downScaleRgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

                await Task.Run(() =>
                {

                    if (!enableSkipFrame || !imageOptimizationHelper.IsCurrentFrameSkipped())
                    {

                        // detect markers.
                        Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners, camMatrix, distCoeffs);

                        // refine marker detection.
                        if (refineMarkerDetection && (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard))
                        {
                            switch (markerType)
                            {
                                case MarkerType.GridBoard:
                                    Aruco.refineDetectedMarkers(rgbMat, gridBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, 10f, 3f, true, recoveredIdxs, detectorParams);
                                    break;
                                case MarkerType.ChArUcoBoard:
                                    Aruco.refineDetectedMarkers(rgbMat, charucoBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, 10f, 3f, true, recoveredIdxs, detectorParams);
                                    break;
                            }
                        }

                        // if at least one marker detected
                        if (ids.total() > 0)
                        {
                            if (markerType != MarkerType.ChArUcoDiamondMarker)
                            {

                                if (markerType == MarkerType.ChArUcoBoard)
                                {
                                    Aruco.interpolateCornersCharuco(corners, ids, rgbMat, charucoBoard, charucoCorners, charucoIds, camMatrix, distCoeffs, charucoMinMarkers);

                                    // draw markers.
                                    Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));
                                    if (charucoIds.total() > 0)
                                    {
                                        Aruco.drawDetectedCornersCharuco(rgbMat, charucoCorners, charucoIds, new Scalar(0, 0, 255));
                                    }
                                }
                                else
                                {
                                    // draw markers.
                                    Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));
                                }


                            }
                            else
                            {
                                // detect diamond markers.
                                Aruco.detectCharucoDiamond(rgbMat, corners, ids, diamondSquareLength / diamondMarkerLength, diamondCorners, diamondIds, camMatrix, distCoeffs);

                                // draw markers.
                                Aruco.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));
                                // draw diamond markers.
                                Aruco.drawDetectedDiamonds(rgbMat, diamondCorners, diamondIds, new Scalar(0, 0, 255));


                            }
                        }

                        if (showRejectedCorners && rejectedCorners.Count > 0)
                            Aruco.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));

                    }

                });


                if (ids.total() > 0)
                {
                    if (markerType != MarkerType.ChArUcoDiamondMarker)
                    {
                        // estimate pose.
                        if (applyEstimationPose)
                        {
                            switch (markerType)
                            {
                                default:
                                case MarkerType.CanonicalMarker:
                                    EstimatePoseCanonicalMarker(rgbMat);
                                    break;
                                case MarkerType.GridBoard:
                                    EstimatePoseGridBoard(rgbMat);
                                    break;
                                case MarkerType.ChArUcoBoard:
                                    EstimatePoseChArUcoBoard(rgbMat);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // estimate pose.
                        if (applyEstimationPose)
                            EstimatePoseChArUcoDiamondMarker(rgbMat);
                    }
                }



                    //Debug.Log("arCamera.cameraToWorldMatrix " + arCamera.cameraToWorldMatrix.ToString());
                    //Debug.Log("arCamera.projectionMatrix " + arCamera.projectionMatrix.ToString());

                    //Debug.Log("arCamera.fieldOfView " + arCamera.fieldOfView);
                    //Debug.Log("arCamera.nearClipPlane " + arCamera.nearClipPlane);
                    //Debug.Log("arCamera.farClipPlane " + arCamera.farClipPlane);
                    //Debug.Log("arCamera.focalLength " + arCamera.focalLength);

                    //Debug.Log("arCamera.transform.localPosition " + arCamera.transform.localPosition);
                    //Debug.Log("arCamera.transform.localRotation " + arCamera.transform.localRotation);
                    //Debug.Log("arCamera.transform.localScale " + arCamera.transform.localScale);

                    //Imgproc.putText(rgbMat, "cameraToWorldMatrix : " + arCamera.cameraToWorldMatrix.ToString(), new Point(400, 400), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "projectionMatrix : " + arCamera.projectionMatrix.ToString(), new Point(400, 440), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "fieldOfView : " + arCamera.fieldOfView, new Point(400, 480), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "nearClipPlane : " + arCamera.nearClipPlane, new Point(400, 520), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "farClipPlane : " + arCamera.farClipPlane, new Point(400, 560), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "arCamera.focalLength " + arCamera.focalLength, new Point(400, 600), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "transform.localPosition : " + arCamera.transform.localPosition, new Point(400, 640), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "transform.localRotation : " + arCamera.transform.localRotation, new Point(400, 680), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);
                    //Imgproc.putText(rgbMat, "transform.localScale : " + arCamera.transform.localScale, new Point(400, 720), Imgproc.FONT_HERSHEY_SIMPLEX, 1, new Scalar(255, 0, 0, 255), 2);


                    //                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    //rgbMat.setTo(new Scalar(0, 0, 0));

                    Utils.fastMatToTexture2D(rgbMat, texture);

                Thread.Sleep(10);
            }
        }
    

     }
}
