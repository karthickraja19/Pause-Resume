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
        private string _url;
        private string _filePath;
        private double _downloadProgress;
        private string _downloadPercentage;

        
        private static readonly HttpClient client = new HttpClient();

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged(nameof(Url));
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
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
            DownloadButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            ResumeButton.IsEnabled = false;

            string destinationPath = Path.Combine(FilePath, "downloadedFile.pdf");

            try
            {
                await DownloadFileAsync(Url, destinationPath, _cancellationTokenSource.Token);
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
            finally
            {
                DownloadButton.IsEnabled = true;
                PauseButton.IsEnabled = false;
                ResumeButton.IsEnabled = true;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            PauseButton.IsEnabled = false;
            ResumeButton.IsEnabled = true;
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            PauseButton.IsEnabled = true;
            ResumeButton.IsEnabled = false;

            string destinationPath = Path.Combine(FilePath, "downloadedFile.pdf");

            try
            {
                await DownloadFileAsync(Url, destinationPath, _cancellationTokenSource.Token);
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
            finally
            {
                DownloadButton.IsEnabled = false;
                PauseButton.IsEnabled = true;
                ResumeButton.IsEnabled = false;
            }
        }

        private async Task DownloadFileAsync(string url, string filePath, CancellationToken cancellationToken)
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

        
                if (response.Headers.AcceptRanges != null && response.Headers.AcceptRanges.Equals("bytes"))
                {
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                        fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, true))
                    {
                       
                        fileStream.Seek(_downloadedBytes, SeekOrigin.Begin);

                        byte[] buffer = new byte[4096]; // Reduced buffer size for faster resume
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
                else
                {
                    throw new Exception("Server does not support resuming downloads.");
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