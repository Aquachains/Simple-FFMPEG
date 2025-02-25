# Simple-FFMPEG  
A simple Windows app for encoding multiple videos in sequence using FFmpeg.

## Dependencies
- [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [FFmpeg](https://ffmpeg.org/): You need to add the FFmpeg binaries to your system's "Path" (see this guide for example on how to do that: [Environment Variables](https://www.computerhope.com/issues/ch000549.htm)).

## Usage
1. Drag and drop a video file into the window, or input the file path manually. Make sure the file exists.
2. Set configurations like "preset" and "tune." The "veryslow" preset is very slow but provides excellent video quality.
3. Add the job to the list.
4. Click the "Start All" button to begin encoding.

Unfinished jobs will be automatically saved when the program is closed and will be reloaded when restarted.

![Gif for guidance 01](https://github.com/Aquachains/Simple-FFMPEG/blob/main/Simple_FFMPEG_guide01.gif)
*WPF and [MahApps.Metro](https://github.com/MahApps/MahApps.Metro) is used for the GUI.*

## Contribution
If you find any bugs or would like to contribute to the project, feel free to open an issue or submit a pull request. Your contributions are welcome!
