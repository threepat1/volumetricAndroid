## Introduction
    XRCO Volumetric Plugin provides simple and efficient volume video decoding. Developers can use XRCO Volumetric Plugin to perform volume video decoding preview and secondary development in Unity3D.

## Features

* Provide high-performance decoding capabilities for recording, broadcasting and real-time live broadcasting
* Preview in editor mode
* All platforms
* Can be controlled by Timeline

## platforms

* Window
* Mac
* IOS
* Android
* Hololens

## Development environment requirements
    Unity2019.4 and above, unity2020.x and above is recommended

## File Directory
    ├─XRCO//The main part of the plugin
    │  ├─Editor
    │  │      MeshPlayerPluginEditor.cs Editor script, //providing functions such as editor mode preview
    │  │
    │  ├─Plugins //Libraries for each platform
    │  │  ├─arm64-v8a
    │  │  ├─armeabi-a7v
    |  │  ├─ios
    │  │  ├─Mac
    │  │  ├─UWP
    │  │  └─x86_64
    │  │
    │  ├─Prefabs
    │  │  │  XRCO.prefab Prefab for quick use
    │  │  │
    │  │  └─Material
    │  │          Logo.png
    │  │          MatPrometh.mat 
    │  │
    │  ├─Scripts
    │  │      MeshPlayerPlugin.cs //The main logic part of the plug-in C#
    │  │      MeshReader.cs //The middle layer between the C# part and the underlying API, encapsulated into MeshData data
    │  │      ReaderAPI.cs The part that calls the underlying API
    │  │
    │  ├─Scenes
    │  │      Basic.unity //Basic function scene
    │  └─StreamingAssets //First move this folder to the root directory, and place the video file here


## Quick start
First put the XRCO/StreamingAssets folder in the Assets root directory
You can directly run the Demo scene in the Scene to quickly use it, or create a new scene by yourself. First drag XRCOCube into the scene, select the SourceType in the component as PLAYBACK, and fill in the SourcePath in the path under the StreamingAssets folder, remember to add the .mp4 suffix , In the path below StreamingAssets, check the InStreamingAssets property. Under the editor, you can click the PreviewFrame progress bar to preview. SourceDurationSec is the total duration of the current model, and SpeedRatio is the playback speed of the model.

## API
HoloCatchLightPlugin Provide some interfaces to control video playback
* MeshPlayerPlugin.OpenSource(string str)  //Open File
* MeshPlayerPlugin.Play()  // Play
* MeshPlayerPlugin.Pause()  // Pause
* MeshPlayerPlugin.GotoSecond(float sec)  // set GotoSecond
* MeshPlayerPlugin.SpeedRatio //set Speed Ratio

info@thexr.co
