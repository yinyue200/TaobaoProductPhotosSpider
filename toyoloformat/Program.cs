using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Globalization;
using System.Text.RegularExpressions;
using static MoreLinq.Extensions.ShuffleExtension;
using SixLabors.ImageSharp.Processing;

namespace toyoloformat
{
    using SizeD = ValueTuple<double, double>;
    class Program
    {
        class Alldata
        {
            public List<ReviewInfo> ReviewInfos { get; set; }
        }
        record ReviewInfo(IEnumerable<string> ImgUrl, string RateContent, string RateSku, string Rater, string RateDate, string AppendContent, string AppendDate, IEnumerable<string> AppendImgUrl);
        static ReviewInfo GetInfo(Dictionary<string, ReviewInfo> cacheinfo, string path)
        {
            var npath = System.IO.Path.GetFileNameWithoutExtension(path);
            var pi = npath.IndexOf('.', StringComparison.Ordinal);
            var nfilename = pi < 0 ? npath : npath.Substring(0, pi);
            cacheinfo.TryGetValue(nfilename, out var reviewInfo);
            return reviewInfo;
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
        static Dictionary<string, ReviewInfo> GetCacheInfo(string dirpath)
        {
            using MD5 md5 = MD5.Create(); ;
            Dictionary<string, ReviewInfo> keyValues = new Dictionary<string, ReviewInfo>();
            var alldata = Newtonsoft.Json.JsonConvert.DeserializeObject<Alldata>(System.IO.File.ReadAllText(dirpath));
            if (alldata == null)
                return null;
            foreach (var one in alldata.ReviewInfos)
            {
                foreach (var url in one.ImgUrl ?? Array.Empty<string>())
                {
                    keyValues[hex(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url)))] = one;
                }
                foreach (var url in one.AppendImgUrl ?? Array.Empty<string>())
                {
                    keyValues[hex(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url)))] = one;
                }
            }
            return keyValues;
        }
        static (double xmin, double xmax, double ymin, double ymax) getrectinfo(CropResult cropResult, double imgh, double imgw)
        {
            var x = cropResult.x;
            var y = cropResult.y;
            var width = cropResult.width;
            var height = cropResult.height;
            var rotate = cropResult.rotate;
            var ncos = Math.Cos(rotate * Math.PI / 180.0);
            var nsin = Math.Sin(rotate * Math.PI / 180.0);
            var rncos = Math.Cos(-rotate * Math.PI / 180.0);
            var rnsin = Math.Sin(-rotate * Math.PI / 180.0);
            var m1 = imgh * ncos;
            var m5 = imgh * nsin;

            SizeD rotatexy(double x0, double y0, double xcenter, double ycenter)
            {
                var x = (x0 - xcenter) * ncos - (y0 - ycenter) * nsin + xcenter;
                var y = (x0 - xcenter) * nsin + (y0 - ycenter) * ncos + ycenter;
                return (x, y);
            }


            SizeD rrotatexy(double x0, double y0, double xcenter, double ycenter)
            {
                var x = (x0 - xcenter) * rncos - (y0 - ycenter) * rnsin + xcenter;
                var y = (x0 - xcenter) * rnsin + (y0 - ycenter) * rncos + ycenter;
                return (x, y);
            }

            double min(params double[] a)
            {
                return System.Linq.Enumerable.Min(a);
            }
            double max(params double[] a)
            {
                return System.Linq.Enumerable.Max(a);
            }

            var p11 = rotatexy(0, 0, 0, 0);
            var p21 = rotatexy(imgw, 0, 0, 0);
            var p31 = rotatexy(0, imgh, 0, 0);
            var p41 = rotatexy(imgw, imgh, 0, 0);
            var xmin1 = min(p11.Item1, p21.Item1, p31.Item1, p41.Item1);
            var xmax1 = max(p11.Item1, p21.Item1, p31.Item1, p41.Item1);
            var ymin1 = min(p11.Item2, p21.Item2, p31.Item2, p41.Item2);
            var ymax1 = max(p11.Item2, p21.Item2, p31.Item2, p41.Item2);
            var pnnwidth = xmax1 - xmin1;
            var pnnheight = ymax1 - ymin1;
            var startpoint = (p11.Item1 - xmin1, p11.Item1 - ymin1); //当前左上角点在图像的位置
            var rrstartpoint = rrotatexy(startpoint.Item1, startpoint.Item2, 0, 0);

            SizeD switchxy(double x, double y)
            {
                var (x1, y1) = rrotatexy(x, y, 0, 0);
                x1 -= rrstartpoint.Item1;
                y1 -= rrstartpoint.Item2;
                return (x1, y1);
            }


            var p1 = switchxy(x, y);
            var p2 = switchxy(x + width, y);
            var p3 = switchxy(x, y + height);
            var p4 = switchxy(x + width, y + height);
            var xmin = min(p1.Item1, p2.Item1, p3.Item1, p4.Item1);
            var xmax = max(p1.Item1, p2.Item1, p3.Item1, p4.Item1);
            var ymin = min(p1.Item2, p2.Item2, p3.Item2, p4.Item2);
            var ymax = max(p1.Item2, p2.Item2, p3.Item2, p4.Item2);
            return (xmin, xmax, ymin, ymax);
        }
        static int Main(string[] args)
        {
            string[] classlist = new string[] { "口红本体", "口红膏体及本体", "口红涂抹样例", "口红外包装" };
            var rootCommand = new RootCommand();
            var convertcommand = new Command("convert");
            {
                var imglisttxt = new Argument("txtlistloc");
                var basefolderforimglist = new Argument("basefolderforimglist");
                var 
            }
            _ = app.Command("convert", (command) =>
               {
                   var imglisttxt = command.Argument("txtlistloc", string.Empty);
                   var basefolderforimglist = command.Argument("basefolderforimglist", string.Empty);
                   var tofolderimages = command.Argument("tofolderimages", string.Empty);
                   var tofolderlabels = command.Argument("tofolderlabels", string.Empty);
                   //var detailoption = command.Option("--detail -d <reviewjsonpath>", string.Empty, CommandOptionType.SingleValue);

                   command.OnExecute(() =>
                   {
                       var list = File.ReadAllLines(imglisttxt.Value);
                       var basefolderforimglistval = basefolderforimglist.Value;
                       var tofolderimagesval = tofolderimages.Value;
                       var tofolderlabelsval = tofolderlabels.Value;
                       //var detailoptionval = detailoption.HasValue() ? detailoption.Value() : null;
                       //var cachevalue = detailoptionval == null ? null : GetCacheInfo(detailoptionval);
                       foreach (var one in list)
                       {
                           if (!string.IsNullOrWhiteSpace(one))
                           {
                               var imgpath = Path.Combine(basefolderforimglistval, one);
                               if (File.Exists(imgpath))
                               {
                                   var imgfilenamepure = Path.GetFileNameWithoutExtension(imgpath);
                                   var imgfilename = Path.GetFileName(imgpath);
                                   var linkto = Path.Combine(tofolderimagesval, imgfilename);
                                   if (!File.Exists(linkto))
                                   {
                                       LostTech.IO.Links.Symlink.CreateForFile(imgpath, linkto);
                                   }
                                   var imgjsonpath = imgpath + ".json";
                                   var txtfilepath = Path.Combine(tofolderlabelsval, imgfilenamepure + ".txt");
                                   if (File.Exists(imgjsonpath))
                                   {
                                       var imginfo = SixLabors.ImageSharp.Image.Identify(imgpath);
                                       var tagresult = Newtonsoft.Json.JsonConvert.DeserializeObject<TagResult>(File.ReadAllText(imgjsonpath));
                                       if (tagresult != null && tagresult.TagCropResults != null)
                                       {
                                           var str = new StringBuilder();
                                           //var reviewinfo = cachevalue == null ? null : GetInfo(cachevalue, imgpath);
                                           foreach (var item in tagresult.TagCropResults)
                                           {
                                               var re = getrectinfo(item.CropResult, imginfo.Height, imginfo.Width);
                                               var xcenter = (re.xmax + re.xmin) / 2.0;
                                               var ycenter = (re.ymax + re.ymin) / 2.0;
                                               var width = re.xmax - re.xmin;
                                               var height = re.ymax - re.ymin;
                                               var index = Array.IndexOf(classlist, item.Tag);
                                               //if (reviewinfo != null)
                                               //{
                                               //    if (item.Tag == "口红膏体及本体" && reviewinfo.RateSku.Contains("999", StringComparison.Ordinal))
                                               //    {
                                               //        index = 4;
                                               //    }
                                               //    if (item.Tag == "口红膏体及本体" && reviewinfo.RateSku.Contains("720", StringComparison.Ordinal))
                                               //    {
                                               //        index = 5;
                                               //    }
                                               //}
                                               str.AppendLine($"{index} {xcenter / imginfo.Width} {ycenter / imginfo.Height} {width / imginfo.Width} {height / imginfo.Height}");
                                           }
                                           File.WriteAllText(txtfilepath, str.ToString());
                                       }
                                   }
                               }
                           }
                       }
                       return 0;
                   });
               });
            _ = app.Command("calcreview", (command) =>
               {
                   var reviewjson = command.Argument("reviewjsonpath", string.Empty);
                   var detailpick = command.Option("--detailpick -p <detialpickregex>", string.Empty, CommandOptionType.SingleValue);
                   command.OnExecute(() =>
                   {
                       Dictionary<string, int> calctag = new Dictionary<string, int>();
                       var reviewjsonval = reviewjson.Value;
                       var detailpickval = detailpick.HasValue() ? new Regex(detailpick.Value()) : null;
                       var alldata = Newtonsoft.Json.JsonConvert.DeserializeObject<Alldata>(System.IO.File.ReadAllText(reviewjsonval));
                       foreach (var one in alldata.ReviewInfos)
                       {
                           var sku = one.RateSku;
                           if (detailpickval != null)
                           {
                               var match = detailpickval.Match(sku);
                               if (match.Success)
                               {
                                   sku = match.Value;
                               }
                           }
                           string key = sku;
                           if (!calctag.ContainsKey(key))
                           {
                               calctag.Add(key, 1);
                           }
                           else
                           {
                               calctag[key]++;
                           }
                       }

                       foreach (var item in calctag)
                       {
                           Console.WriteLine($"{item.Key} : {item.Value}");
                       }
                       return 0;
                   });
               });
            _ = app.Command("calc", (command) =>
               {
                   var folder = command.Argument("folder", string.Empty);
                   var detailoption = command.Option("--detail -d <reviewjsonpath>", string.Empty, CommandOptionType.SingleValue);
                   var detailpick = command.Option("--detailpick -p <detialpickregex>", string.Empty, CommandOptionType.SingleValue);

                   command.OnExecute(() =>
                   {
                       int missingtag = 0;
                       int backgroundimg = 0;
                       Dictionary<string, int> calctag = new Dictionary<string, int>();
                       foreach (var one in classlist)
                       {
                           calctag[one] = 0;
                       }
                       var folderval = folder.Value;
                       var detailpickval = detailpick.HasValue() ? new Regex(detailpick.Value()) : null;

                       Dictionary<string, ReviewInfo> cacheinfo;
                       if (detailoption.HasValue())
                       {
                           cacheinfo = GetCacheInfo(detailoption.Value());
                       }
                       else
                       {
                           cacheinfo = null;
                       }

                       var filelist = Directory.EnumerateFiles(folderval);
                       var jpgs = filelist.Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).ToList();
                       foreach (var filename in jpgs)
                       {
                           var jsonfilename = filename + ".json";
                           if (File.Exists(jsonfilename))
                           {
                               var tagresult = Newtonsoft.Json.JsonConvert.DeserializeObject<TagResult>(File.ReadAllText(jsonfilename));
                               if (tagresult.PhotosTags.Count == 1 && tagresult.PhotosTags[0] == "无关图片")
                               {
                                   backgroundimg++;
                               }
                               else if (tagresult.TagCropResults.Count == 0)
                               {
                                   missingtag++;
                               }
                               else
                               {
                                   foreach (var crop in tagresult.TagCropResults)
                                   {
                                       ReviewInfo reviewInfo = null;
                                       if (cacheinfo != null)
                                       {
                                           reviewInfo = GetInfo(cacheinfo, filename);
                                       }
                                       calctag[crop.Tag]++;

                                       if (reviewInfo == null)
                                       {
                                       }
                                       else
                                       {
                                           var sku = reviewInfo.RateSku;
                                           if (detailpickval != null)
                                           {
                                               var match = detailpickval.Match(sku);
                                               if (match.Success)
                                               {
                                                   sku = match.Value;
                                               }
                                           }
                                           string key = crop.Tag + ":::" + sku;
                                           if (!calctag.ContainsKey(key))
                                           {
                                               calctag.Add(key, 1);
                                           }
                                           else
                                           {
                                               calctag[key]++;
                                           }
                                       }
                                   }
                               }
                           }
                           else
                           {
                               missingtag++;
                           }
                       }
                       Console.WriteLine($"missing {missingtag} background {backgroundimg}");
                       foreach (var item in calctag)
                       {
                           Console.WriteLine($"{item.Key} : {item.Value}");
                       }
                       Console.WriteLine($"图片数 ：{jpgs.Count}");
                       return 0;
                   });
               });
            _ = app.Command("genlist", (command) =>
            {
                var imgfolder = command.Argument("imgfolder", string.Empty);
                var outfolder = command.Argument("outfolder", string.Empty);
                var trainpercent = command.Argument("trainpercent", "min is 0 and max is 100");
                var exceptemptyoption = command.Option("--exceptempty", string.Empty, CommandOptionType.NoValue);
                command.OnExecute(() =>
                {
                    var imgfolderval = imgfolder.Value;
                    var trainpercentval = double.Parse(trainpercent.Value, CultureInfo.InvariantCulture);
                    var exceptemptyoptionval = exceptemptyoption.HasValue();
                    var outfolderval = outfolder.Value;
                    var filelist = Directory.EnumerateFiles(imgfolderval).ToList();
                    List<string> finallist;
                    if (exceptemptyoptionval)
                    {
                        var jsonlist = filelist.Where(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).Select(a => a[..^5]).ToHashSet();
                        finallist = filelist.Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && jsonlist.Contains(a)).Shuffle().ToList();
                    }
                    else
                    {
                        finallist = filelist.Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).Shuffle().ToList();
                    }
                    var traincountk = trainpercentval / 100.0;
                    var traincount = (int)(traincountk * finallist.Count);
                    var trainset = finallist.Take(traincount).ToList();
                    var valset = finallist.Skip(traincount).ToList();
                    var filenameran = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
                    File.WriteAllLines(Path.Combine(outfolderval, filenameran + "_train.txt"), trainset);
                    File.WriteAllLines(Path.Combine(outfolderval, filenameran + "_val.txt"), valset);
                    return 0;
                });
            });
            _ = app.Command("yolosplitbycolor", (command) =>
            {
                var detailoption = command.Argument("reviewjsonpath", string.Empty);
                var folderpath = command.Argument("folderpath", string.Empty);
                var linkedtofolderpath = command.Argument("linkedtofolderpath", string.Empty);
                var copyoption = command.Option("--copy", string.Empty, CommandOptionType.NoValue);
                command.OnExecute(() =>
                {
                    var detailoptionval = detailoption.Value;
                    var folderpathval = folderpath.Value;
                    var linkedtofolderpathval = linkedtofolderpath.Value;
                    var cachevalue = GetCacheInfo(detailoptionval);
                    var iscopy = copyoption.HasValue();

                    foreach (var one in Directory.EnumerateFiles(folderpathval))
                    {
                        var purename = Path.GetFileNameWithoutExtension(one);
                        if (cachevalue.TryGetValue(purename, out var reviewInfo))
                        {
                            var sku = reviewInfo.RateSku;
                            var folder = Path.Combine(linkedtofolderpathval, sku);
                            if(!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }
                            var linkto = Path.Combine(folder, purename + ".jpg");
                            if (!File.Exists(linkto))
                            {
                                if(iscopy)
                                {
                                    File.Copy(one, linkto);
                                }
                                else
                                {
                                    LostTech.IO.Links.Symlink.CreateForFile(one, linkto);
                                }
                            }
                        }
                    }
                    return 0;
                });

            });
            _ = app.Command("cropbycolor", (command) =>
            {
                var detailoption = command.Argument("reviewjsonpath", string.Empty);
                var folderpath = command.Argument("folderpath", string.Empty);
                var linkedtofolderpath = command.Argument("linkedtofolderpath", string.Empty);
                var copyoption = command.Option("--copy", string.Empty, CommandOptionType.NoValue);
                command.OnExecute(() =>
                {
                    var detailoptionval = detailoption.Value;
                    var folderpathval = folderpath.Value;
                    var linkedtofolderpathval = linkedtofolderpath.Value;
                    var cachevalue = GetCacheInfo(detailoptionval);

                    foreach (var one in Directory.EnumerateFiles(folderpathval).Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
                    {
                        var purename = Path.GetFileNameWithoutExtension(one);
                        var jsonpath = one + ".json";
                        if (cachevalue.TryGetValue(purename, out var reviewInfo))
                        {
                            if (File.Exists(jsonpath))
                            {
                                var tagresult = Newtonsoft.Json.JsonConvert.DeserializeObject<TagResult>(File.ReadAllText(jsonpath));
                                if(tagresult.TagCropResults is not null)
                                {
                                    foreach (var item in tagresult.TagCropResults.Where(a => a.Tag == "口红膏体及本体"))
                                    {
                                        var img = SixLabors.ImageSharp.Image.Load(one, out var format);
                                        var re = getrectinfo(item.CropResult, img.Height, img.Width);

                                        var sku = reviewInfo.RateSku;
                                        var folder = Path.Combine(linkedtofolderpathval, sku);
                                        if (!Directory.Exists(folder))
                                        {
                                            Directory.CreateDirectory(folder);
                                        }
                                        var linkto = Path.Combine(folder, purename + ".jpg");
                                        if (!File.Exists(linkto))
                                        {
                                            try
                                            {
                                                var nimage = img.Clone(i => i.Crop(new SixLabors.ImageSharp.Rectangle((int)re.xmin, (int)re.ymin, (int)(re.xmax - re.xmin), (int)(re.ymax - re.ymin))));
                                                using var writeimgstream = File.Create(linkto);
                                                nimage.Save(writeimgstream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                                            }
                                            catch
                                            {
                                                File.Copy(one, linkto);
                                                Console.WriteLine($"{one} invaild!");
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                    return 0;
                });

            });
            _ = app.Command("yologenbrand", (command) =>
               {
                   var folderlist = command.Argument("folderlist", string.Empty, true);
                   var trainpercent = command.Argument("trainpercent", "min is 0 and max is 100");
                   var exceptemptyoption = command.Option("--exceptempty", string.Empty, CommandOptionType.NoValue);
                   var outfolder = command.Argument("outfolder", string.Empty);
                   command.OnExecute(() =>
                   {
                       var folderlistval = folderlist.Values.Select(a =>
                       {
                           var index = a.IndexOf(';');
                           if (index < 0)
                               throw new Exception();
                           return (name: int.Parse(a[0..index]), path: a[(index + 1)..]);
                       }).ToList();
                       var trainpercentval = double.Parse(trainpercent.Value, CultureInfo.InvariantCulture);
                       var exceptemptyoptionval = exceptemptyoption.HasValue();
                       var outfolderval = outfolder.Value;
                       var outimagefolder = Path.Combine(outfolderval, "images");
                       var outlabelfolder = Path.Combine(outfolderval, "labels");
                       if (!Directory.Exists(outimagefolder))
                           Directory.CreateDirectory(outimagefolder);
                       if (!Directory.Exists(outlabelfolder))
                           Directory.CreateDirectory(outlabelfolder);
                       foreach (var (foldername, folderpath) in folderlistval)
                       {
                           var filelist = Directory.EnumerateFiles(folderpath).ToList();
                           List<string> finallist;
                           if (exceptemptyoptionval)
                           {
                               var jsonlist = filelist.Where(a => a.EndsWith(".json", StringComparison.OrdinalIgnoreCase)).Select(a => a[..^5]).ToHashSet();
                               finallist = filelist.Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && jsonlist.Contains(a)).Shuffle().ToList();
                           }
                           else
                           {
                               finallist = filelist.Where(a => a.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)).Shuffle().ToList();
                           }
                           var traincountk = trainpercentval / 100.0;
                           var traincount = (int)(traincountk * finallist.Count);
                           var trainset = finallist.Take(traincount).ToList();
                           var valset = finallist.Skip(traincount).ToList();
                           foreach (var imgpath in trainset.Concat(valset))
                           {
                               if (File.Exists(imgpath))
                               {
                                   var imgfilenamepure = Path.GetFileNameWithoutExtension(imgpath);
                                   var imgfilename = Path.GetFileName(imgpath);
                                   var linkto = Path.Combine(outimagefolder, imgfilename);
                                   if (!File.Exists(linkto))
                                   {
                                       LostTech.IO.Links.Symlink.CreateForFile(imgpath, linkto);
                                   }
                                   var imgjsonpath = imgpath + ".json";
                                   var txtfilepath = Path.Combine(outlabelfolder, imgfilenamepure + ".txt");
                                   if (File.Exists(imgjsonpath))
                                   {
                                       var imginfo = SixLabors.ImageSharp.Image.Identify(imgpath);
                                       var tagresult = Newtonsoft.Json.JsonConvert.DeserializeObject<TagResult>(File.ReadAllText(imgjsonpath));
                                       if (tagresult != null && tagresult.TagCropResults != null)
                                       {
                                           var str = new StringBuilder();
                                           foreach (var item in tagresult.TagCropResults.Where(a=> a.Tag is "口红本体" or "口红膏体及本体"))
                                           {
                                               var re = getrectinfo(item.CropResult, imginfo.Height, imginfo.Width);
                                               var xcenter = (re.xmax + re.xmin) / 2.0;
                                               var ycenter = (re.ymax + re.ymin) / 2.0;
                                               var width = re.xmax - re.xmin;
                                               var height = re.ymax - re.ymin;
                                               var index = foldername;
                                               str.AppendLine($"{index} {xcenter / imginfo.Width} {ycenter / imginfo.Height} {width / imginfo.Width} {height / imginfo.Height}");
                                           }
                                           File.WriteAllText(txtfilepath, str.ToString());
                                       }
                                   }
                               }
                           }
                       }
                       return 0;
                   });
               });
            return app.Execute(args);
        }
    }
}