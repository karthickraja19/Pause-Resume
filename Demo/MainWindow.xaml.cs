using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Demo
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private CancellationTokenSource _cancellationTokenSource;
        private long _downloadedBytes = 0;
        private long _totalBytes = 0;
        private string _url = "https://github.com/szalony9szymek/large/releases/download/free/large";
        private string _filePath = @"C:\Users\2270395\Downloads";
        private string _fileName = "downloadedFile.pdf";
        private string _destinationPath;
        private double _downloadProgress;
        private string _downloadPercentage;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; 

            _destinationPath = Path.Combine(_filePath, _fileName);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set
            {
                _downloadProgress = value;
                OnPropertyChanged(nameof(DownloadProgress));
            }
        }

        public string DownloadPercentage
        {
            get => _downloadPercentage;
            set
            {
                _downloadPercentage = value;
                OnPropertyChanged(nameof(DownloadPercentage));
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void Download_ClickButtonAsync(object sender, RoutedEventArgs e)
        {
            ResetDownloadProgress();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await DownloadFileAsync(_url, _destinationPath, _cancellationTokenSource.Token);
                MessageBox.Show("File downloaded successfully!");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Download paused!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel(); 
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await DownloadFileAsync(_url, _destinationPath, _cancellationTokenSource.Token);
                MessageBox.Show("File downloaded successfully!");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Download paused!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task DownloadFileAsync(string url, string filePath, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                
                if (File.Exists(filePath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    _downloadedBytes = fileInfo.Length; 
                }

                
                if (_downloadedBytes > 0)
                {
                    client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(_downloadedBytes, null);
                }

                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    _totalBytes = _downloadedBytes + response.Content.Headers.ContentLength.GetValueOrDefault();
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                        fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            _downloadedBytes += bytesRead;

                            UpdateProgress(_downloadedBytes, _totalBytes);
                        }
                    }
                }
            }
        }

        private void UpdateProgress(long downloadedBytes, long totalBytes)
        {
            double progress = (double)downloadedBytes / totalBytes * 100;

            if (progress > 100)
                progress = 100;
            Application.Current.Dispatcher.Invoke(() =>
            {
                DownloadProgress = progress;
                DownloadPercentage = $"{progress:0.00}%";
            });
        }

        private void ResetDownloadProgress()
        {
            DownloadProgress = 0;
            DownloadPercentage = "0.00%";
        }
    }
}