# raisimUnity

## What is raisimUnity?
 
raisimUnity is a visualizer for raisim based on [Unity engine](https://unity.com/). 
The visualizer get the simulation data from raisim server application via TCP/IP.

Tested on Ubuntu 18.04 LST.

Note. raisimUnity uses 8080 port as a default.

## How to 

### Install 

Please install/save everything locally to prevent corrupting your system files. We will assume that you have a single workspace where you save all repos related to raisim. Here we introduce two variables

- WORKSPACE: workspace where you clone your git repos.
- LOCAL_BUILD: build directory where you install exported cmake libraries.

#### Dependencies

First, install [raisimLib](https://github.com/leggedrobotics/raisimLib).
Then, install unity.

### Development

Here we introduce how the workflow can be integrated with [Unity Editor]() and [JetBrains Rider IDE](https://www.jetbrains.com/rider/).

#### Prerequisites 

- JetBrains Rider (or Visual Studio or VS code; whatever IDE you prefer) 
- Unity Hub and Unity Editor (linux version is available on [here](https://forum.unity.com/threads/unity-hub-v-1-6-0-is-now-available.640792/))

#### Steps

1. Create an Unity Project on Unity Editor
    - you can set your default editor on Edit > Preference > External Tools > External Script Editor
    - see [this](https://answers.unity.com/questions/1240640/how-do-i-change-the-default-script-editor.html)
2. Create C# Script in the Assets directory
3. Create 'root' GameObject by ```GameObject > Create Empty``` 
4. Add your C# script on the 'root' object.
5. Edit your C# script by open the script. (will automatically redirect to Rider)
6. Write your code, run and debug. 

#### Debug 

1. run debug on Rider by pushing debug button. 
2. start application by pushing start button on Rider.
 