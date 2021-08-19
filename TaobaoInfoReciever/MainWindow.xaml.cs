using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using PeanutButter.Utils;
using System.Collections.ObjectModel;
using System.IO;

namespace TaobaoInfoReciever
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        record ReviewInfo(IEnumerable<string> ImgUrl, string RateContent, string RateSku, string Rater, string RateDate, string AppendContent, string AppendDate, IEnumerable<string> AppendImgUrl);
        PeanutButter.SimpleHTTPServer.HttpServer httpServer;
        public MainWindow()
        {
            InitializeComponent();
            list.ItemsSource = ParsedReviewsInfos;
        }
        record revieveinfo(string url,string fullhtml);
        record ParsedReviewsInfo(string Page,List<ReviewInfo> Reviews);
        ObservableCollection<ParsedReviewsInfo> ParsedReviewsInfos { get; set; } = new ObservableCollection<ParsedReviewsInfo>();
        private void StartButton_Click(object sender,RoutedEventArgs e)
        {
            if (httpServer == null)
            {
                int port;
                if(int.TryParse(portbox.Text,out port))
                {

                }
                else
                {
                    port = 16832;
                }
                httpServer = new PeanutButter.SimpleHTTPServer.HttpServer(port, null);
                httpServer.AddJsonDocumentHandler((hp, stream) =>
                {
                    if (hp.Path == "/yinyue200/TaobaoInfoReciever/postinfo")
                    {
                        var poststr = stream.AsString();
                        var postobj = Newtonsoft.Json.JsonConvert.DeserializeObject<revieveinfo>(poststr);
                        var context = BrowsingContext.New(Configuration.Default);
                        var parser = context.GetService<IHtmlParser>();
                        var source = postobj.fullhtml;
                        var document = parser.ParseDocument(source);
                        if (postobj.url.Contains("tmall.com", StringComparison.OrdinalIgnoreCase))
                        {
                            var pagecontin = document.QuerySelector(".rate-paginator");
                            var nowpage = pagecontin.SelectNodes("span").Select(a => a.TextContent.Trim()).First(a => a is not "<<上一页" and not "..."); ;
                            var rategrid = document.QuerySelector(".rate-grid");
                            var reviews = rategrid.SelectNodes("./table/tbody/tr");
                            List<ReviewInfo> reviewsresult = new List<ReviewInfo>(reviews.Count);
                            foreach (IHtmlElement item in reviews)
                            {
                                var re = GetTmailReviewDetailAsync(item);
                                reviewsresult.Add(re);
                            }
                            Dispatcher.Invoke(() =>
                            {
                                ParsedReviewsInfos.Add(new ParsedReviewsInfo(nowpage, reviewsresult));
                            });
                        }
                        else
                        {
                            var rategrid = document.QuerySelector(".tb-revbd");
                            var reviews = rategrid.SelectNodes("./ul/li");
                            List<ReviewInfo> reviewsresult = new List<ReviewInfo>(reviews.Count);
                            foreach (IHtmlElement item in reviews)
                            {
                                var re = GetTaobaoReviewDetailAsync(item);
                                reviewsresult.Add(re);
                            }
                        }
                    }
                    return new { issucc = true };
                });
            }
        }
        private ReviewInfo GetTaobaoReviewDetailAsync(IHtmlElement webElement)
        {
            var ratedate = webElement.QuerySelector(".tb-r-date").TextContent;
            var fulltext = webElement.QuerySelector(".tb-tbcr-content").TextContent;
            var sku = webElement.QuerySelector(".tb-r-info").TextContent;
            var pic = webElement.QuerySelector(".tb-rev-item-media");
            var pics = pic.QuerySelectorAll(".photo-item");
            var ano = webElement.QuerySelector(".from-whom").TextContent;
            List<string> urls = new List<string>();
            foreach (var item in pics)
            {
                var img = (IHtmlElement)item.SelectSingleNode("img");
                urls.Add(img.GetAttribute("src"));
            }
            var append = webElement.QuerySelectorAll(".tb-rev-item-append").ToList();
            string AppendContent = null; string AppendDate = null;
            List<string> appendurls = null;
            if (append.Count == 1)
            {
                var appendele = append[0];
                AppendContent = appendele.QuerySelector(".tb-tbcr-content").TextContent;
                AppendDate = appendele.QuerySelector(".tb-r-date").TextContent;
                var picappend = webElement.QuerySelector(".tb-rev-item-media");
                var appendpics = picappend.QuerySelectorAll(".photo-item");
                appendurls = new List<string>();
                foreach (var item in appendpics)
                {
                    appendurls.Add(item.GetAttribute("src"));
                }
            }
            return new ReviewInfo(urls, fulltext, sku, ano, ratedate, AppendContent, AppendDate, appendurls);
        }
        private ReviewInfo GetTmailReviewDetailAsync(IHtmlElement webElement)
        {
            var ratedate = webElement.QuerySelector(".tm-rate-date").TextContent;
            var fulltext = webElement.QuerySelector(".tm-rate-fulltxt").TextContent;
            var sku = webElement.QuerySelector(".rate-sku").TextContent;
            var pics = webElement.QuerySelector(".tm-m-photos");
            var picss = pics.SelectNodes(".//li[@data-src]");
            var ano = webElement.QuerySelector(".rate-user-info").TextContent;
            List<string> urls = new List<string>();
            foreach (IHtmlElement item in picss)
            {
                urls.Add(item.GetAttribute("data-src"));
            }
            var append = webElement.QuerySelectorAll(".tm-rate-append").ToList();
            string AppendContent = null; string AppendDate = null;
            List<string> appendurls = null;
            if (append.Count == 1)
            {
                var appendele = append[0];
                AppendContent = appendele.QuerySelector(".tm-rate-fulltxt").TextContent;
                AppendDate = appendele.QuerySelector(".tm-rate-title").TextContent;
                var appendpics = appendele.QuerySelector(".tm-m-photos");
                var appendpicss = appendpics.SelectNodes(".//li[@data-src]");
                appendurls = new List<string>();
                foreach (IHtmlElement item in appendpicss)
                {
                    appendurls.Add(item.GetAttribute("data-src"));
                }
            }
            return new ReviewInfo(urls, fulltext, sku, ano, ratedate, AppendContent, AppendDate, appendurls);
        }

        private void clearbutton_Click(object sender, RoutedEventArgs e)
        {
            ParsedReviewsInfos.Clear();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            httpServer.Dispose();
            httpServer = null;
        }

        private void delbutton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in list.SelectedItems.Cast<ParsedReviewsInfo>().ToList())
            {
                ParsedReviewsInfos.Remove(item);
            }
        }
        class Alldata
        {
            public List<ReviewInfo> ReviewInfos { get; set; }
        }
        private void savebutton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "test"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.Filter = "JSON files (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();
            if(result.HasValue&&result.Value)
            {
                string filename = dialog.FileName;
                Alldata alldata = new Alldata
                {
                    ReviewInfos = ParsedReviewsInfos.Select(a => a.Reviews).Aggregate<IEnumerable<ReviewInfo>>((a, b) => a.Concat(b)).ToList()
                };
                File.WriteAllText(filename, Newtonsoft.Json.JsonConvert.SerializeObject(alldata));
            }

        }
    }
}
