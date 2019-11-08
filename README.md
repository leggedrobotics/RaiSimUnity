# RaisimUnity (NOT COMPLETED) 

![raisimunity gif](https://github.com/leggedrobotics/raisimUnity/blob/master/Images/raisimunity.gif)
 
raisimUnity is a visualizer for raisim based on [Unity](https://unity.com/). It gets the simulation data from raisim server application via TCP/IP.

The project was tested on Ubuntu 18.04 LST.

## How to 

### Prerequisites

- [RaiSimLib](https://github.com/leggedrobotics/raisimLib)
- (optional for developement) Unity Editor >= 2019.2.9f1 (linux version is available on [here](https://forum.unity.com/threads/unity-hub-v-1-6-0-is-now-available.640792/))

- Add the following line in your simulation code: see [Example code](https://github.com/leggedrobotics/raisimUnity/tree/master/Examples/src)
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
- Run your simulation. 
- Run RaiSimUnity

![](https://github.com/leggedrobotics/raisimUnity/blob/master/Images/step1.png)
![](https://github.com/leggedrobotics/raisimUnity/blob/master/Images/step2.png)
![](https://github.com/leggedrobotics/raisimUnity/blob/master/Images/step3.png)
![](https://github.com/leggedrobotics/raisimUnity/blob/master/Images/step4.png)

### Development

Clone this repository with git and [git-lfs](https://git-lfs.github.com/): we use git-lfs for large files such as materials, meshes, texture images etc.

```sh
$ git clone https://github.com/eastskykang/raisimUnity.git
```

Once you cloned source code, get lfs files by 

```sh
$ git lfs pull origin
```

You should see texture JPEG files properly from ```Assets/Resources/texture/cc0/```. 

We strongly recommend to use JetBrain's Rider IDE and Unity Rider Editor package >= 1.1.2 for development. 

- See [Wiki doc](https://github.com/leggedrobotics/raisimUnity/wiki/Unity-with-Rider) for more details.
- See [Wiki doc](https://github.com/leggedrobotics/raisimUnity/wiki/Creating-a-material-from-texture-files) to create new material from texture files.

## Dependencies

3rd party libraries, packages and assets are already included in the project. 
This is just for listing the source.

- [pb_Stl](https://github.com/karl-/pb_Stl)
- C# Collada model 
- [OBJImport](https://assetstore.unity.com/packages/tools/modeling/runtime-obj-importer-49547)
- [SimpleFileBrowser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)

## Default Material List

- Bricks1
- Concrete1
- Fabric1
- Ground1
- Leather1
- Metal1
- Metal2
- Metal3
- Metal4
- PavingStones1
- PavingStones2
- Planks1
- ... see [this](https://github.com/leggedrobotics/raisimUnity/tree/master/Assets/Resources/materials/Resources)