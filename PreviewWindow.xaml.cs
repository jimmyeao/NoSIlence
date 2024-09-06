using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NAudio.Wave;
using System.IO;
using System.Threading.Tasks;

namespace NoSilence
{
    public partial class PreviewWindow : Window
    {
        private string filePath;
        private float[] audioSamples;
        private string ffmpegPath;
        private int zoomLevel = 1;
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFileReader;

        public PreviewWindow(string filePath, string ffmpegPath)
        {
            InitializeComponent();
            this.filePath = filePath;
            this.ffmpegPath = ffmpegPath; // Store the FFmpeg path
            LoadWaveform();
        }

        private async void LoadWaveform()
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File does not exist. Please check the path.");
                return;
            }

            try
            {
                await Task.Run(() => LoadAudioData());
                DrawWaveform();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading waveform: {ex.Message}");
            }
        }

        private void LoadAudioData()
        {
            using (var reader = new AudioFileReader(filePath))
            {
                var samples = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int read;
                var totalSamples = new MemoryStream();

                while ((read = reader.Read(samples, 0, samples.Length)) > 0)
                {
                    var byteBuffer = new byte[read * sizeof(float)];
                    Buffer.BlockCopy(samples, 0, byteBuffer, 0, byteBuffer.Length);
                    totalSamples.Write(byteBuffer, 0, byteBuffer.Length);
                }

                audioSamples = new float[totalSamples.Length / sizeof(float)];
                totalSamples.Position = 0;
                var byteBufferAll = totalSamples.ToArray();
                Buffer.BlockCopy(byteBufferAll, 0, audioSamples, 0, byteBufferAll.Length);
            }
        }

        private void DrawWaveform()
        {
            WaveformCanvas.Children.Clear();
            double midY = WaveformCanvas.ActualHeight / 2;
            double width = WaveformCanvas.ActualWidth;
            int samplesPerPixel = (audioSamples.Length / zoomLevel) / (int)width;
            if (samplesPerPixel == 0) samplesPerPixel = 1;

            for (int x = 0; x < width; x++)
            {
                int start = x * samplesPerPixel;
                int end = (x + 1) * samplesPerPixel;
                if (end >= audioSamples.Length) break;

                float min = float.MaxValue;
                float max = float.MinValue;
                for (int n = start; n < end; n++)
                {
                    var val = audioSamples[n];
                    if (val < min) min = val;
                    if (val > max) max = val;
                }

                var line = new Line
                {
                    X1 = x,
                    X2 = x,
                    Y1 = midY - (min * midY),
                    Y2 = midY - (max * midY),
                    Stroke = Brushes.LightBlue
                };

                WaveformCanvas.Children.Add(line);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                audioFileReader = new AudioFileReader(filePath);
                outputDevice.Init(audioFileReader);
            }
            outputDevice.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            outputDevice?.Pause();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            outputDevice?.Stop();
            if (audioFileReader != null)
            {
                audioFileReader.Position = 0; // Reset position
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            zoomLevel = Math.Min(zoomLevel + 1, 10);
            DrawWaveform();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomLevel = Math.Max(zoomLevel - 1, 1);
            DrawWaveform();
        }

        protected override void OnClosed(EventArgs e)
        {
            outputDevice?.Dispose();
            audioFileReader?.Dispose();
            base.OnClosed(e);
        }
    }
}
