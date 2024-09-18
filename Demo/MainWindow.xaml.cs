using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
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
            UpdateButtonStates();

            NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;
        }

        private void NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (!e.IsAvailable)
            {
                
                Application.Current.Dispatcher.Invoke(() => PauseDownloadOnNetworkLoss());
            }
        }

        private void PauseDownloadOnNetworkLoss()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel(); 
                MessageBox.Show("Your system is offline. The download is paused.", "Network Error", MessageBoxButton.OK, MessageBoxImage.Warning);

                IsDownloadEnabled = false;
                IsPauseEnabled = false;
                IsResumeEnabled = false;
                UpdateButtonStates();
            }
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
        public bool IsDownloadEnabled { get; set; } = true;
        public bool IsPauseEnabled { get; set; } = false;
        public bool IsResumeEnabled { get; set; } = false;

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
        private void UpdateButtonStates()
        {
            OnPropertyChanged(nameof(IsDownloadEnabled));
            OnPropertyChanged(nameof(IsPauseEnabled));
            OnPropertyChanged(nameof(IsResumeEnabled));
        }
        private async void Download_ClickButtonAsync(object sender, RoutedEventArgs e)
        {
            IsDownloadEnabled = false;
            IsPauseEnabled = true;
            IsResumeEnabled = false;
            UpdateButtonStates();
            ResetDownloadProgress();

            _cancellationTokenSource = new CancellationTokenSource();

            string fileName = GetFileNameFromUrl(Url);  // Extract the file name from the URL
            string destinationPath = FilePath;  // Just the directory

            var downloadFile = new DownloadFile
            {
                FileName = fileName,  // Store the file name separately
                FilePath = destinationPath,  // Store the directory path
                Url = Url,
                Status = "Downloading"
            };

            _allFiles.Add(downloadFile);
            FilterFiles();

            try
            {
                await DownloadFileAsync(Url, downloadFile.FullPath, _cancellationTokenSource.Token);  // Use the full path here
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
                IsDownloadEnabled = true;
                IsPauseEnabled = false;
                IsResumeEnabled = false;
                UpdateButtonStates();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            MessageBox.Show("Download paused!");
            IsDownloadEnabled = false;
            IsPauseEnabled = false;
            IsResumeEnabled = true;
            UpdateButtonStates();
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            IsDownloadEnabled = false;
            IsPauseEnabled = true;
            IsResumeEnabled = false;
            UpdateButtonStates();
            _cancellationTokenSource = new CancellationTokenSource();

            string fileName = GetFileNameFromUrl(Url);
            string destinationPath = FilePath;  // Only the directory path

            var downloadFile = _incompleteFiles.Find(f => f.FullPath == Path.Combine(destinationPath, fileName));
            if (downloadFile != null)
            {
                downloadFile.Status = "Downloading";
                _incompleteFiles.Remove(downloadFile);
                if (!_allFiles.Contains(downloadFile))
                    _allFiles.Add(downloadFile);
            }

            try
            {
                await DownloadFileAsync(Url, downloadFile.FullPath, _cancellationTokenSource.Token);  // Use the full path here
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
                IsDownloadEnabled = true;
                IsPauseEnabled = false;
                IsResumeEnabled = false;
                UpdateButtonStates();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            //MessageBox.Show("Download canceled!");
            ResetDownloadProgress();
            Url = string.Empty;
            FilePath = string.Empty;
            IsDownloadEnabled = true;
            IsPauseEnabled = false;
            IsResumeEnabled = false;
            UpdateButtonStates();
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            FilterFiles();
        }

        private void FilterFiles()
        {
            var selectedFilter = (comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
            switch (selectedFilter)
            {
                case "Completed Files":
                    FilteredFiles = _completedFiles;
                    foreach (var file in _completedFiles)
                    {
                        file.Status = "Completed";
                        file.DownloadedBytes = file.TotalBytes; 
                    }
                    break;
                case "Incomplete Files":
                    FilteredFiles = _incompleteFiles;
                    UpdateProgressForIncompleteFiles();
                    break;
                default:
                    FilteredFiles = _allFiles;
                    //ResetDownloadProgress();
                    break;
            }
            OnPropertyChanged(nameof(FilteredFiles));
        }

        private void UpdateProgressForIncompleteFiles()
        {
            foreach (var file in _incompleteFiles)
            {
                if (File.Exists(file.FilePath))
                {
                    FileInfo fileInfo = new FileInfo(file.FilePath);
                    file.DownloadedBytes = fileInfo.Length;
                }

                if (file.TotalBytes > 0)
                {
                    double progress = (double)((file.DownloadedBytes / file.TotalBytes) * 100);
                    file.Status = $"Downloading: {progress:0.00}%";
                }
                else if(file.TotalBytes==0)
                {
                    file.Status = "Paused";
                }
            }
            OnPropertyChanged(nameof(FilteredFiles));
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

                            
                            var file = _incompleteFiles.FirstOrDefault(f => f.FilePath == filePath);
                            if (file != null)
                            {
                                file.DownloadedBytes = _downloadedBytes;
                                file.TotalBytes = _totalBytes;
                            }
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
                var file = _allFiles.FirstOrDefault(f => f.FilePath == FilePath);
                if (file != null)
                {
                    file.DownloadedBytes = downloadedBytes;
                    file.TotalBytes = totalBytes;
                }
                progressBar.Maximum = totalBytes;
                progressBar.Value = downloadedBytes;
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

        private void FilesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedFile = FilesListBox.SelectedItem as DownloadFile;

            if (selectedFile != null)
            {
                
                Url = selectedFile.Url;
                FilePath = selectedFile.FilePath; 
                string fullPath = selectedFile.FullPath;

               
                if (selectedFile.Status == "Completed")
                {
                    progressBar.Maximum = 100;
                    progressBar.Value = 100;
                    DownloadPercentage = "100.00%";
                }
                else if (selectedFile.Status == "Incomplete" && selectedFile.TotalBytes > 0)
                {
                    double progress = (double)selectedFile.DownloadedBytes / selectedFile.TotalBytes * 100;
                    progressBar.Maximum = selectedFile.TotalBytes;
                    progressBar.Value = selectedFile.DownloadedBytes;
                    DownloadPercentage = $"{progress:0.00}%";
                }
                else
                {
                    progressBar.Value = 0;
                    progressBar.Maximum = 100;
                    DownloadPercentage = "0.00%";
                }
            }
            else
            {
                progressBar.Value = 0;
                progressBar.Maximum = 100;
                DownloadPercentage = "0.00%";
                Url = string.Empty;
                FilePath = string.Empty;
            }
        }

        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            await ResumeDownload();

        }

        private async Task ResumeDownload()
        {
            IsDownloadEnabled = false;
            IsPauseEnabled = true;
            IsResumeEnabled = false;
            UpdateButtonStates();
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
                IsDownloadEnabled = true;
                IsPauseEnabled = false;
                IsResumeEnabled = false;
                UpdateButtonStates();
            }
        }
    }


    public class DownloadFile
    {
        public string FileName { get; set; } 
        public string FilePath { get; set; } 
        public string Url { get; set; }
        public string Status { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }

        public string FullPath => Path.Combine(FilePath, FileName); 
    }


}