using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using System.Security.Cryptography;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TaobaoProductPhotosSpider
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        object getsetting(string key)
        {
            if(Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }
        public MainWindow()
        {
            this.InitializeComponent();
            Title = "TaobaoProductPhotosSpider";
            taobaoProductSpider = new TaobaoProductSpider();
            filepath_box.Text = getsetting("reportpath") as string ?? string.Empty;
            webdriver_path.Text = getsetting("webdriverpath") as string ?? string.Empty;
            port_box.Text = getsetting("webdriverport")?.ToString() ?? string.Empty;
        }
        TaobaoProductSpider taobaoProductSpider;
        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.IsEnabled = false;
            try
            {
                taobaoProductSpider.ProductUrl = url_box.Text;
                taobaoProductSpider.ReportPath = filepath_box.Text;
                taobaoProductSpider.webdriverpath = webdriver_path.Text;
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["reportpath"] = filepath_box.Text;
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["webdriverpath"] = webdriver_path.Text;

                if (int.TryParse(port_box.Text, out var port))
                {
                    taobaoProductSpider.webdriverport = port;
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values["webdriverport"] = port;
                }
                else
                {
                    if (port_box.Text != "")
                    {
                        await new ContentDialog() { Title = "ERROR", Content = "端口号无效",CloseButtonText="OK",XamlRoot=this.Content.XamlRoot }.ShowAsync();
                    }
                }
                await taobaoProductSpider.StartAsync();
            }
            catch(Exception err)
            {
                await new ContentDialog() { Title="ERROR", Content= err.ToString(), CloseButtonText = "OK", XamlRoot = this.Content.XamlRoot }.ShowAsync();
            }
            finally
            {
                myButton.IsEnabled = true;
            }
        }

        private async void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            savelastpage.IsEnabled = false;
            nextBtn.IsEnabled = false;
            try
            {
                await taobaoProductSpider.NextAsync(true);
            }
            catch(Exception err)
            {
                await new ContentDialog() { Content = err.ToString(), XamlRoot = this.Content.XamlRoot, CloseButtonText = "OK" }.ShowAsync();
            }
            finally
            {
                savelastpage.IsEnabled = true;
                nextBtn.IsEnabled = true;
            }
        }

        private void autonext_chk_Checked(object sender, RoutedEventArgs e)
        {
            taobaoProductSpider.EnableAutoNext = true;
        }

        private void autonext_chk_Unchecked(object sender, RoutedEventArgs e)
        {
            taobaoProductSpider.EnableAutoNext = false;
        }

        private void stop_btn_Click(object sender, RoutedEventArgs e)
        {
            taobaoProductSpider.Stop();
        }
        private async void download_btn_Click(object sender, RoutedEventArgs e)
        {
            download_btn.IsEnabled = false;
            try
            {
                string purl(string url)
                {
                    url = url.Replace("_40x40.jpg", string.Empty);
                    url = url.Replace("_400x400.jpg", string.Empty);
                    if(url.StartsWith("//",StringComparison.Ordinal))
                    {
                        url = "https:" + url;
                    }
                    return url;
                }
                var filepath = filepath_box.Text;
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Alldata>(File.ReadAllText(filepath));
                var dir = Path.GetDirectoryName(filepath);
                var filename = Path.GetFileNameWithoutExtension(filepath);
                var dirname = Path.Combine(dir, filename.Trim());
                if(!Directory.Exists(dirname))
                {
                    Directory.CreateDirectory(dirname);
                }
                using var md5 = MD5.Create();
                List<(string, string)> urls = new List<(string, string)>();
                foreach (var item in data.ReviewInfos)
                {
                    foreach(var one in item.ImgUrl)
                    {
                        var hex = Windows.Security.Cryptography.CryptographicBuffer.EncodeToHexString(
                            Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(
                                md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(one))));

                        urls.Add((purl(one), hex + ".jpg"));
                    }
                }
                using var httpclient = new System.Net.Http.HttpClient();
                bool nocmp = true;
                while(nocmp)
                {
                    bool havenoexist = false;
                    foreach(var one in urls)
                    {
                        var imgpath = Path.Combine(dirname, one.Item2);
                        if (File.Exists(imgpath))
                        {

                        }
                        else
                        {
                            havenoexist = true;

                            //download

                            var httpResponse = await httpclient.GetAsync(one.Item1);
                            if(httpResponse.IsSuccessStatusCode)
                            {
                                using var stream = File.Create(imgpath);
                                await httpResponse.Content.CopyToAsync(stream);
                            }
                            
                        }
                    }
                    if(!havenoexist)
                    {
                        nocmp = false;
                    }
                }
            }
            catch(Exception err)
            {
                await new ContentDialog() { Content= err.ToString(), XamlRoot = this.Content.XamlRoot, CloseButtonText = "OK" }.ShowAsync();
            }
            finally
            {
                download_btn.IsEnabled = true;
            }
        }

        private async void savelastpage_Click(object sender, RoutedEventArgs e)
        {
            savelastpage.IsEnabled = false;
            nextBtn.IsEnabled = false;
            try
            {
                await taobaoProductSpider.NextAsync(false);
            }
            catch (Exception err)
            {
                await new ContentDialog() { Content = err.ToString(), XamlRoot = this.Content.XamlRoot, CloseButtonText = "OK" }.ShowAsync();
            }
            finally
            {
                savelastpage.IsEnabled = true;
                nextBtn.IsEnabled = true;
            }
        }
    }
}
