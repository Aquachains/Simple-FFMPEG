using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Simple_FFMPEG
{
    public partial class MainWindow : MetroWindow
    {
        public ObservableCollection<Item> Items { get; set; } = [];

        private const string START_ALL = "Start all";
        private const string STOP_ALL = "Stop all";
        private const string CONFIG_FILENAME = "config.json";
        private readonly string _configFilename = Path.Combine(Environment.CurrentDirectory, CONFIG_FILENAME);
        private readonly string CMD_Format = "-stats -progress pipe:1 -i \"{0}\" {1} {2} -c:v libx264 -preset {3} -tune {4} -map 0 \"{0}_output{5}\"";
        private readonly BackgroundWorker _worker;

        public MainWindow()
        {
            InitializeComponent();
            tbCmd.Text = CMD_Format;

            var tet = btnOperationAll;

            LoadConfig();
            dgItems.ItemsSource = Items;

            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = true;
        }

        private void LoadConfig()
        {
            if (File.Exists(_configFilename))
            {
                Items.Clear();

                var data = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(_configFilename));
                if (data != null)
                {
                    foreach (var d in data)
                    {
                        Items.Add(d);
                    }
                }
            }
        }

        private void SaveConfig()
        {
            var data = Items.Where(i => i.Status == String.Empty).ToList();

            if (data.Count > 0)
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_configFilename, json);
            }
            else
            {
                File.WriteAllText(_configFilename, String.Empty);
            }
        }

        private Item GetNextItem()
        {
            return Items.FirstOrDefault(i => String.IsNullOrWhiteSpace(i.Status));
        }

        private void UpdateStartBtn()
        {
            var item = GetNextItem();

            if (item == null)
            {
                btnOperationAll.Content = START_ALL;
                btnOperationAll.IsEnabled = false;
            }
            else
            {
                btnOperationAll.IsEnabled = true;
                btnOperationAll.Content = _worker.IsBusy ? STOP_ALL : START_ALL;
            }
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            UpdateStartBtn();
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            var item = GetNextItem();
            Debug.WriteLine($"File: {item?.Filename ?? "empty"} | Progress: {e.ProgressPercentage}");

            if (item != null && item.Status != "DONE")
            {
                item.Progress = e.ProgressPercentage;

                if (item.Progress >= 100)
                {
                    item.Status = "DONE";
                }
            }
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            // Do all jobs in sequence
            while (GetNextItem() != null)
            {
                if (_worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                var item = GetNextItem();

                // First get frame count of the video with ffprobe.exe
                var ffprobeFrameCount = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffprobe",
                        Arguments = $"-select_streams v -show_streams \"{item.Filename}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                var output = String.Empty;
                const string nbFramesProperty = "nb_frames=";
                try
                {
                    ffprobeFrameCount.Start();
                    while (!ffprobeFrameCount.StandardOutput.EndOfStream)
                    {
                        output = ffprobeFrameCount.StandardOutput.ReadLine();
                        if (output.Contains(nbFramesProperty))
                        {
                            // If nothing is found or its "N/A" the progress cant show the progress (MaxFrames = -1)
                            item.MaxFrames = output.Contains("N/A") ? -1 : Convert.ToInt32(output.Replace(nbFramesProperty, String.Empty));
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    item.MaxFrames = -1;

                    Console.WriteLine(ex.Message);
                }

                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg.exe",
                        Arguments = String.Format(item.CMD, item.Filename, item.DoAudio ? "" : "", item.DoSubtitles ? "" : "", item.Preset, item.Tune, new FileInfo(item.Filename).Extension),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = false
                    }
                };

                try
                {
                    const string FrameProperty = "frame=";
                    ffmpegProcess.Start();
                    while (!ffmpegProcess.StandardOutput.EndOfStream)
                    {
                        if (_worker.CancellationPending)
                        {
                            item.Frames = 0;
                            _worker.ReportProgress(0);
                            e.Cancel = true;

                            if (!ffmpegProcess.HasExited)
                            {
                                ffmpegProcess.Kill();
                            }

                            return;
                        }

                        output = ffmpegProcess.StandardOutput.ReadLine();
                        if (output.Contains(FrameProperty))
                        {
                            item.Frames = Convert.ToInt32(output.Replace(FrameProperty, String.Empty));

                            _worker.ReportProgress(item.MaxFrames == -1 ? -1 : (int)(((float)item.Frames / item.MaxFrames) * 100f));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!ffmpegProcess.HasExited)
                    {
                        ffmpegProcess.Kill();
                    }

                    item.Frames = 0;
                    _worker.ReportProgress(0);

                    Console.WriteLine(ex.Message);
                }
            }
        }

        private bool IsFileAlreadyQueued()
        {
            return Items.Any(i => i.Filename == tbFilename.Text);
        }

        private void TbFilename_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            btnAdd.IsEnabled = File.Exists(tbFilename.Text);

            if (IsFileAlreadyQueued())
            {
                btnAdd.IsEnabled = false;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (IsFileAlreadyQueued())
            {
                btnAdd.IsEnabled = false;
                return;
            }

            Items.Add(new Item(tbFilename.Text, cbPreset.Text, cbTune.Text, checkAudio.IsChecked.Value, checkSubtitles.IsChecked.Value, checkUseGPU.IsChecked.Value, checkUseGPU.IsChecked.Value ? cbHardwareAcceleration.Text : String.Empty, 0, String.Empty, tbCmd.Text));
            tbCmd.Text = CMD_Format;
            UpdateStartBtn();
        }

        /// <summary>
        /// Allow dropping files to the window to easily add a filepath.
        /// </summary>
        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                tbFilename.Text = files[0];
            }
        }

        private void BtnOperationAll_Click(object sender, RoutedEventArgs e)
        {
            if (btnOperationAll.Content.ToString() == START_ALL)
            {
                _worker.RunWorkerAsync();
            }
            else if (btnOperationAll.Content.ToString() == STOP_ALL)
            {
                _worker.CancelAsync();
            }

            UpdateStartBtn();
        }

        private void CheckUseGPU_Click(object sender, RoutedEventArgs e)
        {
            cbHardwareAcceleration.IsEnabled = checkUseGPU.IsChecked.Value;
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            SaveConfig();
        }

        private async void BtnCMD_Click(object sender, RoutedEventArgs e)
        {
            // Open Input-Dialog for handling custom ffmpeg command options.
            var task = await this.ShowInputAsync("Custom command [only for advanced users!]", "Set your custom command for ffmpeg:", new MetroDialogSettings() { DefaultText = CMD_Format });

            if (!String.IsNullOrWhiteSpace(task))
            {
                tbCmd.Text = task;
            }
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            string file = (sender as Button).Tag.ToString();

            if (!String.IsNullOrWhiteSpace(file))
            {
                Items.Remove(Items.Where(i => i.Name == file).First());
            }
        }

        private void OpenUpGithubSite(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Aquachains/Simple-FFMPEG",
                UseShellExecute = true
            });
        }
    }
}