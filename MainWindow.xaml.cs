using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Serilog; // Assuming Serilog is used for logging

namespace NoSilence
{
    public partial class MainWindow : Window
    {
        private string ffmpegPath;
        private string selectedFilePath;
        private string outputDirectory;

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

            if (fileList.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one MP3 file to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Prompt for output directory
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                outputDirectory = folderDialog.SelectedPath;
            }
            else
            {
                MessageBox.Show("Please select a directory to save the processed files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            foreach (string filePath in fileList.SelectedItems)
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File does not exist: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                string outputFilePath = overwriteCheckBox.IsChecked == true
                    ? filePath
                    : Path.Combine(outputDirectory, "trimmed_" + Path.GetFileName(filePath));

                // Run FFmpeg in a separate task
                await RunFFmpegAsync(filePath, outputFilePath);
            }
        }

        private async Task RunFFmpegAsync(string inputFilePath, string outputFilePath)
        {
            // Corrected FFmpeg command with proper formatting
            string arguments = $"-i \"{inputFilePath}\" -af \"silenceremove=start_periods=1:start_duration=0.5:start_threshold=-60dB:detection=peak,aformat=dblp,areverse,silenceremove=start_periods=1:start_duration=0.5:start_threshold=-60dB:detection=peak,aformat=dblp,areverse\" \"{outputFilePath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var process = new Process { StartInfo = startInfo })
                {
                    process.OutputDataReceived += (sender, e) => Dispatcher.Invoke(() =>
                    {
                        if (e.Data != null) outputTextBox.AppendText(e.Data + Environment.NewLine);
                    });

                    process.ErrorDataReceived += (sender, e) => Dispatcher.Invoke(() =>
                    {
                        if (e.Data != null) outputTextBox.AppendText(e.Data + Environment.NewLine);
                    });

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Error removing silence from {inputFilePath}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                    }
                    else
                    {
                        Dispatcher.Invoke(() => MessageBox.Show($"Silence removed successfully from {inputFilePath}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error running FFmpeg process.");
                Dispatcher.Invoke(() => MessageBox.Show($"An error occurred while processing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }


        private void FileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (fileList.SelectedItem != null)
            {
                selectedFilePath = fileList.SelectedItem.ToString();
            }
        }
    }
}
