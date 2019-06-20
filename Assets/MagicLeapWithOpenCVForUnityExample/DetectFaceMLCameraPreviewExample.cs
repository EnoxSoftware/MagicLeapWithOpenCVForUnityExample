using MagicLeapWithOpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MagicLeapWithOpenCVForUnityExample
{
    /// <summary>
    /// DetectFace MLCameraPreview Example
    /// </summary>
    [RequireComponent(typeof(MLCameraPreviewToMatHelper), typeof(ImageOptimizationHelper))]
    public class DetectFaceMLCameraPreviewExample : MonoBehaviour
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
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

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
        /// tokenSource
        /// </summary>
        CancellationTokenSource tokenSource = new CancellationTokenSource();


        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

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

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            //            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            //            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("deviceName", webCamTextureToMatHelper.GetDeviceName().ToString());
                fpsMonitor.Add("width", webCamTextureToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTextureToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("downscaleRaito", imageOptimizationHelper.downscaleRatio.ToString());
                fpsMonitor.Add("frameSkippingRatio", imageOptimizationHelper.frameSkippingRatio.ToString());
                fpsMonitor.Add("downscale_width", downscaleMat.width().ToString());
                fpsMonitor.Add("downscale_height", downscaleMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            //            float width = webCamTextureMat.width ();
            //            float height = webCamTextureMat.height ();
            //                                    
            //            float widthScale = (float)Screen.width / width;
            //            float heightScale = (float)Screen.height / height;
            //            if (widthScale < heightScale) {
            //                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            //            } else {
            //                Camera.main.orthographicSize = height / 2;
            //            }


            rgbaMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC4);
            grayMat = new Mat(downscaleMat.rows(), downscaleMat.cols(), CvType.CV_8UC1);

            //Main Process
            Process();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");


            // Cancel Task
            tokenSource.Cancel();


            if (rgbaMat != null)
                rgbaMat.Dispose();

            if (grayMat != null)
                grayMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

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



        /// <summary>
        /// Precess
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
                // Debug.Log ("rgbaMat.ToString() " + rgbaMat.ToString ());

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
                Imgproc.cvtColor(downScaleRgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                await Task.Run(() =>
                {
                    // detect faces on the downscale image
                    if (!enableSkipFrame || !imageOptimizationHelper.IsCurrentFrameSkipped())
                    {

                        using (Mat equalizeHistMat = new Mat())
                        using (MatOfRect faces = new MatOfRect())
                        {
                            Imgproc.equalizeHist(grayMat, equalizeHistMat);

                            cascade.detectMultiScale(equalizeHistMat, faces, 1.1f, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE, new Size(equalizeHistMat.cols() * 0.15, equalizeHistMat.cols() * 0.15), new Size());


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


                        //Imgproc.rectangle(rgbaMat, new Point(0, 0), new Point(rgbaMat.width(), rgbaMat.height()), new Scalar(0, 0, 0, 0), -1);

                        for (int i = 0; i < detectResult.Length; i++)
                        {

                            Imgproc.rectangle(rgbaMat, new Point(detectResult[i].x, detectResult[i].y), new Point(detectResult[i].x + detectResult[i].width, detectResult[i].y + detectResult[i].height), new Scalar(255, 0, 0, 255), 4);
                        }
                    }
                });

                

                Utils.fastMatToTexture2D(rgbaMat, texture);


                Thread.Sleep(10);


            }

        }

    }
}
