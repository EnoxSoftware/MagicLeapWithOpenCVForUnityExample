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

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace MagicLeap
{
    /// <summary>
    /// This class handles visualization of the video and the UI with the status
    /// of the recording.
    /// </summary>
    public class OpenCVRawVideoCaptureVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The renderer to show the video capture on")]
        private Renderer _screenRenderer = null;

        //[Header("Visuals")]
        //[SerializeField, Tooltip("Object that will show up when recording")]
        //private GameObject _recordingIndicator = null;

        #pragma warning disable 414
        //[SerializeField, Tooltip("Posterization levels of the frame processor")]
        //private byte _posterizationLevels = 4;

        private Texture2D _rawVideoTexture = null;
#pragma warning restore 414

        //Mat yBufferMat = null;
        //Mat uBufferMat = null;
        //Mat vBufferMat = null;

        //Mat uBufferResizedMat = null;
        //Mat vBufferResizedMat = null;

        Mat yuvMat = null;
        Mat rgbMat = null;


        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The faces.
        /// </summary>
        MatOfRect faces;

        /// <summary>
        /// LBP_CASCADE_FILENAME
        /// </summary>
        protected static readonly string LBP_CASCADE_FILENAME = "lbpcascade_frontalface.xml";


        /// <summary>
        /// Check for all required variables to be initialized.
        /// </summary>
        void Start()
        {
            if(_screenRenderer == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._screenRenderer is not set, disabling script.");
                enabled = false;
                return;
            }

            //if (_recordingIndicator == null)
            //{
            //    Debug.LogError("Error: RawVideoCaptureVisualizer._recordingIndicator is not set, disabling script.");
            //    enabled = false;
            //    return;
            //}

            _screenRenderer.enabled = true;



            cascade = new CascadeClassifier();
            cascade.load(Utils.getFilePath(LBP_CASCADE_FILENAME));
            //            cascade.load (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
            if (cascade.empty())
            {
                Debug.LogError("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
        }

        /// <summary>
        /// Handles video capture being started.
        /// </summary>
        public void OnCaptureStarted()
        {
            // Manage canvas visuals
            _screenRenderer.enabled = true;
            //_recordingIndicator.SetActive(true);
        }

        /// <summary>
        /// Handles video capture ending.
        /// </summary>
        public void OnCaptureEnded()
        {
            // Manage canvas visuals
            //_recordingIndicator.SetActive(false);
        }

#if PLATFORM_LUMIN
        /// <summary>
        /// Display the raw video frame on the texture object.
        /// </summary>
        /// <param name="extras">Unused.</param>
        /// <param name="frameData">Contains raw frame bytes to manipulate.</param>
        /// <param name="frameMetadata">Unused.</param>
        public void OnRawCaptureDataReceived(MLCamera.ResultExtras extras, MLCamera.YUVFrameInfo frameData, MLCamera.FrameMetadata frameMetadata)
        {
            //MLCamera.YUVBuffer yBuffer = frameData.Y;

            //if (_rawVideoTexture == null)
            //{
            //    _rawVideoTexture = new Texture2D((int)yBuffer.Stride, (int)yBuffer.Height, TextureFormat.R8, false);
            //    _rawVideoTexture.filterMode = FilterMode.Point;
            //    _screenRenderer.material.mainTexture = _rawVideoTexture;
            //    _screenRenderer.material.mainTextureScale = new Vector2(yBuffer.Width / (float)yBuffer.Stride, -1.0f);
            //}

            //ProcessImage(yBuffer.Data, _posterizationLevels);
            //_rawVideoTexture.LoadRawTextureData(yBuffer.Data);
            //_rawVideoTexture.Apply();


            Utils.setDebugMode(true);

            ////only yBuffer
            //MLCamera.YUVBuffer yBuffer = frameData.Y;

            //if (yBufferMat == null) yBufferMat = new Mat((int)yBuffer.Height, (int)yBuffer.Stride, CvType.CV_8UC1);
            ////MatUtils.copyToMat<byte>(yBuffer.Data, frameMat);
            //yBufferMat.put(0, 0, yBuffer.Data);

            //Imgproc.putText(yBufferMat, "W:" + yBufferMat.width() + " H:" + yBufferMat.height() + " SO:" + Screen.orientation, new Point(5, yBufferMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            //if (_rawVideoTexture == null)
            //{
            //    _rawVideoTexture = new Texture2D((int)yBuffer.Stride, (int)yBuffer.Height, TextureFormat.R8, false);
            //    _rawVideoTexture.filterMode = FilterMode.Point;
            //    _screenRenderer.material.mainTexture = _rawVideoTexture;
            //    _screenRenderer.material.mainTextureScale = new Vector2(yBuffer.Width / (float)yBuffer.Stride, -1.0f);
            //}

            //Utils.fastMatToTexture2D(yBufferMat, _rawVideoTexture, false);


            ////YUV -> RGB
            //MLCamera.YUVBuffer yBuffer = frameData.Y;
            //MLCamera.YUVBuffer uBuffer = frameData.U;
            //MLCamera.YUVBuffer vBuffer = frameData.V;

            //if (yBufferMat == null) yBufferMat = new Mat((int)yBuffer.Height, (int)yBuffer.Stride, CvType.CV_8UC1);
            ////MatUtils.copyToMat<byte>(yBuffer.Data, frameMat);
            //yBufferMat.put(0, 0, yBuffer.Data);
            ////Debug.LogError("yBufferMat.ToString() " + yBufferMat.ToString());

            //if (uBufferMat == null) uBufferMat = new Mat((int)uBuffer.Height, (int)uBuffer.Stride, CvType.CV_8UC1);
            ////MatUtils.copyToMat<byte>(yBuffer.Data, frameMat);
            //uBufferMat.put(0, 0, uBuffer.Data);
            ////Debug.LogError("uBufferMat.ToString() " + uBufferMat.ToString());
            //if (uBufferResizedMat == null) uBufferResizedMat = new Mat();
            //Imgproc.resize(uBufferMat, uBufferResizedMat, new Size(yBuffer.Stride, yBuffer.Height), 0, 0, Imgproc.INTER_NEAREST);
            ////Debug.LogError("uBufferResizedMat.ToString() " + uBufferResizedMat.ToString());

            //if (vBufferMat == null) vBufferMat = new Mat((int)vBuffer.Height, (int)vBuffer.Stride, CvType.CV_8UC1);
            ////MatUtils.copyToMat<byte>(yBuffer.Data, frameMat);
            //vBufferMat.put(0, 0, vBuffer.Data);
            ////Debug.LogError("vBufferMat.ToString() " + vBufferMat.ToString());
            //if (vBufferResizedMat == null) vBufferResizedMat = new Mat();
            //Imgproc.resize(vBufferMat, vBufferResizedMat, new Size(yBuffer.Stride, yBuffer.Height), 0, 0, Imgproc.INTER_NEAREST);
            ////Debug.LogError("vBufferResizedMat.ToString() " + vBufferResizedMat.ToString());

            //List<Mat> mv = new List<Mat>();
            //mv.Add(yBufferMat);
            ////mv.Add(uBufferMat);
            ////mv.Add(vBufferMat);
            //mv.Add(uBufferResizedMat);
            //mv.Add(vBufferResizedMat);

            //if (yuvMat == null) yuvMat = new Mat();
            //OpenCVForUnity.CoreModule.Core.merge(mv, yuvMat);
            ////Debug.LogError("yuvMat.ToString() " + yuvMat.ToString());

            //if (rgbMat == null) rgbMat = new Mat();

            ////Imgproc.cvtColor(yuvMat, rgbMat, Imgproc.COLOR_YUV420p2RGB);
            //Imgproc.cvtColor(yuvMat, rgbMat, Imgproc.COLOR_YUV2RGB);
            ////Debug.LogError("rgbMat.ToString() " + rgbMat.ToString());

            //Imgproc.putText(rgbMat, "W:" + rgbMat.width() + " H:" + rgbMat.height() + " SO:" + Screen.orientation, new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);


            //if (_rawVideoTexture == null)
            //{
            //    _rawVideoTexture = new Texture2D((int)yBuffer.Stride, (int)yBuffer.Height, TextureFormat.RGB24, false);
            //    _rawVideoTexture.filterMode = FilterMode.Point;
            //    _screenRenderer.material.mainTexture = _rawVideoTexture;
            //    _screenRenderer.material.mainTextureScale = new Vector2(yBuffer.Width / (float)yBuffer.Stride, -1.0f);
            //}

            //Utils.fastMatToTexture2D(rgbMat, _rawVideoTexture,false);



            //YUV420 -> RGB
            MLCamera.YUVBuffer yBuffer = frameData.Y;
            MLCamera.YUVBuffer uBuffer = frameData.U;
            MLCamera.YUVBuffer vBuffer = frameData.V;

            int width = (int)yBuffer.Stride;
            int height = (int)yBuffer.Height;

            if (yuvMat == null) yuvMat = new Mat(height + height / 2, width, CvType.CV_8UC1);
            yuvMat.put(0, 0, yBuffer.Data);
            yuvMat.put(height, 0, uBuffer.Data);
            yuvMat.put(height + height / 4, 0, vBuffer.Data);
            //Debug.LogError("yuvMat.ToString() " + yuvMat.ToString());

            if (rgbMat == null) rgbMat = new Mat();

            Imgproc.cvtColor(yuvMat, rgbMat, Imgproc.COLOR_YUV2RGB_I420);
            //Debug.LogError("rgbMat.ToString() " + rgbMat.ToString());


            ProcessMat(rgbMat);


            Imgproc.putText(rgbMat, "W:" + rgbMat.width() + " H:" + rgbMat.height() + " SO:" + Screen.orientation, new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);


            if (_rawVideoTexture == null)
            {
                _rawVideoTexture = new Texture2D((int)yBuffer.Stride, (int)yBuffer.Height, TextureFormat.RGB24, false);
                _rawVideoTexture.filterMode = FilterMode.Point;
                _screenRenderer.material.mainTexture = _rawVideoTexture;
                _screenRenderer.material.mainTextureScale = new Vector2(yBuffer.Width / (float)yBuffer.Stride, -1.0f);
            }

            Utils.fastMatToTexture2D(rgbMat, _rawVideoTexture, false);

            Utils.setDebugMode(false);
        }
#endif

        /// <summary>
        /// Disables the rendere.
        /// </summary>
        public void OnRawCaptureEnded()
        {
            _screenRenderer.enabled = false;
        }

        ///// <summary>
        ///// Modify `data` by applying a posterization transformation to it.
        ///// </summary>
        ///// <param name="data">The byte array to modify.</param>
        ///// <param name="levels">The threshold levels to split each byte into.</param>
        //public static void ProcessImage(byte[] data, byte levels)
        //{
        //    byte factor = (byte) (byte.MaxValue / levels);
        //    for (int i = 0; i < data.Length; i++)
        //    {
        //        data[i] = (byte) (data[i] / factor * factor);
        //    }
        //}

        private void ProcessMat(Mat frameMat)
        {
            if(grayMat == null)grayMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC1);

            if(faces == null)faces = new MatOfRect();

            Imgproc.cvtColor(frameMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);


            if (cascade != null)
                cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size(grayMat.cols() * 0.2, grayMat.rows() * 0.2), new Size());


            OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
            for (int i = 0; i < rects.Length; i++)
            {
                //              Debug.Log ("detect faces " + rects [i]);

                Imgproc.rectangle(frameMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0), 10);
            }

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (grayMat != null)
                grayMat.Dispose();

            if (faces != null)
                faces.Dispose();

            if (cascade != null)
                cascade.Dispose();
        }
    }
}
