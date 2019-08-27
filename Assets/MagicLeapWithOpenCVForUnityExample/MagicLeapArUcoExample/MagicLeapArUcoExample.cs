
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using MagicLeapWithOpenCVForUnity.UnityUtils.Helper;

using UnityEngine.XR.MagicLeap;
using System;
using System.Threading.Tasks;

namespace MagicLeapWithOpenCVForUnityExample
{
    /// <summary>
    /// MagicLeap ArUco Example
    /// An example of marker-based AR view and camera pose estimation using the aruco (ArUco Marker Detection) module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// </summary>
    [RequireComponent(typeof(MLCameraPreviewToMatHelper), typeof(ImageOptimizationHelper))]
    public class MagicLeapArUcoExample : MonoBehaviour
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
        /// The matrix that inverts the Y-axis.
        /// </summary>
        Matrix4x4 invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

        /// <summary>
        /// The matrix that inverts the Z-axis.
        /// </summary>
        Matrix4x4 invertZM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));

        /// <summary>
        /// The transformation matrix.
        /// </summary>
        Matrix4x4 transformationM;

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

        /// <summary>
        /// The camera offset matrix.
        /// </summary>
        Matrix4x4 cameraOffsetM = Matrix4x4.Translate(new Vector3(-0.07f, -0.005f, 0));

        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();
        Mat downScaleFrameMat;

        bool _isThreadRunning = false;
        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        bool _isDetecting = false;
        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

        // Use this for initialization
        void Start()
        {
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

            //Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

#if PLATFORM_LUMIN && !UNITY_EDITOR

            // get camera intrinsic calibration parameters.
            MLCVCameraIntrinsicCalibrationParameters outParameters = new MLCVCameraIntrinsicCalibrationParameters();
            MLResult result = MLCamera.GetIntrinsicCalibrationParameters(out outParameters);
            if (result.IsOk)
            {
                //Imgproc.putText(webCamTextureMat, "Width : " + outParameters.Width, new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Height : " + outParameters.Height, new Point(20, 180), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "FocalLength : " + outParameters.FocalLength, new Point(20, 270), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "FOV : " + outParameters.FOV, new Point(20, 360), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "PrincipalPoint : " + outParameters.PrincipalPoint, new Point(20, 450), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Distortion [0] : " + outParameters.Distortion[0], new Point(20, 540), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Distortion [1] : " + outParameters.Distortion[1], new Point(20, 630), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Distortion [2] : " + outParameters.Distortion[2], new Point(20, 720), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Distortion [3] : " + outParameters.Distortion[3], new Point(20, 810), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                //Imgproc.putText(webCamTextureMat, "Distortion [4] : " + outParameters.Distortion[4], new Point(20, 900), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
            }
            else
            {
                if (result.Code == MLResultCode.PrivilegeDenied)
                {
                    //Imgproc.putText(webCamTextureMat, "MLResultCode.PrivilegeDenied", new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                }
                else if (result.Code == MLResultCode.UnspecifiedFailure)
                {
                    //Imgproc.putText(webCamTextureMat, "MLResultCode.UnspecifiedFailure", new Point(20, 90), Imgproc.FONT_HERSHEY_SIMPLEX, 3, new Scalar(255, 0, 0, 255), 5);
                }
            }

            Matrix4x4 projectionMatrix = ARUtils.CalculateProjectionMatrixFromCameraMatrixValues(outParameters.FocalLength.x, outParameters.FocalLength.y,
                outParameters.PrincipalPoint.x, outParameters.PrincipalPoint.y, outParameters.Width, outParameters.Height, 0.3703704f, 10f);
            Debug.Log("CalculateProjectionMatrix: " + projectionMatrix.ToString());


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
            if (enableDownScale)
            {
                width /= imageOptimizationHelper.downscaleRatio;
                height /= imageOptimizationHelper.downscaleRatio;
            }

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;


            // set camera parameters.
            double fx = outParameters.FocalLength.x;
            double fy = outParameters.FocalLength.y;
            double cx = outParameters.PrincipalPoint.x;
            double cy = outParameters.PrincipalPoint.y;
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

#else
            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
            if (enableDownScale)
            {
                width /= imageOptimizationHelper.downscaleRatio;
                height /= imageOptimizationHelper.downscaleRatio;
            }

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;

            // set camera parameters.
            int max_d = (int)Mathf.Max(width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;

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

            // if WebCamera is frontFaceing, flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice().isFrontFacing)
            {
                webCamTextureToMatHelper.flipHorizontal = true;
            }

#endif

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

            // Display objects near the camera.
            arCamera.nearClipPlane = 0.01f;


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
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            StopThread();

            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Clear();
            }
            isDetecting = false;


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
            
            lock (ExecuteOnMainThread)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                if (!enableSkipFrame || !imageOptimizationHelper.IsCurrentFrameSkipped())
                {

                    if (!isDetecting)
                    {
                        isDetecting = true;

                        rgbaMat = webCamTextureToMatHelper.GetMat();
                        // Debug.Log ("rgbaMat.ToString() " + rgbaMat.ToString ());

                        // detect markers on the downscale image
                        Mat downScaleRgbaMat = null;
                        float DOWNSCALE_RATIO = 1.0f;
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

                        StartThread(
                            // action
                            () =>
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
                            },
                            // done
                            () => {

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

                                if (showRejectedCorners && rejectedCorners.Count > 0)
                                    Aruco.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));

                                Utils.fastMatToTexture2D(rgbMat, texture);

                                isDetecting = false;
                            }
                        );
                    }
                }
            }
            
        }

        private void StartThread(Action action, Action done)
        {
            Task.Run(() => {
                isThreadRunning = true;

                action();

                lock (ExecuteOnMainThread)
                {
                    if (ExecuteOnMainThread.Count == 0)
                    {
                        ExecuteOnMainThread.Enqueue(() => {
                            done();
                        });
                    }
                }

                isThreadRunning = false;
            });
        }

        private void StopThread()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning)
            {
                //Wait threading stop
            }
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


            // Create transform matrix.
            transformationM = Matrix4x4.TRS(poseData.pos, poseData.rot, Vector3.one);

            // Right-handed coordinates system (OpenCV) to left-handed one (Unity)
            // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
            ARM = invertYM * transformationM * invertYM;

            // Apply Y-axis and Z-axis refletion matrix. (Adjust the posture of the AR object)
            ARM = ARM * invertYM * invertZM;

            // Apply the cameraToWorld matrix with the Z-axis inverted.
            ARM = arCamera.cameraToWorldMatrix * invertZM * cameraOffsetM * ARM;

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
     }
}
