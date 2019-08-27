using MagicLeapWithOpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.XR.MagicLeap;

namespace MagicLeapWithOpenCVForUnityExample
{
    /// <summary>
    /// MagicLeapDnn Example
    /// </summary>
    [RequireComponent(typeof(MLCameraPreviewToMatHelper), typeof(ImageOptimizationHelper))]
    public class MagicLeapDnnExample : MonoBehaviour
    {

        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model;

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config;

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes;

        [TooltipAttribute("Optional list of classes to label detected objects.")]
        public List<string> classesList;

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold;

        [TooltipAttribute("Preprocess input image by multiplying on a scale factor.")]
        public float scale;

        [TooltipAttribute("Preprocess input image by subtracting mean values. Mean values should be in BGR order and delimited by spaces.")]
        public Scalar mean;

        [TooltipAttribute("Indicate that model works with RGB input images instead BGR ones.")]
        public bool swapRB;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight;

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
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        List<string> classNames;
        List<string> outBlobNames;
        List<string> outBlobTypes;

        string classes_filepath;
        string config_filepath;
        string model_filepath;

        List<int> _classIdsList = new List<int>();
        List<float> _confidencesList = new List<float>();
        List<OpenCVForUnity.CoreModule.Rect> _boxesList = new List<OpenCVForUnity.CoreModule.Rect>();

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


