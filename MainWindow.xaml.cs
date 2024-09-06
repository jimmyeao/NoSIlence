using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Serilog; // Assuming Serilog is used for logging

namespace NoSilence
{
    public partial class MainWindow : Window
    {
        private string ffmpegPath;
        private string selectedFilePath;
        private string selectedOutputFolder;
        private int silenceThreshold = 60; // Default value

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Serilog or any other logger here if needed
            Log.Information("Application started.");
            ffmpegPath = ffmpegPathTextBox.Text; // Initialize FFmpeg path from the TextBox
        }

        private void BrowseFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe",
                Title = "Select FFmpeg Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ffmpegPath = openFileDialog.FileName;
                ffmpegPathTextBox.Text = ffmpegPath;
                Log.Information("FFmpeg path set to: {FFmpegPath}", ffmpegPath);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower() == ".mp3")
                    {
                        fileList.Items.Add(file);
                        Log.Information("File added: {FilePath}", file);
                    }
                }
            }
        }

        private void FileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (fileList.SelectedItem != null)
            {
                selectedFilePath = fileList.SelectedItem.ToString();
                Log.Information("File selected: {SelectedFilePath}", selectedFilePath);
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show("Please select an MP3 file to preview.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Pass the FFmpeg path to the PreviewWindow constructor
            PreviewWindow previewWindow = new PreviewWindow(selectedFilePath, ffmpegPath);
            previewWindow.Show();
        }

        private async void RemoveSilence_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                MessageBox.Show("Please specify a valid FFmpeg executable path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (fileList.Items.Count == 0)
            {
                MessageBox.Show("Please select at least one MP3 file to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ask user for output directory
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    return;
                }
                selectedOutputFolder = dialog.SelectedPath;
            }

            processingProgressBar.Maximum = fileList.Items.Count;
            processingProgressBar.Value = 0;

            bool overwriteAll = false;
            foreach (string filePath in fileList.Items)
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File does not exist: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                string outputFilePath = Path.Combine(selectedOutputFolder, Path.GetFileName(filePath));

                currentFileLabel.Content = $"Processing: {Path.GetFileName(filePath)}";

                if (File.Exists(outputFilePath))
                {
                    if (!overwriteAll)
                    {
                        var overwriteDialog = new OverwriteDialog(Path.GetFileName(outputFilePath));
                        overwriteDialog.Owner = this; // Set owner for modal behavior
                        overwriteDialog.ShowDialog();

                        switch (overwriteDialog.Result)
                        {
                            case OverwriteDialogResult.Yes:
                                File.Delete(outputFilePath);
                                break;
                            case OverwriteDialogResult.YesToAll:
                                overwriteAll = true;
                                File.Delete(outputFilePath);
                                break;
                            case OverwriteDialogResult.No:
                                continue;
                            case OverwriteDialogResult.Cancel:
                                return;
                        }
                    }
                    else
                    {
                        File.Delete(outputFilePath);
                    }
                }

                // Run FFmpeg and update progress
                await Task.Run(() => RunFFmpegProcess(filePath, outputFilePath, silenceThreshold));

                processingProgressBar.Value++;
            }

            MessageBox.Show("Processing completed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            currentFileLabel.Content = "Processing: Completed";
        }


        private void RunFFmpegProcess(string inputFilePath, string outputFilePath, int silenceThreshold)
        {
            try
            {
                // Construct the FFmpeg arguments using the silenceThreshold parameter
                string arguments = $"-i \"{inputFilePath}\" -af silenceremove=start_periods=1:start_duration=1:start_threshold=-{silenceThreshold}dB:detection=peak,aformat=dblp,areverse,silenceremove=start_periods=1:start_duration=1:start_threshold=-{silenceThreshold}dB:detection=peak,aformat=dblp,areverse \"{outputFilePath}\"";

                Process ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = ffmpegPath;
                ffmpegProcess.StartInfo.Arguments = $"-fflags +nobuffer -progress pipe:1 -nostats {arguments}";
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;

                ffmpegProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            outputTextBox.AppendText(e.Data + Environment.NewLine);
                            outputTextBox.ScrollToEnd();
                        }));
                    }
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            outputTextBox.AppendText(e.Data + Environment.NewLine);
                            outputTextBox.ScrollToEnd();
                        }));
                    }
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();
                ffmpegProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running FFmpeg: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Error running FFmpeg");
            }
        }




    }
}
