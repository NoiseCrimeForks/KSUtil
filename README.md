# KSUtil
Kinect Studio Utility tool - shows basic usage of the Kinect tooling APIs.

# NoiseCrime Notes
The documentation for using this tool at [docs.microsoft.com](https://docs.microsoft.com/en-us/previous-versions/windows/kinect/dn785527(v=ieb.10))is incorrect.
I was unable to get playback working with named streams using the suggested command line arguments

> KSUtil -play <filePath> –loop 2 –stream depth ir body

Instead I had to switch from using '-' to '/' as an argument definition.

> KSUtil /play <filePath> /loop 2 /stream depth ir color

