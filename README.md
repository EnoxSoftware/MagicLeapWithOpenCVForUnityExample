# MagicLeap With OpenCVForUnity Example


## Environment
* MagicLeapOne Lumin OS 0.96
* Unity 2019.1.0f2 (64-bit)  
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR) 2.3.5+ 


## Setup
1. Download the latest release unitypackage. [MagicLeapWithOpenCVForUnityExample.unitypackage](https://github.com/EnoxSoftware/MagicLeapWithOpenCVForUnityExample/releases)
1. Create a new project. (MagicLeapWithOpenCVForUnityExample)
1. Import "C:/Users/xxx/MagicLeap/tools/unity/v0.21.0/MagicLeap.unitypackage"
1. Import the OpenCVForUnity.
    * Setup the OpenCVForUnity. (Tools > OpenCV for Unity > Set Plugin Import Settings)
    * Move the "OpenCVForUnity/StreamingAssets/" folder to the "Assets/" folder.
    * Downlod https://raw.githubusercontent.com/pjreddie/darknet/master/cfg/yolov3-tiny.cfg. Copy yolov3-tiny.cfg to "Assets/StreamingAssets/dnn/" folder. Downlod  https://pjreddie.com/media/files/yolov3-tiny.weights. Copy yolov3-tiny.weights to "Assets/StreamingAssets/dnn/" folder. Downlod  https://github.com/pjreddie/darknet/tree/master/data/coco.names. Copy coco.names to "Assets/StreamingAssets/dnn/" folder.     
1. Import the MagicLeapWithOpenCVForUnityExample.unitypackage.
1. Add the "Assets/MagicLeapWithOpenCVForUnityExample/*.unity" files to the "Scenes In Build" list in the "Build Settings" window.
1. Copy "Assets/MagicLeap/Examples/Plugins/Lumin/manifest.xml" to "Assets/Plugins/Lumin/manifest.xml". https://creator.magicleap.com/learn/guides/migrate-unity-editors
1. Build and Deploy to MagicLeap.


## ScreenShot
![magicleap_yoloexample.jpg](magicleap_yoloexample.jpg) 


