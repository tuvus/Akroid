### Building the project

1. Download [UnityHub](https://unity.com/download) and install it.
2. Activate or sign in with a personal license, this should not cost any money.
3. Open a terminal and navigate to the directory that you would like to have the project located in and run `git clone https://github.com/tuvus/Akroid.git`.
Alternatively you can go to [github](https://github.com/tuvus/Akroid#) and download the zip file by pressing on the green code button and extracting it.
4. Open the directory as a project in UnityHub. UnityHub will recommend installing the UnityEditor with a version corresponding to the project, install it.
5. After the editor has been installed you can open the project.
6. Pressing the play button will start the project, however the TextMeshPro essentials needs to be downloaded before the text will appear.
To fix this go to `Window -> TextMeshPro -> Import TMP Essential Resources` and confirm. Now the project should run as expected.

### Using Jetbrains Rider IDE on linux
If you have a Jetbrains license or are a student you can use the Rider IDE.

1. Download [Jetbrains Rider](https://www.jetbrains.com/rider/download/).
2. With Akroid open in the unity editor go to `Window -> Package Manager` and click on the Unity Registry tab, then search for and install the JetBrains Rider Editor
3. Go to `Edit -> Preferences -> External Tools`, click on the External Script Editor field and select JetBrains Rider.
If it is not present, you will have to select the Rider executable in your file system usually at Rider/bin/rider.
4. Open a C# file in the Unity Editor, it should open the project in rider. Rider should show some errors with MSBuild, .net and mono.
To fix this we need to download them.
5. Download .net `sudo apt install dotnet8` or go to their [website](https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website) for other distributions.
6. Download mono, instructions are on their [website](https://www.mono-project.com/download/stable/#download-lin).
7. Close Rider and open it again.
If there is still a problem setting up mono and .net with msbuild go to `settings -> Build, Execute, Deployment -> Toolset and Build` and set the MSBuild Version to a higher value.

