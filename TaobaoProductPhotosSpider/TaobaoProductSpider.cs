using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using static OpenQA.Selenium.RelativeBy;

namespace TaobaoProductPhotosSpider
{
    record ReviewInfo(IEnumerable<string> ImgUrl, string RateContent, string RateSku, string Rater, string RateDate, string AppendContent, string AppendDate, IEnumerable<string> AppendImgUrl);
    class Alldata
    {
        public List<ReviewInfo> ReviewInfos { get; set; }
    }
    class TaobaoProductSpider
    {
        public bool EnableAutoNext { get; set; }
        public int webdriverport { get; set; } = 57813;
        public string webdriverpath { get; set; } = @"C:\Users\yinyu\Documents\commongreensoftwares\edgedriver_win64";
        public string ReportPath { get; set; } = @"C:\Users\yinyu\Documents\testkouhong.json";
        NoUIDispatcher NoUIDispatcher { get; } = new NoUIDispatcher();
        IWebDriver WebDriver;
        public string ProductUrl { get; set; } = "https://detail.tmall.com/item.htm?spm=a230r.1.14.9.4f9a4d3fCtITN4&id=582724323698&ns=1&abbucket=11";
        public void Stop()
        {
            WebDriver.Quit();
        }
        public async Task StartAsync()
        {
            if(WebDriver==null)
            {
                WebDriver = new OpenQA.Selenium.Edge.EdgeDriver(
                OpenQA.Selenium.Edge.EdgeDriverService.CreateDefaultService(webdriverpath, "msedgedriver.exe", webdriverport),
                new OpenQA.Selenium.Edge.EdgeOptions());
            }

            NoUIDispatcher.Start();
            await NoUIDispatcher.RunAsync(() =>
            {
                WebDriver.Navigate().GoToUrl(ProductUrl);
                Task.Delay(2000).Wait();
                if (WebDriver.Url.Contains("tmall.com", StringComparison.OrdinalIgnoreCase))
                {
                    WebDriver.Url += "#J_Reviews";
                    Task.Delay(2000).Wait();
                    for (int i = 0; i < 100; i++)
                    {
                        try
                        {
                            var ratepic = WebDriver.FindElement(By.ClassName("rate-list-picture"));
                            ratepic.Click();
                        }
                        catch (NoSuchElementException)
                        {
                            continue;
                        }
                        catch
                        {

                        }
                        break;
                    }
                }
                else
                {

                }


            }).ConfigureAwait(false);
        }
        public async Task NextAsync()
        {
            do
            {
                await NoUIDispatcher.RunAsync(() =>
                {
                    if(WebDriver.Url.Contains("tmall.com",StringComparison.OrdinalIgnoreCase))
                    {
                        var rategrid = WebDriver.FindElement(By.ClassName("rate-grid"));
                        var reviews = rategrid.FindElements(By.XPath("./table/tbody/tr"));
                        List<ReviewInfo> reviewsresult = new List<ReviewInfo>(reviews.Count);
                        foreach (var item in reviews)
                        {
                            var re = GetTmailReviewDetailAsync(item).Result;
                            reviewsresult.Add(re);
                            System.Diagnostics.Debug.WriteLine(re.ToString());
                        }

                        var page = WebDriver.FindElement(By.ClassName("rate-paginator"));
                        var nextpage = page.FindElement(By.XPath("a[text()='下一页>>']"));
                        nextpage.Click();

                        var alldata = File.Exists(ReportPath) ? Newtonsoft.Json.JsonConvert.DeserializeObject<Alldata>(File.ReadAllText(ReportPath)) : null;
                        if (alldata == null)
                            alldata = new Alldata() { ReviewInfos = new List<ReviewInfo>() };
                        alldata.ReviewInfos.AddRange(reviewsresult);
                        File.WriteAllText(ReportPath, Newtonsoft.Json.JsonConvert.SerializeObject(alldata));

                        Task.Delay(1000).Wait();
                    }
                    else
                    {
                        var rategrid = WebDriver.FindElement(By.ClassName("tb-revbd"));
                        var reviews = rategrid.FindElements(By.XPath("./ul/li"));
                        List<ReviewInfo> reviewsresult = new List<ReviewInfo>(reviews.Count);
                        foreach (var item in reviews)
                        {
                            var re = GetTaobaoReviewDetailAsync(item).Result;
                            reviewsresult.Add(re);
                            System.Diagnostics.Debug.WriteLine(re.ToString());
                        }

                        var nextpage = WebDriver.FindElement(By.ClassName("pg-next"));
                        nextpage.Click();

                        var alldata = File.Exists(ReportPath) ? Newtonsoft.Json.JsonConvert.DeserializeObject<Alldata>(File.ReadAllText(ReportPath)) : null;
                        if (alldata == null)
                            alldata = new Alldata() { ReviewInfos = new List<ReviewInfo>() };
                        alldata.ReviewInfos.AddRange(reviewsresult);
                        File.WriteAllText(ReportPath, Newtonsoft.Json.JsonConvert.SerializeObject(alldata));

                        Task.Delay(1000).Wait();
                    }

                }).ConfigureAwait(false);
            } while (EnableAutoNext);
        }
        public async Task<ReviewInfo> GetTaobaoReviewDetailAsync(IWebElement webElement)
        {
            var ratedate = webElement.FindElement(By.ClassName("tb-r-date")).Text;
            var fulltext = webElement.FindElement(By.ClassName("tb-tbcr-content")).Text;
            var sku = webElement.FindElement(By.ClassName("tb-r-info")).Text;
            var pic = webElement.FindElement(By.ClassName("tb-rev-item-media"));
            var pics = pic.FindElements(By.ClassName("photo-item"));
            var ano = webElement.FindElement(By.ClassName("from-whom")).Text;
            List<string> urls = new List<string>();
            foreach (var item in pics)
            {
                var img = item.FindElement(By.XPath("img"));
                urls.Add(img.GetAttribute("src"));
            }
            var append = webElement.FindElements(By.ClassName("tb-rev-item-append"));
            string AppendContent = null; string AppendDate = null;
            List<string> appendurls = null;
            if (append.Count==1)
            {
                var appendele = append[0];
                AppendContent = appendele.FindElement(By.ClassName("tb-tbcr-content")).Text;
                AppendDate = appendele.FindElement(By.ClassName("tb-r-date")).Text;
                var picappend = webElement.FindElement(By.ClassName("tb-rev-item-media"));
                var appendpics = picappend.FindElements(By.ClassName("photo-item"));
                appendurls = new List<string>();
                foreach (var item in appendpics)
                {
                    appendurls.Add(item.GetAttribute("src"));
                }
            }
            return new ReviewInfo(urls, fulltext, sku, ano, ratedate, AppendContent, AppendDate, appendurls);
        }
        public async Task<ReviewInfo> GetTmailReviewDetailAsync(IWebElement webElement)
        {
            var ratedate = webElement.FindElement(By.ClassName("tm-rate-date")).Text;
            var fulltext = webElement.FindElement(By.ClassName("tm-rate-fulltxt")).Text;
            var sku = webElement.FindElement(By.ClassName("rate-sku")).Text;
            var pics = webElement.FindElement(By.ClassName("tm-m-photos"));
            var picss = pics.FindElements(By.XPath(".//li[@data-src]"));
            var ano = webElement.FindElement(By.ClassName("rate-user-info")).Text;
            List<string> urls = new List<string>();
            foreach (var item in picss)
            {
                urls.Add(item.GetAttribute("data-src"));
            }
            var append = webElement.FindElements(By.ClassName("tm-rate-append"));
            string AppendContent = null; string AppendDate = null;
            List<string> appendurls = null;
            if (append.Count==1)
            {
                var appendele = append[0];
                AppendContent = appendele.FindElement(By.ClassName("tm-rate-fulltxt")).Text;
                AppendDate= appendele.FindElement(By.ClassName("tm-rate-title")).Text;
                var appendpics = appendele.FindElement(By.ClassName("tm-m-photos"));
                var appendpicss = appendpics.FindElements(By.XPath(".//li[@data-src]"));
                appendurls = new List<string>();
                foreach (var item in appendpicss)
                {
                    appendurls.Add(item.GetAttribute("data-src"));
                }
            }
            return new ReviewInfo(urls, fulltext, sku, ano, ratedate, AppendContent, AppendDate, appendurls);
        }
    }
}
