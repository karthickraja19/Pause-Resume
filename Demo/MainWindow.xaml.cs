using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using static System.Net.WebRequestMethods;

namespace Demo
{

    public partial class MainWindow : Window
    {

        private CancellationTokenSource _cancellationTokenSource;
        private long _downloadedBytes = 0;
        private string _url = "https://examplefile.com/text/txt/400-mb-txt";
        private string _filePath = @"C:\Users\2270395\Downloads";
        private string _fileName = "downloadedFile.pdf";
        private string _destinationPath;

        public MainWindow()
        {
            InitializeComponent();
            _destinationPath = Path.Combine(_filePath, _fileName);
        }

        private async void Download_ClickButtonAsync(object sender, RoutedEventArgs e)
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
                // Resume download from where it was paused
                client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(_downloadedBytes, null);
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException();

                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            _downloadedBytes += bytesRead;
                        }
                    }
                }
            }
        }
    }
}
