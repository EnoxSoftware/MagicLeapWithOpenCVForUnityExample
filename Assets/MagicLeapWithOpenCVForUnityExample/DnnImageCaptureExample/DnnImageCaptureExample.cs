// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;
using System.Collections.Generic;
using System.Threading;
using MagicLeap.Core.StarterKit;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.DnnModule;
using System.Linq;
using OpenCVForUnity.UtilsModule;

namespace MagicLeap
{
    public class DnnImageCaptureExample : MonoBehaviour
    {
        [SerializeField, Space, Tooltip("MLControllerConnectionHandlerBehavior reference.")]
        private MLControllerConnectionHandlerBehavior _controllerConnectionHandler = null;

        [SerializeField, Tooltip("The text used to display status information for the example.")]
        private Text _statusText = null;

        [SerializeField, Tooltip("Object to set new images on.")]
        private GameObject _previewObject = null;

        private bool _isCameraConnected = false;

        private bool _isCapturing = false;

        private bool _hasStarted = false;

        private bool _privilegesBeingRequested = false;

        private Thread _captureThread = null;

        /// <summary>
        /// The example is using threads on the call to MLCamera.CaptureRawImageAsync to alleviate the blocking
        /// call at the beginning of CaptureRawImageAsync, and the safest way to prevent race conditions here is to
        /// lock our access into the MLCamera class, so that we don't accidentally shut down the camera
        /// while the thread is attempting to work
        /// </summary>
        private object _cameraLockObject = new object();


        [TooltipAttribute("Path to input image.")]
        public string input;

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



        //yolov3
        //        string input = "004545.jpg";
        //        public string input = "person.jpg";
        //        public string model = "yolov3-tiny.weights";
        //        public string config = "yolov3-tiny.cfg";
        //        //        string model = "yolov2-tiny.weights";
        //        //        string config = "yolov2-tiny.cfg";
        //        public string classes = "coco.names";
        //
        //
        //        public float confThreshold = 0.24f;
        //        public float nmsThreshold = 0.24f;
        //        public float scale = 1f / 255f;
        //        public Scalar mean = new Scalar (0, 0, 0);
        //        public bool swapRB = false;
        //        public int inpWidth = 416;
        //        public int inpHeight = 416;
        //
        //        List<string> classNames;

        //                //MobileNetSSD
        //                string input = "004545.jpg";
        //        //        string input = "person.jpg";
        //                string model = "MobileNetSSD_deploy.caffemodel";
        //                string config = "MobileNetSSD_deploy.prototxt";
        //                string classes;
        //        //        string classes = "coco.names";
        //
        //                float confThreshold = 0.2f;
        //                float nmsThreshold = 0.2f;
        //                float scale = 2f / 255f;
        //                Scalar mean = new Scalar (127.5, 127.5, 127.5);
        //                bool swapRB = false;
        //                int inpWidth = 300;
        //                int inpHeight = 300;
        //
        //                List<string> classNames = new List<string>(new string[]{"background",
        //                    "aeroplane", "bicycle", "bird", "boat",
        //                    "bottle", "bus", "car", "cat", "chair",
        //                    "cow", "diningtable", "dog", "horse",
        //                    "motorbike", "person", "pottedplant",
        //                    "sheep", "sofa", "train", "tvmonitor"
        //                });
        //        //        List<string> classNames;

        //        //ResnetSSDFaceDetection
        //        string input = "grace_hopper_227.png";
        //        //        string input = "person.jpg";
        //        string model = "res10_300x300_ssd_iter_140000.caffemodel";
        //        string config = "deploy.prototxt";
        //        //        string model = "yolov2-tiny.weights";
        //        //        string config = "yolov2-tiny.cfg";
        //        string classes;
        //
        //
        //        float confThreshold = 0.5f;
        //        float nmsThreshold = 0.5f;
        //        float scale = 1f;
        //        Scalar mean = new Scalar (104, 177, 123);
        //        bool swapRB = false;
        //        int inpWidth = 300;
        //        int inpHeight = 300;
        //
        //        List<string> classNames;


        List<string> classNames;
        List<string> outBlobNames;
        List<string> outBlobTypes;

        string classes_filepath;
        string input_filepath;
        string config_filepath;
        string model_filepath;


