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
        private List<DownloadFile> _allFiles = new List<DownloadFile>();
        private List<DownloadFile> _completedFiles = new List<DownloadFile>();
        private List<DownloadFile> _incompleteFiles = new List<DownloadFile>();

        private long _downloadedBytes = 0;
        private long _totalBytes = 0;
        private string _url;
        private string _filePath;
        private double _downloadProgress;
        private string _downloadPercentage;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            FilteredFiles = _allFiles;  
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

        private List<DownloadFile> _filteredFilesList;

        public List<DownloadFile> FilteredFiles
        {
            get => _filteredFilesList;
            set
            {
                _filteredFilesList = value;
                OnPropertyChanged(nameof(FilteredFiles));
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

            string fileName = GetFileNameFromUrl(Url);
            string destinationPath = Path.Combine(FilePath, fileName);
            var downloadFile = new DownloadFile { FileName = fileName, FilePath = destinationPath, Status = "Downloading" };

            _allFiles.Add(downloadFile);  
            FilterFiles(); 

            try
            {
                await DownloadFileAsync(Url, destinationPath, _cancellationTokenSource.Token);
                downloadFile.Status = "Completed";
                _completedFiles.Add(downloadFile);  
                _incompleteFiles.Remove(downloadFile);  
            }
            catch (OperationCanceledException)
            {
                downloadFile.Status = "Incomplete";
                if (!_incompleteFiles.Contains(downloadFile))
                    _incompleteFiles.Add(downloadFile);  
            }
            finally
            {
                FilterFiles();  
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();  
            MessageBox.Show("Download paused!");
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            string fileName = GetFileNameFromUrl(Url);
            string destinationPath = Path.Combine(FilePath, fileName);

            
            var downloadFile = _incompleteFiles.Find(f => f.FilePath == destinationPath);
            if (downloadFile != null)
            {
                downloadFile.Status = "Downloading";
                _incompleteFiles.Remove(downloadFile);
                if (!_allFiles.Contains(downloadFile))
                    _allFiles.Add(downloadFile);  
            }

            try
            {
                await DownloadFileAsync(Url, destinationPath, _cancellationTokenSource.Token);
                downloadFile.Status = "Completed";
                _completedFiles.Add(downloadFile);  
                _incompleteFiles.Remove(downloadFile);  
            }
            catch (OperationCanceledException)
            {
                downloadFile.Status = "Incomplete";
                if (!_incompleteFiles.Contains(downloadFile))
                    _incompleteFiles.Add(downloadFile);  
            }
            finally
            {
                FilterFiles();
            }
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            FilterFiles();  
        }

        private void FilterFiles()
        {
            var selectedFilter = (comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();

            if (selectedFilter == "Completed Files")
            {
                FilteredFiles = _completedFiles;
            }
            else if (selectedFilter == "Incomplete Files")
            {
                FilteredFiles = _incompleteFiles;
            }
            else
            {
                FilteredFiles = _allFiles;
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

        
        private string GetFileNameFromUrl(string url)
        {
            return Path.GetFileName(new Uri(url).AbsolutePath);
        }
    }

    
    public class DownloadFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Status { get; set; }  
    }
}