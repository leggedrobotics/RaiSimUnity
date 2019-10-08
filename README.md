# raisimUnity

## What is raisimUnity?
 
raisimUnity is a visualizer for raisim based on [Unity engine](https://unity.com/). 
The visualizer get the simulation data from raisim server application via TCP/IP.

Tested on Ubuntu 18.04 LST.

Note. raisimUnity uses 8080 port as a default.

## How to 

### Prerequisites

- [RaiSimLib](https://github.com/leggedrobotics/raisimLib)
- (optional for developement) Unity Editor (linux version is available on [here](https://forum.unity.com/threads/unity-hub-v-1-6-0-is-now-available.640792/))

### Install 

Please install/save everything locally to prevent corrupting your system files. We will assume that you have a single workspace where you save all repos related to raisim. Here we introduce two variables

- WORKSPACE: workspace where you clone your git repos.
- LOCAL_BUILD: build directory where you install exported cmake libraries.

### Development

We strongly recommend to use JetBrain's Rider IDE for development. See [Wiki doc](https://github.com/eastskykang/raisimUnity/wiki/Unity-with-Rider) for more details.