        /// <summary>
        /// Using Awake so that Privileges is set before PrivilegeRequester Start.
        /// </summary>
        void Awake()
        {
            if (_controllerConnectionHandler == null)
            {
                Debug.LogError("Error: ImageCaptureExample._controllerConnectionHandler is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_statusText == null)
            {
                Debug.LogError("Error: ImageCaptureExample._statusText is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_previewObject == null)
            {
                Debug.LogError("Error: ImageCaptureExample._previewObject is not set, disabling script.");
                enabled = false;
                return;
            }

            // This is made active when we have a captured image to show.
            _previewObject.SetActive(false);

            // Before enabling the Camera, the scene must wait until the privilege has been granted.
            MLResult result = MLPrivilegesStarterKit.Start();
            #if PLATFORM_LUMIN
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: ImageCaptureExample failed starting MLPrivilegesStarterKit, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }
            #endif

            result = MLPrivilegesStarterKit.RequestPrivilegesAsync(HandlePrivilegesDone, MLPrivileges.Id.CameraCapture);
            #if PLATFORM_LUMIN
            if (!result.IsOk)
            {
                Debug.LogErrorFormat("Error: ImageCaptureExample failed requesting privileges, disabling script. Reason: {0}", result);
                MLPrivilegesStarterKit.Stop();
                enabled = false;
                return;
            }
            #endif

            _privilegesBeingRequested = true;


            classes_filepath = Utils.getFilePath("dnn/" + classes);
            input_filepath = Utils.getFilePath("dnn/" + input);
            config_filepath = Utils.getFilePath("dnn/" + config);
            model_filepath = Utils.getFilePath("dnn/" + model);
        }

        /// <summary>
        /// Stop the camera, unregister callbacks, and stop input and privileges APIs.
        /// </summary>
        void OnDisable()
        {
            #if PLATFORM_LUMIN
            MLInput.OnControllerButtonDown -= OnButtonDown;
            #endif

            lock (_cameraLockObject)
            {
                if (_isCameraConnected)
                {
                    #if PLATFORM_LUMIN
                    MLCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;
                    #endif

                    _isCapturing = false;
                    DisableMLCamera();
                }
            }
        }

        /// <summary>
        /// Cannot make the assumption that a reality privilege is still granted after
        /// returning from pause. Return the application to the state where it
        /// requests privileges needed and clear out the list of already granted
        /// privileges. Also, disable the camera and unregister callbacks.
        /// </summary>
        void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                lock (_cameraLockObject)
                {
                    if (_isCameraConnected)
                    {
                        #if PLATFORM_LUMIN
                        MLCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;
                        #endif

                        _isCapturing = false;

                        DisableMLCamera();
                    }
                }

                #if PLATFORM_LUMIN
                MLInput.OnControllerButtonDown -= OnButtonDown;
                #endif

                _hasStarted = false;
            }
        }

        /// <summary>
        /// Cleans up the component.
        /// </summary>
        void OnDestroy()
        {
            if (_privilegesBeingRequested)
            {
                _privilegesBeingRequested = false;

                MLPrivilegesStarterKit.Stop();
            }
        }

        /// <summary>
        /// Display privilege error if necessary or update status text.
        /// </summary>
        private void Update()
        {
            UpdateStatusText();
        }

        /// <summary>
        /// Updates examples status text.
        /// </summary>
        private void UpdateStatusText()
        {
            _statusText.text = string.Format("<color=#dbfb76><b>Controller Data</b></color>\nStatus: {0}\n", ControllerStatus.Text);
        }

        /// <summary>
        /// Captures a still image using the device's camera and returns
        /// the data path where it is saved.
        /// </summary>
        /// <param name="fileName">The name of the file to be saved to.</param>
        public void TriggerAsyncCapture()
        {
            if (_captureThread == null || (!_captureThread.IsAlive))
            {
                ThreadStart captureThreadStart = new ThreadStart(CaptureThreadWorker);
                _captureThread = new Thread(captureThreadStart);
                _captureThread.Start();
            }
            else
            {
                Debug.Log("Previous thread has not finished, unable to begin a new capture just yet.");
            }
        }

        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private void EnableMLCamera()
        {
            #if PLATFORM_LUMIN
            lock (_cameraLockObject)
            {
                MLResult result = MLCamera.Start();
                if (result.IsOk)
                {
                    result = MLCamera.Connect();
                    _isCameraConnected = true;
                }
                else
                {
                    Debug.LogErrorFormat("Error: ImageCaptureExample failed starting MLCamera, disabling script. Reason: {0}", result);
                    enabled = false;
                    return;
                }
            }
            #endif
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// </summary>
        private void DisableMLCamera()
        {
            #if PLATFORM_LUMIN
            lock (_cameraLockObject)
            {
                if (MLCamera.IsStarted)
                {
                    MLCamera.Disconnect();
                    // Explicitly set to false here as the disconnect was attempted.
                    _isCameraConnected = false;
                    MLCamera.Stop();
                }
            }
            #endif
        }

        /// <summary>
        /// Once privileges have been granted, enable the camera and callbacks.
        /// </summary>
        private void StartCapture()
        {
            if (!_hasStarted)
            {
                lock (_cameraLockObject)
                {
                    EnableMLCamera();

                    #if PLATFORM_LUMIN
                    MLCamera.OnRawImageAvailable += OnCaptureRawImageComplete;
                    #endif
                }

                #if PLATFORM_LUMIN
                MLInput.OnControllerButtonDown += OnButtonDown;
                #endif

                _hasStarted = true;
            }
        }

        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        private void HandlePrivilegesDone(MLResult result)
        {
            _privilegesBeingRequested = false;
            MLPrivilegesStarterKit.Stop();

            #if PLATFORM_LUMIN
            if (result != MLResult.Code.PrivilegeGranted)
            {
                Debug.LogErrorFormat("Error: ImageCaptureExample failed to get requested privileges, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }
            #endif

            Debug.Log("Succeeded in requesting all privileges");
            StartCapture();
        }

        /// <summary>
        /// Handles the event for button down.
        /// </summary>
        /// <param name="controllerId">The id of the controller.</param>
        /// <param name="button">The button that is being pressed.</param>
        private void OnButtonDown(byte controllerId, MLInput.Controller.Button button)
        {
            if (_controllerConnectionHandler.IsControllerValid(controllerId) && MLInput.Controller.Button.Bumper == button && !_isCapturing)
            {
                TriggerAsyncCapture();
            }
        }

        /// <summary>
        /// Handles the event of a new image getting captured.
        /// </summary>
        /// <param name="imageData">The raw data of the image.</param>
        private void OnCaptureRawImageComplete(byte[] imageData)
        {
            lock (_cameraLockObject)
            {
                _isCapturing = false;
            }
            //// Initialize to 8x8 texture so there is no discrepency
            //// between uninitalized captures and error texture
            //Texture2D texture = new Texture2D(8, 8);
            //bool status = texture.LoadImage(imageData);

            //if (status && (texture.width != 8 && texture.height != 8))
            //{
            //    _previewObject.SetActive(true);
            //    Renderer renderer = _previewObject.GetComponent<Renderer>();
            //    if (renderer != null)
            //    {
            //        renderer.material.mainTexture = texture;
            //    }
            //}

            Mat buff = new Mat(1, imageData.Length, CvType.CV_8UC1);
            MatUtils.copyToMat<byte>(imageData, buff);

            Mat imgMat = Imgcodecs.imdecode(buff, Imgcodecs.IMREAD_COLOR);
            //            Debug.Log ("imgMat.ToString() " + imgMat.ToString ());
            buff.Dispose();

            Run(imgMat);

            Imgproc.cvtColor(imgMat, imgMat, Imgproc.COLOR_BGR2RGB);

            Texture2D outputTexture = new Texture2D(imgMat.width(), imgMat.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(imgMat, outputTexture);

            imgMat.Dispose();

            //if (status && (texture.width != 8 && texture.height != 8))
            //{
                _previewObject.SetActive(true);
                Renderer renderer = _previewObject.GetComponent<Renderer>();
                if(renderer != null)
                {
                    renderer.material.mainTexture = outputTexture;
                }
            //}
        }

        /// <summary>
        /// Worker function to call the API's Capture function
        /// </summary>
        private void CaptureThreadWorker()
        {
            #if PLATFORM_LUMIN
            lock (_cameraLockObject)
            {
                if (MLCamera.IsStarted && _isCameraConnected)
                {
                    MLResult result = MLCamera.CaptureRawImageAsync();
                    if (result.IsOk)
                    {
                        _isCapturing = true;
                    }
                }
            }
            #endif
        }


        // Use this for initialization
        void Run(Mat img)
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (!string.IsNullOrEmpty(classes))
            {
                classNames = readClassNames(classes_filepath);
#if !UNITY_WSA_10_0
                if (classNames == null)
                {
                    Debug.LogError(classes_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
                }
#endif
            }
            else if (classesList.Count > 0)
            {
                classNames = classesList;
            }



            Net net = null;

            if (string.IsNullOrEmpty(config_filepath) || string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(config_filepath + " or " + model_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
            else
            {
                //! [Initialize network]
                net = Dnn.readNet(model_filepath, config_filepath);
                //! [Initialize network]

            }


            if (net == null)
            {

                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);

            }
            else
            {

                outBlobNames = getOutputsNames(net);
                //                for (int i = 0; i < outBlobNames.Count; i++) {
                //                    Debug.Log ("names [" + i + "] " + outBlobNames [i]);
                //                }

                outBlobTypes = getOutputsTypes(net);
                //                for (int i = 0; i < outBlobTypes.Count; i++) {
                //                    Debug.Log ("types [" + i + "] " + outBlobTypes [i]);
                //                }


                // Create a 4D blob from a frame.
                Size inpSize = new Size(inpWidth > 0 ? inpWidth : img.cols(),
                    inpHeight > 0 ? inpHeight : img.rows());
                Mat blob = Dnn.blobFromImage(img, scale, inpSize, mean, swapRB, false);


                // Run a model.
                net.setInput(blob);

                if (net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
                {  // Faster-RCNN or R-FCN
                    Imgproc.resize(img, img, inpSize);
                    Mat imInfo = new Mat(1, 3, CvType.CV_32FC1);
                    imInfo.put(0, 0, new float[] {
                        (float)inpSize.height,
                        (float)inpSize.width,
                        1.6f
                    });
                    net.setInput(imInfo, "im_info");
                }


                TickMeter tm = new TickMeter();
                tm.start();


                List<Mat> outs = new List<Mat>();
                net.forward(outs, outBlobNames);


                tm.stop();
                Debug.Log("Inference time, ms: " + tm.getTimeMilli());


                postprocess(img, outs, net);

                for (int i = 0; i < outs.Count; i++)
                {
                    outs[i].Dispose();
                }
                blob.Dispose();
                net.Dispose();

            }


            Utils.setDebugMode(false);
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
        protected virtual void postprocess(Mat frame, List<Mat> outs, Net net)
        {
            string outLayerType = outBlobTypes[0];


            List<int> classIdsList = new List<int>();
            List<float> confidencesList = new List<float>();
            List<Rect2d> boxesList = new List<Rect2d>();
            if (net.getLayer(new DictValue(0)).outputNameToIndex("im_info") != -1)
            {
                // Faster-RCNN or R-FCN
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1)
                {

                    outs[0] = outs[0].reshape(1, (int)outs[0].total() / 7);

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {

                        outs[0].get(i, 0, data);

                        float confidence = data[2];

                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 0);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
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

                    //Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs[0].rows(); i++)
                    {

                        outs[0].get(i, 0, data);

                        float confidence = data[2];

                        if (confidence > confThreshold)
                        {
                            int class_id = (int)(data[1]);

                            float left = data[3] * frame.cols();
                            float top = data[4] * frame.rows();
                            float right = data[5] * frame.cols();
                            float bottom = data[6] * frame.rows();
                            float width = right - left + 1f;
                            float height = bottom - top + 1f;

                            classIdsList.Add((int)(class_id) - 0);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));
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

                    //Debug.Log ("outs[i].ToString() "+outs[i].ToString());

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

                            float centerX = positionData[0] * frame.cols();
                            float centerY = positionData[1] * frame.rows();
                            float width = positionData[2] * frame.cols();
                            float height = positionData[3] * frame.rows();
                            float left = centerX - width / 2;
                            float top = centerY - height / 2;

                            classIdsList.Add(maxIdx);
                            confidencesList.Add((float)confidence);
                            boxesList.Add(new Rect2d(left, top, width, height));

                        }
                    }
                }
            }
            else
            {
                Debug.Log("Unknown output layer type: " + outLayerType);
            }


            MatOfRect2d boxes = new MatOfRect2d();
            boxes.fromList(boxesList);

            MatOfFloat confidences = new MatOfFloat();
            confidences.fromList(confidencesList);


            MatOfInt indices = new MatOfInt();
            Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);

            //Debug.Log ("indices.dump () "+indices.dump ());
            //Debug.Log ("indices.ToString () "+indices.ToString());

            for (int i = 0; i < indices.total(); ++i)
            {
                int idx = (int)indices.get(i, 0)[0];
                Rect2d box = boxesList[idx];
                drawPred(classIdsList[idx], confidencesList[idx], box.x, box.y,
                    box.x + box.width, box.y + box.height, frame);
            }

            indices.Dispose();
            boxes.Dispose();
            confidences.Dispose();

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
        protected virtual void drawPred(int classId, float conf, double left, double top, double right, double bottom, Mat frame)
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
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            top = Mathf.Max((float)top, (float)labelSize.height);
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), OpenCVForUnity.CoreModule.Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        /// <summary>
        /// Gets the outputs names.
        /// </summary>
        /// <returns>The outputs names.</returns>
        /// <param name="net">Net.</param>
        protected virtual List<string> getOutputsNames(Net net)
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
        protected virtual List<string> getOutputsTypes(Net net)
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
