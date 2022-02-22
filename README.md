# MagicLeap With OpenCVForUnity Example


## Environment
* MagicLeapOne Lumin OS 0.98.30
* Lumin SDK 0.26
* Unity 2020.3.29f1 (64-bit)  
* [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088?aid=1011l4ehR) 2.4.7+ 


## Setup
1. Download the latest release unitypackage. [MagicLeapWithOpenCVForUnityExample.unitypackage](https://github.com/EnoxSoftware/MagicLeapWithOpenCVForUnityExample/releases)
1. Create a new project. (MagicLeapWithOpenCVForUnityExample) and [setup UnityProject](https://developer.magicleap.com/en-us/learn/guides/import-the-magic-leap-unity-package).
    * Import the Magic Leap SDK for Unity asset from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/magic-leap-sdk-for-unity-194780).
    ![magicleap_sdk_for_unity.png](magicleap_sdk_for_unity.png)
    * Setup MagicLeap PROJECT SETUP TOOL.
    ![project_setup_tool.png](project_setup_tool.png)
    * Copy [the "MagicLeap" folder](https://github.com/magicleap/MagicLeapUnityExamples/tree/main/Assets) to your project.
1. Import the OpenCVForUnity.
    * Setup the OpenCVForUnity. (Tools > OpenCV for Unity > Set Plugin Import Settings)
    * Move the "OpenCVForUnity/StreamingAssets/" folder to the "Assets/" folder.
    * Downlod https://raw.githubusercontent.com/pjreddie/darknet/master/cfg/yolov3-tiny.cfg. Copy yolov3-tiny.cfg to "Assets/StreamingAssets/dnn/" folder. Downlod  https://pjreddie.com/media/files/yolov3-tiny.weights. Copy yolov3-tiny.weights to "Assets/StreamingAssets/dnn/" folder. Downlod  https://github.com/pjreddie/darknet/tree/master/data/coco.names. Copy coco.names to "Assets/StreamingAssets/dnn/" folder.     
1. Import the MagicLeapWithOpenCVForUnityExample.unitypackage.
   ![setup.png](setup.png)
1. Add the "Assets/MagicLeapWithOpenCVForUnityExample/*.unity" files to the "Scenes In Build" list in the "Build Settings" window.
1. Check CameraCapture and ComputerVision checkbox in Publishing Settings.
   ![manifest_settings.png](manifest_settings.png)
1. Build and Deploy to MagicLeap.


## ScreenShot
![dnn_imagecapture_example.jpg](dnn_imagecapture_example.jpg) 
![facedetection_rawvideocapture_example.jpg](facedetection_rawvideocapture_example.jpg)


