using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

using UnityEngine.XR.MagicLeap;

namespace MagicLeapWithOpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// MLCameraPreview to mat helper.
    /// v 1.1.0
    /// </summary>
    [RequireComponent (typeof(PrivilegeRequester))]
    public class MLCameraPreviewToMatHelper : MonoBehaviour
    {
        /// <summary>
        /// Set the name of the camera device to use. (or device index number)
        /// </summary>
        [SerializeField, FormerlySerializedAs ("requestedDeviceName"), TooltipAttribute ("Set the name of the device to use. (or device index number)")]
        protected string _requestedDeviceName = null;

        public virtual string requestedDeviceName {
            get { return _requestedDeviceName; }
            set {
                _requestedDeviceName = value;
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        /// <summary>
        /// Set the width of camera.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("requestedWidth"), TooltipAttribute ("Set the width of camera.")]
        protected int _requestedWidth = 1920;

        public virtual int requestedWidth {
            get { return _requestedWidth; }
            set {
                _requestedWidth = (int)Mathf.Clamp (value, 0f, float.MaxValue);
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        /// <summary>
        /// Set the height of camera.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("requestedHeight"), TooltipAttribute ("Set the height of camera.")]
        protected int _requestedHeight = 1080;

        public virtual int requestedHeight {
            get { return _requestedHeight; }
            set {
                _requestedHeight = (int)Mathf.Clamp (value, 0f, float.MaxValue);
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("requestedIsFrontFacing"), TooltipAttribute ("Set whether to use the front facing camera.")]
        protected bool _requestedIsFrontFacing = false;

        public virtual bool requestedIsFrontFacing {
            get { return _requestedIsFrontFacing; }
            set {
                _requestedIsFrontFacing = value;
                if (hasInitDone) {
                    Initialize (_requestedIsFrontFacing, requestedFPS, rotate90Degree);
                }
            }
        }

        /// <summary>
        /// Set the frame rate of camera.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("requestedFPS"), TooltipAttribute ("Set the frame rate of camera.")]
        protected float _requestedFPS = 30f;

        public virtual float requestedFPS {
            get { return _requestedFPS; }
            set {
                _requestedFPS = Mathf.Clamp (value, -1f, float.MaxValue);
                if (hasInitDone) {
                    bool isPlaying = IsPlaying ();
                    Stop ();
#if UNITY_EDITOR
                    webCamTexture.requestedFPS = _requestedFPS;
#endif
                    if (isPlaying)
                        Play ();
                }
            }
        }

        /// <summary>
        /// Sets whether to rotate camera frame 90 degrees. (clockwise)
        /// </summary>
        [SerializeField, FormerlySerializedAs ("rotate90Degree"), TooltipAttribute ("Sets whether to rotate camera frame 90 degrees. (clockwise)")]
        protected bool _rotate90Degree = false;

        public virtual bool rotate90Degree {
            get { return _rotate90Degree; }
            set {
                _rotate90Degree = value;
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        /// <summary>
        /// Determines if flips vertically.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("flipVertical"), TooltipAttribute ("Determines if flips vertically.")]
        protected bool _flipVertical = false;

        public virtual bool flipVertical {
            get { return _flipVertical; }
            set { _flipVertical = value; }
        }

        /// <summary>
        /// Determines if flips horizontal.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("flipHorizontal"), TooltipAttribute ("Determines if flips horizontal.")]
        protected bool _flipHorizontal = false;

        public virtual bool flipHorizontal {
            get { return _flipHorizontal; }
            set { _flipHorizontal = value; }
        }

        /// <summary>
        /// The number of frames before the initialization process times out.
        /// </summary>
        [SerializeField, FormerlySerializedAs ("timeoutFrameCount"), TooltipAttribute ("The number of frames before the initialization process times out.")]
        protected int _timeoutFrameCount = 300;

        public virtual int timeoutFrameCount {
            get { return _timeoutFrameCount; }
            set { _timeoutFrameCount = (int)Mathf.Clamp (value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// UnityEvent that is triggered when this instance is initialized.
        /// </summary>
        public UnityEvent onInitialized;

        /// <summary>
        /// UnityEvent that is triggered when this instance is disposed.
        /// </summary>
        public UnityEvent onDisposed;

        /// <summary>
        /// UnityEvent that is triggered when this instance is error Occurred.
        /// </summary>
        public ErrorUnityEvent onErrorOccurred;

        #if UNITY_EDITOR
        /// <summary>
        /// The active WebcamTexture.
        /// </summary>
        protected WebCamTexture webCamTexture;
        
        /// <summary>
        /// The active WebcamDevice.
        /// </summary>
        protected WebCamDevice webCamDevice;
        #endif

        /// <summary>
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

        /// <summary>
        /// The rotated frame mat
        /// </summary>
        protected Mat rotatedFrameMat;

        /// <summary>
        /// The buffer colors.
        /// </summary>
        protected Color32[] colors;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        /// <summary>
        /// The initialization coroutine.
        /// </summary>
        protected IEnumerator initCoroutine;

        /// <summary>
        /// The orientation of the screen.
        /// </summary>
        protected ScreenOrientation screenOrientation;

        /// <summary>
        /// The width of the screen.
        /// </summary>
        protected int screenWidth;

        /// <summary>
        /// The height of the screen.
        /// </summary>
        protected int screenHeight;

        /// <summary>
        /// Indicates whether this instance avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
        /// Sets compulsorily the requestedFPS parameter to 15 (only when using the front camera), to avoid the problem of the WebCamTexture image becoming low light.
        /// https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
        /// https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
        /// </summary>
        public bool avoidAndroidFrontCameraLowLightIssue = false;


        #if !UNITY_EDITOR
        private bool _isCameraConnected = false;
        private bool _isCapturing = false;
        private bool _hasStarted = false;
        private bool _doPrivPopup = false;
        private bool _hasShownPrivPopup = false;

        /// <summary>
        /// The example is using threads on the call to MLCamera.CaptureRawImageAsync to alleviate the blocking
        /// call at the beginning of CaptureRawImageAsync, and the safest way to prevent race conditions here is to
        /// lock our access into the MLCamera class, so that we don't accidentally shut down the camera
        /// while the thread is attempting to work
        /// </summary>
        private object _cameraLockObject = new object ();

        private PrivilegeRequester _privilegeRequester;


        private Texture2D texture;
#endif


        [System.Serializable]
        public enum ErrorCode : int
        {
            UNKNOWN = 0,
            CAMERA_DEVICE_NOT_EXIST = 1,
            TIMEOUT = 2,
        }

        [System.Serializable]
        public class ErrorUnityEvent : UnityEngine.Events.UnityEvent<ErrorCode>
        {

        }

        protected virtual void OnValidate ()
        {
            _requestedWidth = (int)Mathf.Clamp (_requestedWidth, 0f, float.MaxValue);
            _requestedHeight = (int)Mathf.Clamp (_requestedHeight, 0f, float.MaxValue);
            _requestedFPS = Mathf.Clamp (_requestedFPS, -1f, float.MaxValue);
            _timeoutFrameCount = (int)Mathf.Clamp (_timeoutFrameCount, 0f, float.MaxValue);
        }

        #if UNITY_EDITOR
        public void Awake ()
        {
            PrivilegeRequester privilegeRequester = GetComponent<PrivilegeRequester> ();
            privilegeRequester.enabled = false;
        }
        #endif

        // Update is called once per frame
        protected virtual void Update ()
        {
#if !UNITY_EDITOR
            if (_doPrivPopup && !_hasShownPrivPopup) {
                Instantiate (Resources.Load ("PrivilegeDeniedError"));
                _doPrivPopup = false;
                _hasShownPrivPopup = true;
            }
#endif


            if (hasInitDone) {
                // Catch the orientation change of the screen and correct the mat image to the correct direction.
                if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height)) {

                    if (onDisposed != null)
                        onDisposed.Invoke ();

                    if (frameMat != null) {
                        frameMat.Dispose ();
                        frameMat = null;
                    }
                    if (rotatedFrameMat != null) {
                        rotatedFrameMat.Dispose ();
                        rotatedFrameMat = null;
                    }

                    frameMat = new Mat (MLCamera.PreviewTextureHeight, MLCamera.PreviewTextureWidth, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));
                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    bool isRotatedFrame = false;
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown)
                    {
                        if (!rotate90Degree)
                            isRotatedFrame = true;
                    }
                    else if (rotate90Degree)
                    {
                        isRotatedFrame = true;
                    }
#else
                    if (rotate90Degree)
                        isRotatedFrame = true;
#endif
                    if (isRotatedFrame)
                        rotatedFrameMat = new Mat (MLCamera.PreviewTextureWidth, MLCamera.PreviewTextureHeight, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));

                    if (onInitialized != null)
                        onInitialized.Invoke ();
                } else {
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy ()
        {
#if !UNITY_EDITOR
            if (_privilegeRequester != null) {
                _privilegeRequester.OnPrivilegesDone -= HandlePrivilegesDone;
            }
#endif

            Dispose ();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Initialize ()
        {
            if (isInitWaiting) {
                CancelInitCoroutine ();
                ReleaseResources ();
            }

            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        public virtual void Initialize (int requestedWidth, int requestedHeight)
        {
            if (isInitWaiting) {
                CancelInitCoroutine ();
                ReleaseResources ();
            }

            this._requestedWidth = requestedWidth;
            this._requestedHeight = requestedHeight;
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        /// <param name="rotate90Degree">If set to <c>true</c> requested to rotate camera frame 90 degrees. (clockwise)</param>
        public virtual void Initialize (bool requestedIsFrontFacing, float requestedFPS = 30f, bool rotate90Degree = false)
        {
            if (isInitWaiting) {
                CancelInitCoroutine ();
                ReleaseResources ();
            }

            _requestedDeviceName = null;
            this._requestedIsFrontFacing = requestedIsFrontFacing;
            this._requestedFPS = requestedFPS;
            this._rotate90Degree = rotate90Degree;
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        /// <param name="rotate90Degree">If set to <c>true</c> requested to rotate camera frame 90 degrees. (clockwise)</param>
        public virtual void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing = false, float requestedFPS = 30f, bool rotate90Degree = false)
        {
            if (isInitWaiting) {
                CancelInitCoroutine ();
                ReleaseResources ();
            }

            this._requestedDeviceName = deviceName;
            this._requestedWidth = requestedWidth;
            this._requestedHeight = requestedHeight;
            this._requestedIsFrontFacing = requestedIsFrontFacing;
            this._requestedFPS = requestedFPS;
            this._rotate90Degree = rotate90Degree;
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize ()
        {
            if (hasInitDone) {
                ReleaseResources ();

                if (onDisposed != null)
                    onDisposed.Invoke ();
            }

            isInitWaiting = true;

#if !UNITY_EDITOR

// If not listed here, the PrivilegeRequester assumes the request for
// the privileges needed, CameraCapture in this case, are in the editor.
_privilegeRequester = GetComponent<PrivilegeRequester> ();

// Before enabling the Camera, the scene must wait until the privilege has been granted.
_privilegeRequester.OnPrivilegesDone += HandlePrivilegesDone;

yield return null;
#else

            float requestedFPS = this.requestedFPS;

            // Creates the camera
            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse (requestedDeviceName, out requestedDeviceIndex)) {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < WebCamTexture.devices.Length) {
                        webCamDevice = WebCamTexture.devices [requestedDeviceIndex];

                        if (avoidAndroidFrontCameraLowLightIssue && webCamDevice.isFrontFacing == true)
                            requestedFPS = 15f;

                        if (requestedFPS < 0) {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                        } else {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                        }
                    }
                } else {
                    for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                        if (WebCamTexture.devices [cameraIndex].name == requestedDeviceName) {
                            webCamDevice = WebCamTexture.devices [cameraIndex];

                            if (avoidAndroidFrontCameraLowLightIssue && webCamDevice.isFrontFacing == true)
                                requestedFPS = 15f;

                            if (requestedFPS < 0) {
                                webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                            } else {
                                webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                            }
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log ("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null) {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {
                        webCamDevice = WebCamTexture.devices [cameraIndex];

                        if (avoidAndroidFrontCameraLowLightIssue && webCamDevice.isFrontFacing == true)
                            requestedFPS = 15f;

                        if (requestedFPS < 0) {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                        } else {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                        }
                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];

                    if (avoidAndroidFrontCameraLowLightIssue && webCamDevice.isFrontFacing == true)
                        requestedFPS = 15f;

                    if (requestedFPS < 0) {
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                    } else {
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, (int)requestedFPS);
                    }
                } else {
                    isInitWaiting = false;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                    yield break;
                }
            }

            // Starts the camera
            webCamTexture.Play ();

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true) {
                if (initFrameCount > timeoutFrameCount) {
                    isTimeout = true;
                    break;
                }
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                else if (webCamTexture.width > 16 && webCamTexture.height > 16) {
#else
                else if (webCamTexture.didUpdateThisFrame) {
#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2
                    while (webCamTexture.width <= 16) {
                        if (initFrameCount > timeoutFrameCount) {
                            isTimeout = true;
                            break;
                        }else {
                            initFrameCount++;
                        }
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    }
                    if (isTimeout) break;
#endif
#endif

                    Debug.Log ("WebCamTextureToMatHelper:: " + "devicename:" + webCamTexture.deviceName + " name:" + webCamTexture.name + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS
                    + " videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                        colors = new Color32[webCamTexture.width * webCamTexture.height];

                    frameMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    bool isRotatedFrame = false;
#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown)
                    {
                        if (!rotate90Degree)
                            isRotatedFrame = true;
                    }
                    else if (rotate90Degree)
                    {
                        isRotatedFrame = true;
                    }
#else
                    if (rotate90Degree)
                        isRotatedFrame = true;
#endif
                    if (isRotatedFrame)
                        rotatedFrameMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke ();

                    break;
                } else {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout) {
                webCamTexture.Stop ();
                webCamTexture = null;
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke (ErrorCode.TIMEOUT);
            }

#endif

        }

        #if !UNITY_EDITOR
        
        /// <summary>
        /// Connects the MLCamera component and instantiates a new instance
        /// if it was never created.
        /// </summary>
        private void EnableMLCamera ()
        {
            lock (_cameraLockObject) {
                MLResult result = MLCamera.Start ();
                if (result.IsOk) {
                    result = MLCamera.Connect ();
                    _isCameraConnected = true;
                } else {
                    if (result.Code == MLResultCode.PrivilegeDenied) {
                        Instantiate (Resources.Load ("PrivilegeDeniedError"));
                    }

                    Debug.LogErrorFormat ("Error: ImageCaptureExample failed starting MLCamera, disabling script. Reason: {0}", result);
                    enabled = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Disconnects the MLCamera if it was ever created or connected.
        /// </summary>
        private void DisableMLCamera ()
        {
            lock (_cameraLockObject) {
                if (MLCamera.IsStarted) {
                    MLCamera.Disconnect ();
                    // Explicitly set to false here as the disconnect was attempted.
                    _isCameraConnected = false;
                    MLCamera.Stop ();
                }
            }
        }

        /// <summary>
        /// Once privileges have been granted, enable the camera and callbacks.
        /// </summary>
        private void StartCapture ()
        {
            if (!_hasStarted) {
                lock (_cameraLockObject) {
                    EnableMLCamera ();
                }

                _hasStarted = true;


                MLCamera.StartPreview ();


                texture = new Texture2D (MLCamera.PreviewTextureWidth, MLCamera.PreviewTextureHeight, TextureFormat.RGBA32, false);


                Debug.Log ("WebCamTextureToMatHelper:: " + " width:" + MLCamera.PreviewTextureWidth + " height:" + MLCamera.PreviewTextureHeight);
                
                if (colors == null || colors.Length != MLCamera.PreviewTextureWidth * MLCamera.PreviewTextureHeight)
                    colors = new Color32[MLCamera.PreviewTextureWidth * MLCamera.PreviewTextureHeight];
                
                frameMat = new Mat (MLCamera.PreviewTextureHeight, MLCamera.PreviewTextureWidth, CvType.CV_8UC4);
                screenOrientation = Screen.orientation;
                screenWidth = Screen.width;
                screenHeight = Screen.height;
                
                bool isRotatedFrame = false;

                if (rotate90Degree)
                    isRotatedFrame = true;

                if (isRotatedFrame)
                    rotatedFrameMat = new Mat (MLCamera.PreviewTextureWidth, MLCamera.PreviewTextureHeight, CvType.CV_8UC4);
                
                isInitWaiting = false;
                hasInitDone = true;
                initCoroutine = null;
                
                if (onInitialized != null)
                    onInitialized.Invoke ();
            }
        }

        /// <summary>
        /// Responds to privilege requester result.
        /// </summary>
        /// <param name="result"/>
        private void HandlePrivilegesDone (MLResult result)
        {
            if (!result.IsOk) {
                if (result.Code == MLResultCode.PrivilegeDenied) {
                    Instantiate (Resources.Load ("PrivilegeDeniedError"));
                }

                Debug.LogErrorFormat ("Error: ImageCaptureExample failed to get requested privileges, disabling script. Reason: {0}", result);
                enabled = false;
                return;
            }

            Debug.Log ("Succeeded in requesting all privileges");
            StartCapture ();
        }

/// <summary>
/// Cannot make the assumption that a reality privilege is still granted after
/// returning from pause. Return the application to the state where it
/// requests privileges needed and clear out the list of already granted
/// privileges. Also, disable the camera and unregister callbacks.
/// </summary>
void OnApplicationPause (bool pause)
{
if (pause) {
lock (_cameraLockObject) {
if (_isCameraConnected) {
_isCapturing = false;
DisableMLCamera ();
}
}

_hasStarted = false;
}
}
#endif

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized ()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public virtual void Play ()
        {
#if !UNITY_EDITOR
            if (hasInitDone)
                MLCamera.StartPreview ();
#else
            if (hasInitDone)
                webCamTexture.Play ();
#endif
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public virtual void Pause ()
        {
#if !UNITY_EDITOR
            if (hasInitDone)
                MLCamera.StopPreview ();
#else
            if (hasInitDone)
                webCamTexture.Pause ();
#endif
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public virtual void Stop ()
        {
#if !UNITY_EDITOR
            if (hasInitDone)
                MLCamera.StopPreview ();
#else
            if (hasInitDone)
                webCamTexture.Stop ();
#endif
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying ()
        {
#if !UNITY_EDITOR
//return hasInitDone ? MLCamera.Previewing : false;
return hasInitDone;
#else
            return hasInitDone ? webCamTexture.isPlaying : false;
#endif
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public virtual bool IsFrontFacing ()
        {
#if !UNITY_EDITOR
            return false;
#else
            return hasInitDone ? webCamDevice.isFrontFacing : false;
#endif
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public virtual string GetDeviceName ()
        {
#if !UNITY_EDITOR
            return "MagicLeap Camera";
#else
            return hasInitDone ? webCamTexture.deviceName : "";
#endif
        }

        /// <summary>
        /// Returns the active camera width.
        /// </summary>
        /// <returns>The active camera width.</returns>
        public virtual int GetWidth ()
        {
            if (!hasInitDone)
                return -1;
            return (rotatedFrameMat != null) ? frameMat.height () : frameMat.width ();
        }

        /// <summary>
        /// Returns the active camera height.
        /// </summary>
        /// <returns>The active camera height.</returns>
        public virtual int GetHeight ()
        {
            if (!hasInitDone)
                return -1;
            return (rotatedFrameMat != null) ? frameMat.width () : frameMat.height ();
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public virtual float GetFPS ()
        {
#if !UNITY_EDITOR
return -1f;
#else
            return hasInitDone ? webCamTexture.requestedFPS : -1f;
#endif


        }

        #if UNITY_EDITOR
        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public virtual WebCamTexture GetWebCamTexture ()
        {
            return hasInitDone ? webCamTexture : null;
        
        }

        /// <summary>
        /// Returns the active WebcamDevice.
        /// </summary>
        /// <returns>The active WebcamDevice.</returns>
        public virtual WebCamDevice GetWebCamDevice ()
        {
            return webCamDevice;
        }
        #endif

        /// <summary>
        /// Returns the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public virtual Matrix4x4 GetCameraToWorldMatrix ()
        {
            return Camera.main.cameraToWorldMatrix;
        }

        /// <summary>
        /// Returns the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public virtual Matrix4x4 GetProjectionMatrix ()
        {
            return Camera.main.projectionMatrix;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame ()
        {
            if (!hasInitDone)
                return false;

#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
            if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                return true;
            } else {
                return false;
            }
#else
#if !UNITY_EDITOR
            return true;
#else
            return webCamTexture.didUpdateThisFrame;
#endif
#endif
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' (RGBA).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat ()
        {

#if !UNITY_EDITOR
//          if (!hasInitDone || !MLCamera.Previewing) {
            if (!hasInitDone) {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            //Because MLCamera.PreviewTexture2D is not readable, you can not get pixels directly. So, Utils.textureToTexture2D () is used.
            //Utils.texture2DToMat (MLCamera.PreviewTexture2D, frameMat);
            Utils.textureToTexture2D (MLCamera.PreviewTexture2D, texture);
            //Graphics.CopyTexture(MLCamera.PreviewTexture2D, texture);
            
            Utils.texture2DToMat (texture, frameMat);

//            int numChannels = 4;
//            Utils.copyToMat<byte>(MagicLeapInternal.MLTextureUtils.ConvertToByteArray(MLCamera.PreviewTexture2D, out numChannels), frameMat);

return frameMat;
#else

            if (!hasInitDone || !webCamTexture.isPlaying) {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            Utils.webCamTextureToMat (webCamTexture, frameMat, colors, false);

#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
            if (rotatedFrameMat != null)
            {
                if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    // (Orientation is Portrait, rotate90Degree is false)
                    if (webCamDevice.isFrontFacing)
                    {
                        FlipMat (frameMat, !flipHorizontal, !flipVertical);
                    }
                    else
                    {
                        FlipMat (frameMat, flipHorizontal, flipVertical);
                    }
                }
                else
                {
                    // (Orientation is Landscape, rotate90Degrees=true)
                    FlipMat (frameMat, flipVertical, flipHorizontal);
                }
                Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            }
            else
            {
                if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown)
                {
                    // (Orientation is Portrait, rotate90Degree is ture)
                    if (webCamDevice.isFrontFacing)
                    {
                        FlipMat (frameMat, flipHorizontal, flipVertical);
                    }
                    else
                    {
                        FlipMat (frameMat, !flipHorizontal, !flipVertical);
                    }
                }
                else
                {
            // (Orientation is Landscape, rotate90Degree is false)
            FlipMat (frameMat, flipVertical, flipHorizontal);
                }
            return frameMat;
            }
#else
            FlipMat (frameMat, flipVertical, flipHorizontal);
            if (rotatedFrameMat != null) {                
                Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            } else {
                return frameMat;
            }
#endif

#endif
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected virtual void FlipMat (Mat mat, bool flipVertical, bool flipHorizontal)
        {
            //Since the order of pixels of WebCamTexture and Mat is opposite, the initial value of flipCode is set to 0 (flipVertical).
            int flipCode = 0;

#if UNITY_EDITOR
            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 90) {
                    flipCode = -1;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = int.MinValue;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = int.MinValue;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = 1;
                }
            }
#endif

            if (flipVertical) {
                if (flipCode == int.MinValue) {
                    flipCode = 0;
                } else if (flipCode == 0) {
                    flipCode = int.MinValue;
                } else if (flipCode == 1) {
                    flipCode = -1;
                } else if (flipCode == -1) {
                    flipCode = 1;
                }
            }

            if (flipHorizontal) {
                if (flipCode == int.MinValue) {
                    flipCode = 1;
                } else if (flipCode == 0) {
                    flipCode = -1;
                } else if (flipCode == 1) {
                    flipCode = int.MinValue;
                } else if (flipCode == -1) {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue) {
                Core.flip (mat, mat, flipCode);
            }
        }

        /// <summary>
        /// Gets the buffer colors.
        /// </summary>
        /// <returns>The buffer colors.</returns>
        public virtual Color32[] GetBufferColors ()
        {
            return colors;
        }

        /// <summary>
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine ()
        {
            if (initCoroutine != null) {
                StopCoroutine (initCoroutine);
                ((IDisposable)initCoroutine).Dispose ();
                initCoroutine = null;
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected virtual void ReleaseResources ()
        {
            isInitWaiting = false;
            hasInitDone = false;

#if !UNITY_EDITOR
//            MLInput.OnControllerButtonDown -= OnButtonDown;
lock (_cameraLockObject) {
if (_isCameraConnected) {
//                    MLCamera.OnRawImageAvailable -= OnCaptureRawImageComplete;
_isCapturing = false;
DisableMLCamera ();
}
}

#else
            if (webCamTexture != null) {
                webCamTexture.Stop ();
                WebCamTexture.Destroy (webCamTexture);
                webCamTexture = null;
            }
#endif

            if (frameMat != null) {
                frameMat.Dispose ();
                frameMat = null;
            }
            if (rotatedFrameMat != null) {
                rotatedFrameMat.Dispose ();
                rotatedFrameMat = null;
            }
        }

        /// <summary>
        /// Releases all resource used by the <see cref="WebCamTextureToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="WebCamTextureToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="WebCamTextureToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="WebCamTextureToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="WebCamTextureToMatHelper"/> was occupying.</remarks>
        public virtual void Dispose ()
        {
            if (colors != null)
                colors = null;

            if (isInitWaiting) {
                CancelInitCoroutine ();
                ReleaseResources ();
            } else if (hasInitDone) {
                ReleaseResources ();

                if (onDisposed != null)
                    onDisposed.Invoke ();
            }
        }
            
            
    }
}