            classes_filepath = Utils.getFilePath("dnn/" + classes);
            config_filepath = Utils.getFilePath("dnn/" + config);
            model_filepath = Utils.getFilePath("dnn/" + model);
            Run();

        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (!string.IsNullOrEmpty(classes))
            {
                classNames = readClassNames(classes_filepath);
                if (classNames == null)
                {
                    Debug.LogError(classes_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
                }
            }
            else if (classesList.Count > 0)
            {
                classNames = classesList;
            }

            if (string.IsNullOrEmpty(config_filepath) || string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(config_filepath + " or " + model_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
            else
            {
                //! [Initialize network]
                net = Dnn.readNet(model_filepath, config_filepath);
                //! [Initialize network]


                outBlobNames = getOutputsNames(net);
                //                for (int i = 0; i < outBlobNames.Count; i++) {
                //                    Debug.Log ("names [" + i + "] " + outBlobNames [i]);
                //                }

                outBlobTypes = getOutputsTypes(net);
                //                for (int i = 0; i < outBlobTypes.Count; i++) {
                //                    Debug.Log ("types [" + i + "] " + outBlobTypes [i]);
                //                }
            }


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

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", texture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));
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
                bgrMat = new Mat(downscaleMat.rows(), downscaleMat.cols(), CvType.CV_8UC3);
            }
            else
            {
                bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
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

            if (bgrMat != null)
                bgrMat.Dispose();

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
                        Imgproc.cvtColor(downScaleRgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);
                        
                        
                        StartThread(
                            // action
                            () =>
                            {

                                if (net == null)
                                {

                                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                                }
                                else
                                {


                                    // Create a 4D blob from a frame.
                                    Size inpSize = new Size(inpWidth > 0 ? inpWidth : bgrMat.cols(),
                                                       inpHeight > 0 ? inpHeight : bgrMat.rows());
                                    Mat blob = Dnn.blobFromImage(bgrMat, scale, inpSize, mean, swapRB, false);


                                    // Run a model.
                                    net.setInput(blob);

                                    if (net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
                                    {  // Faster-RCNN or R-FCN
                                        Imgproc.resize(bgrMat, bgrMat, inpSize);
                                        Mat imInfo = new Mat(1, 3, CvType.CV_32FC1);
                                        imInfo.put(0, 0, new float[] {
                                            (float)inpSize.height,
                                            (float)inpSize.width,
                                            1.6f
                                        });
                                        net.setInput(imInfo, "im_info");
                                    }


                                    //TickMeter tm = new TickMeter();
                                    //tm.start();

                                    List<Mat> outs = new List<Mat>();
                                    net.forward(outs, outBlobNames);

                                    //tm.stop();
                                    //Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());


                                    postprocess(bgrMat, outs, net);

                                    for (int i = 0; i < outs.Count; i++)
                                    {
                                        outs[i].Dispose();
                                    }
                                    blob.Dispose();


                                    if (enableDownScale)
                                    {
                                        for (int i = 0; i < _boxesList.Count; ++i)
                                        {
                                            var rect = _boxesList[i];
                                            _boxesList[i] = new OpenCVForUnity.CoreModule.Rect(
                                                (int)(rect.x * DOWNSCALE_RATIO),
                                            (int)(rect.y * DOWNSCALE_RATIO),
                                            (int)(rect.width * DOWNSCALE_RATIO),
                                            (int)(rect.height * DOWNSCALE_RATIO));
                                        }

                                    }
                                }

                            },
                            // done
                            () => {

                                // hide camera image.
                                //Imgproc.rectangle(rgbaMat, new Point(0, 0), new Point(rgbaMat.width(), rgbaMat.height()), new Scalar(0, 0, 0, 0), -1);


                                MatOfRect boxes = new MatOfRect();
                                boxes.fromList(_boxesList);

                                MatOfFloat confidences = new MatOfFloat();
                                confidences.fromList(_confidencesList);


                                MatOfInt indices = new MatOfInt();
                                Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);

                                //            Debug.Log ("indices.dump () "+indices.dump ());
                                //            Debug.Log ("indices.ToString () "+indices.ToString());

                                for (int i = 0; i < indices.total(); ++i)
                                {
                                    int idx = (int)indices.get(i, 0)[0];
                                    OpenCVForUnity.CoreModule.Rect box = _boxesList[idx];
                                    drawPred(_classIdsList[idx], _confidencesList[idx], box.x, box.y,
                                        box.x + box.width, box.y + box.height, rgbaMat);
                                }

                                indices.Dispose();
                                boxes.Dispose();
                                confidences.Dispose();


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

            if (net != null)
                net.Dispose();

            Utils.setDebugMode(false);

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
        /// Reads the class names.
        /// </summary>
        /// <returns>The class names.</returns>
        /// <param name="filename">Filename.</param>
        private List<string> readClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }

        /// <summary>
        /// Postprocess the specified frame, outs and net.
        /// </summary>
        /// <param name="frame">Frame.</param>
        /// <param name="outs">Outs.</param>
        /// <param name="net">Net.</param>
        private void postprocess(Mat frame, List<Mat> outs, Net net)
        {
            string outLayerType = outBlobTypes[0];


            List<int> classIdsList = new List<int>();
            List<float> confidencesList = new List<float>();
            List<OpenCVForUnity.CoreModule.Rect> boxesList = new List<OpenCVForUnity.CoreModule.Rect>();
            if (net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
            {  // Faster-RCNN or R-FCN
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {

                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //                    Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {

                        outs[0].get(i, 0, data);

                        float confidence = data[2];

                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);


                            int left = (int)(data[3] * frame.cols());
                            int top = (int)(data[4] * frame.rows());
                            int right = (int)(data[5] * frame.cols());
                            int bottom = (int)(data[6] * frame.rows());
                            int width = right - left + 1;
                            int height = bottom - top + 1;


                            classIdsList.Add((int)(class_id) - 0);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new OpenCVForUnity.CoreModule.Rect(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "DetectionOutput")
            {
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {

                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //                    Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {

                        outs[0].get(i, 0, data);

                        float confidence = data[2];

                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);


                            int left = (int)(data[3] * frame.cols());
                            int top = (int)(data[4] * frame.rows());
                            int right = (int)(data[5] * frame.cols());
                            int bottom = (int)(data[6] * frame.rows());
                            int width = right - left + 1;
                            int height = bottom - top + 1;


                            classIdsList.Add((int)(class_id) - 0);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new OpenCVForUnity.CoreModule.Rect(left, top, width, height));
                        }
                    }
                }
            }
            else if (outLayerType == "Region")
            {
                for (int i = 0; i < outs.Count; ++i)
                {
                    // Network produces output blob with a shape NxC where N is a number of
                    // detected objects and C is a number of classes + 4 where the first 4
                    // numbers are [center_x, center_y, width, height]

                    //                        Debug.Log ("outs[i].ToString() "+outs[i].ToString());

                    float[] positionData = new float[5];
                    float[] confidenceData = new float[outs[i].cols() - 5];

                    for (int p = 0; p < outs[i].rows(); p++)
                    {



                        outs[i].get(p, 0, positionData);

                        outs[i].get(p, 5, confidenceData);

                        int maxIdx = confidenceData.Select((val, idx) => new { V = val, I = idx }).Aggregate((max, working) => (max.V > working.V) ? max : working).I;
                        float confidence = confidenceData[maxIdx];

                        if (confidence > confThreshold)
                        {

                            int centerX = (int)(positionData[0] * frame.cols());
                            int centerY = (int)(positionData[1] * frame.rows());
                            int width = (int)(positionData[2] * frame.cols());
                            int height = (int)(positionData[3] * frame.rows());
                            int left = centerX - width / 2;
                            int top = centerY - height / 2;

                            classIdsList.Add(maxIdx);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new OpenCVForUnity.CoreModule.Rect(left, top, width, height));

                        }
                    }
                }
            }
            else
            {
                Debug.Log("Unknown output layer type: " + outLayerType);
            }

            _classIdsList = new List<int>(classIdsList);
            _confidencesList = new List<float>(confidencesList);
            _boxesList = new List<OpenCVForUnity.CoreModule.Rect>(boxesList);
        }

        /// <summary>
        /// Draws the pred.
        /// </summary>
        /// <param name="classId">Class identifier.</param>
        /// <param name="conf">Conf.</param>
        /// <param name="left">Left.</param>
        /// <param name="top">Top.</param>
        /// <param name="right">Right.</param>
        /// <param name="bottom">Bottom.</param>
        /// <param name="frame">Frame.</param>
        private void drawPred(int classId, float conf, int left, int top, int right, int bottom, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

            string label = conf.ToString();
            if (classNames != null && classNames.Count != 0)
            {
                if (classId < (int)classNames.Count)
                {
                    label = classNames[classId] + ": " + label;
                }
            }

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, 2, baseLine);

            top = Mathf.Max(top, (int)labelSize.height);
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255),2);
        }

        /// <summary>
        /// Gets the outputs names.
        /// </summary>
        /// <returns>The outputs names.</returns>
        /// <param name="net">Net.</param>
        private List<string> getOutputsNames(Net net)
        {
            List<string> names = new List<string>();


            MatOfInt outLayers = net.getUnconnectedOutLayers();
            for (int i = 0; i < outLayers.total(); ++i)
            {
                names.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_name());
            }
            outLayers.Dispose();

            return names;
        }

        /// <summary>
        /// Gets the outputs types.
        /// </summary>
        /// <returns>The outputs types.</returns>
        /// <param name="net">Net.</param>
        private List<string> getOutputsTypes(Net net)
        {
            List<string> types = new List<string>();


            MatOfInt outLayers = net.getUnconnectedOutLayers();
            for (int i = 0; i < outLayers.total(); ++i)
            {
                types.Add(net.getLayer(new DictValue((int)outLayers.get(i, 0)[0])).get_type());
            }
            outLayers.Dispose();

            return types;
        }

    }
}
