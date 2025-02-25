using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Simple_FFMPEG
{
    public class Item : INotifyPropertyChanged
    {
        private string filename;
        public string Filename { get => filename; set { filename = value; OnPropertyChanged(); } }
        private string name;
        public string Name { get => name; set { name = value; OnPropertyChanged(); } }
        private string preset;
        public string Preset { get => preset; set { preset = value; OnPropertyChanged(); } }
        private string tune;
        public string Tune { get => tune; set { tune = value; OnPropertyChanged(); } }
        private bool doAudio;
        public bool DoAudio { get => doAudio; set { doAudio = value; OnPropertyChanged(); } }
        private bool doSubtitles;
        public bool DoSubtitles { get => doSubtitles; set { doSubtitles = value; OnPropertyChanged(); } }
        private bool useGPU;
        public bool UseGPU { get => useGPU; set { useGPU = value; OnPropertyChanged(); } }
        private string gPU_Config;
        public string GPU_Config { get => gPU_Config; set { gPU_Config = value; OnPropertyChanged(); } }
        private int progress;
        public int Progress { get => progress; set { progress = value; OnPropertyChanged(); } }
        private string status;
        public string Status { get => status; set { status = value; OnPropertyChanged(); } }
        private string cMD;
        public string CMD { get => cMD; set { cMD = value; OnPropertyChanged(); } }
        private int maxFrames;
        public int MaxFrames { get => maxFrames; set { maxFrames = value; OnPropertyChanged(); } }
        private int frames;
        public int Frames { get => frames; set { frames = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public Item(string filename, string preset, string tune, bool doAudio, bool doSubtitles, bool useGPU, string gpu_config, int progress, string status, string cmd)
        {
            Filename = filename;
            Name = new FileInfo(filename).Name;
            Preset = preset;
            Tune = tune;
            DoAudio = doAudio;
            DoSubtitles = doSubtitles;
            UseGPU = useGPU;
            GPU_Config = gpu_config;
            Progress = progress;
            Status = status;
            CMD = cmd;
            MaxFrames = 0;
            Frames = 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
