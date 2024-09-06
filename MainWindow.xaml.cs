using System;
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

        private void RemoveSilence_Click(object sender, RoutedEventArgs e)
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

            foreach (string filePath in fileList.SelectedItems)
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File does not exist: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                // Logic to call FFmpeg and remove silence
                Log.Information("Starting silence removal for file: {FilePath}", filePath);
                // Call FFmpeg logic
            }
        }
        private void FileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (fileList.SelectedItem != null)
            {
                selectedFilePath = fileList.SelectedItem.ToString();
                Log.Information("File selected for preview: {FilePath}", selectedFilePath);
            }
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Implement save logic
        }
    }
}
