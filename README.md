# RaisimUnity
 
raisimUnity is a visualizer for raisim based on [Unity](https://unity.com/). 
The visualizer gets the simulation data from raisim server application via TCP/IP.

The project was tested on Ubuntu 18.04 LST.

## How to 

### Prerequisites

- [RaiSimLib](https://github.com/leggedrobotics/raisimLib)
- (optional for developement) Unity Editor >= 2019.2.9f1 (linux version is available on [here](https://forum.unity.com/threads/unity-hub-v-1-6-0-is-now-available.640792/))

### Quick start

Clone this repository with git and [git-lfs](https://git-lfs.github.com/): we use git-lfs for large files such as materials, meshes, texture images etc.

```sh
$ git clone https://github.com/eastskykang/raisimUnity.git
```

Once you cloned source code, get lfs files by 

```sh
$ git lfs pull origin
```

You should see texture JPEG files properly from ```Assets/Resources/texture/cc0/```.

### Development

We strongly recommend to use JetBrain's Rider IDE and Unity Rider Editor package >= 1.1.2 for development. 

- See [Wiki doc](https://github.com/eastskykang/raisimUnity/wiki/Unity-with-Rider) for more details.
- See [Wiki doc](https://github.com/eastskykang/raisimUnity/wiki/Creating-a-material-from-texture-files) to create new material from texture files.

