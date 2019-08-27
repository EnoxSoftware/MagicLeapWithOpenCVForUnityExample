 using MagicLeapWithOpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.XR.MagicLeap;

namespace MagicLeapWithOpenCVForUnityExample
{
    /// <summary>
    /// MagicLeap FaceDetection Example
    /// </summary>
    [RequireComponent(typeof(MLCameraPreviewToMatHelper), typeof(ImageOptimizationHelper))]
    public class MagicLeapFaceDetectionExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        MLCameraPreviewToMatHelper webCamTextureToMatHelper;

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
        /// rgbaMat
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// detectResult
        /// </summary>
        OpenCVForUnity.CoreModule.Rect[] detectResult;

        /// <summary>
        /// the main camera.
        /// </summary>
        public Camera mainCamera;

        /// <summary>
        /// The quad renderer.
        /// </summary>
        Renderer quad_renderer;

        /// <summary>
        /// The camera offset matrix.
        /// </summary>
        Matrix4x4 cameraOffsetM = Matrix4x4.Translate(new Vector3(-0.07f, -0.005f, 0));

        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

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
            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();
            webCamTextureToMatHelper = gameObject.GetComponent<MLCameraPreviewToMatHelper>();
            webCamTextureToMatHelper.Initialize();


            cascade = new CascadeClassifier();
            //cascade.load (Utils.getFilePath ("lbpcascade_frontalface.xml"));
            cascade.load(Utils.getFilePath("haarcascade_frontalface_alt.xml"));
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();
            Mat downscaleMat = imageOptimizationHelper.GetDownScaleMat(webCamTextureMat);

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0,0));
            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);

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

            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
#else
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", mainCamera.projectionMatrix);
#endif


            //            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            rgbaMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC4);
            if (enableDownScale)
            {
                grayMat = new Mat(downscaleMat.rows(), downscaleMat.cols(), CvType.CV_8UC1);
            }
            else
            {
                grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
            }

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

            if (grayMat != null)
                grayMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
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

                        // detect faces on the downscale image
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
                        Imgproc.cvtColor(downScaleRgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                        StartThread(
                            // action
                            () =>
                            {
                                Imgproc.equalizeHist(grayMat, grayMat);

                                using (MatOfRect faces = new MatOfRect())
                                {

                                    cascade.detectMultiScale(grayMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size(grayMat.cols() * 0.15, grayMat.cols() * 0.15), new Size());

                                    //Debug.Log("faces.dump() " + faces.dump());

                                    detectResult = faces.toArray();
                                }

                                if (enableDownScale)
                                {
                                    for (int i = 0; i < detectResult.Length; ++i)
                                    {
                                        var rect = detectResult[i];
                                        detectResult[i] = new OpenCVForUnity.CoreModule.Rect(
                                            (int)(rect.x * DOWNSCALE_RATIO),
                                            (int)(rect.y * DOWNSCALE_RATIO),
                                            (int)(rect.width * DOWNSCALE_RATIO),
                                            (int)(rect.height * DOWNSCALE_RATIO));
                                    }
                                }
                            },
                            // done
                            () => {

                                // hide camera image.
                                //Imgproc.rectangle(rgbaMat, new Point(0, 0), new Point(rgbaMat.width(), rgbaMat.height()), new Scalar(0, 0, 0, 0), -1);

                                for (int i = 0; i < detectResult.Length; i++)
                                {
                                    Imgproc.rectangle(rgbaMat, new Point(detectResult[i].x, detectResult[i].y), new Point(detectResult[i].x + detectResult[i].width, detectResult[i].y + detectResult[i].height), new Scalar(255, 0, 0, 255), 4);
                                }

                                Utils.fastMatToTexture2D(rgbaMat, texture);


                                isDetecting = false;
                            }
                        );
                    }
                }
            }

            if (webCamTextureToMatHelper.IsPlaying())
            {
                Matrix4x4 cameraToWorldMatrix = mainCamera.cameraToWorldMatrix;
                cameraToWorldMatrix = cameraOffsetM * cameraToWorldMatrix;
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 2.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
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

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (webCamTextureToMatHelper != null)
                webCamTextureToMatHelper.Dispose();

            if (imageOptimizationHelper != null)
                imageOptimizationHelper.Dispose();

            if (cascade != null)
                cascade.Dispose();
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
    }
}
