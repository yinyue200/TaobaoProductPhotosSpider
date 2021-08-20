using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

namespace TaobaoInfoReciever
{
    /// <summary>
    /// DownloadProgressWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadProgressWindow : Window
    {
        public DownloadProgressWindow(string json)
        {
            InitializeComponent();
            download(json);
            CancellationToken = CancellationTokenSource.Token;
        }
        static string hex(byte[] s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(35);
            for (int i = 0; i < s.Length; i++)
            {
                // 将得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                sb.Append(s[i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        CancellationToken CancellationToken;
        private async void download(string json)
        {
            try
            {
                string purl(string url)
                {
                    url = url.Replace("_40x40.jpg", string.Empty);
                    url = url.Replace("_400x400.jpg", string.Empty);
                    if (url.StartsWith("//", StringComparison.Ordinal))
                    {
                        url = "https:" + url;
                    }
                    return url;
                }
                var filepath = json;
                Alldata data;
                using (var reader = File.OpenText(filepath))
                {
                    using var jsontextreader = new Newtonsoft.Json.JsonTextReader(reader);
                    var se = Newtonsoft.Json.JsonSerializer.CreateDefault();
                    data = se.Deserialize<Alldata>(jsontextreader);
                }
                var dir = Path.GetDirectoryName(filepath);
                var filename = Path.GetFileNameWithoutExtension(filepath);
                var dirname = Path.Combine(dir, filename.Trim());
                if (!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                using var md5 = MD5.Create();
                List<(string, string)> urls = new List<(string, string)>();
                foreach (var item in data.ReviewInfos)
                {
                    foreach (var one in item.ImgUrl)
                    {
                        var hexstr = hex(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(one)));

                        urls.Add((purl(one), hexstr + ".jpg"));
                    }
                }
                using var httpclient = new System.Net.Http.HttpClient();
                bool nocmp = true;
                progress.Maximum = urls.Count;
                while (nocmp)
                {
                    bool havenoexist = false;
                    int downloadcount = 0;
                    foreach (var one in urls)
                    {
                        CancellationToken.ThrowIfCancellationRequested();
                        var imgpath = Path.Combine(dirname, one.Item2);
                        if (File.Exists(imgpath))
                        {
                            downloadcount++;
                        }
                        else
                        {
                            havenoexist = true;

                            //download
                            downloadcount++;
                            var httpResponse = await httpclient.GetAsync(one.Item1, CancellationToken);
                            if (httpResponse.IsSuccessStatusCode)
                            {
                                using var stream = File.Create(imgpath);
                                await httpResponse.Content.CopyToAsync(stream);
                            }
                            progress.Value = downloadcount;
                        }
                    }
                    progress.Value = downloadcount;
                    if (!havenoexist)
                    {
                        nocmp = false;
                    }
                }
                MessageBox.Show("Done");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                await Task.Yield();
                Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource.Cancel();
        }

        private void progress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            statusbox.Text = $"{e.NewValue}/{progress.Maximum}";
        }
    }
}
