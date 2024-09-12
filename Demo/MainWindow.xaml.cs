using System.IO;
using System.Net.Http;
using System.Text;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //comboBox.ItemsSource = new List<string> { "All Files", "Completed Files", "Incomplete Files" };
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            MessageBox.Show($"{menuItem.Header} clicked");
        }
        private async  void Download_ClickButtonAsync(object sender, RoutedEventArgs e)
        {
            string url = "https://www.learningcontainer.com/wp-content/uploads/2019/09/sample-pdf-file.pdf";
            string filePath = @"C:\\download manager";
            string fileExtension = Path.GetExtension(url);
            string fileName = "downloadedFile" + fileExtension;
            string destinationPath = Path.Combine(filePath, fileName);
            try
            {
                await DownloadFileAsync(url, destinationPath);
                MessageBox.Show("File downloaded successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void DownloadButton_MouseEnter(object sender, MouseEventArgs e)
        {
            DownloadButton.Background = new SolidColorBrush(Colors.LightGreen);
        }

        private void DownloadButton_MouseLeave(object sender, MouseEventArgs e)
        {
            DownloadButton.Background = new SolidColorBrush(Colors.LightGray);
        }


        private async Task DownloadFileAsync(string url, string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(0, null);
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync(), fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }
                }
            }
        }
    }
}
