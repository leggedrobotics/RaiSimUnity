# RaisimUnity (NOT COMPLETED) 

![raisimunity gif](Images/raisimunity.gif)
 
raisimUnity is a visualizer for raisim based on [Unity](https://unity.com/). It gets the simulation data from raisim server application via TCP/IP.

The project was tested on Ubuntu 18.04 LST.

## How to 

### Dependencies

The following Unity plugins are already included in the project.                
- [SimpleFileBrowser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)

The following Unity package dependencies are imported by [Packages/manifest.json](Packages/manifest.json).
- [UnityMeshImporter](https://github.com/eastskykang/UnityMeshImporter)

The followings are optional dependencies
- [ffmpeg](https://www.ffmpeg.org/) for video recording
    - You can install ffmpeg by 
        ```sh
        $ sudo apt install ffmpeg
        ``` 

### Quickstart with RaiSim

1. Add the following line in your RaiSim simulation code: see [Example code](https://github.com/leggedrobotics/raisimUnity/tree/master/Examples/src)
    ```cpp
      /// launch raisim servear
      raisim::RaisimServer server(&world);
      server.launchServer();
    
      while(1) {
        raisim::MSLEEP(2);
        server.integrateWorldThreadSafe();
      }
    
      server.killServer();
    ```
2. Run your RaiSim simulation. 
3. Run RaiSimUnity application.
![](Images/step1.png)
4. Add your resource directory that contains your mesh, material etc.
![](Images/step2.png)
5. Tap *Connect* button after specify TCP address and port.
![](Images/step3.png)
6. You can change background by *Background* dropdown menu in run time.
![](Images/step4.png)

### Development

1. Clone this repository with git and [git-lfs](https://git-lfs.github.com/): we use git-lfs for large files such as materials, meshes, texture images etc.
```sh
$ git clone https://github.com/eastskykang/raisimUnity.git
```
2. Once you cloned source code, get lfs files by 
```sh
$ git lfs pull origin
```
You should see texture JPEG files properly from ```Assets/Resources/texture/cc0/```. 
3. Open the project by Unity Editor

4. We strongly recommend to use JetBrain's Rider IDE and Unity Rider Editor package >= 1.1.2 for development. 
    - See [Wiki doc](https://github.com/leggedrobotics/raisimUnity/wiki/Unity-with-Rider) for more details.
    - See [Wiki doc](https://github.com/leggedrobotics/raisimUnity/wiki/Creating-a-material-from-texture-files) to create new material from texture files.

## Default Materials

raisimUnity has default materials created from [CC0 textures](https://cc0textures.com/) and [Free PBR Materials](https://freepbr.com/) textures.

See [this](Assets/Resources/materials/Resources)