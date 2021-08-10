using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace toyoloformat
{
    using SizeD = ValueTuple<double, double>;
    class Program
    {
        static (double xmin, double xmax, double ymin, double ymax) getrectinfo(CropResult cropResult,double imgh,double imgw)
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

            SizeD rotatexy(double x0,double y0,double xcenter,double ycenter)
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
            var app = new CommandLineApplication();
            _ = app.Command("convert", (command) =>
               {
                   var imglisttxt = command.Argument("txtlistloc", string.Empty);
                   var basefolderforimglist = command.Argument("basefolderforimglist", string.Empty);
                   var tofolderimages = command.Argument("tofolderimages", string.Empty);
                   var tofolderlabels = command.Argument("tofolderlabels", string.Empty);
                   command.OnExecute(() =>
                   {
                       var list = File.ReadAllLines(imglisttxt.Value);
                       var basefolderforimglistval = basefolderforimglist.Value;
                       var tofolderimagesval = tofolderimages.Value;
                       var tofolderlabelsval = tofolderlabels.Value;
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
                                   if(File.Exists(imgjsonpath))
                                   {
                                       var imginfo = SixLabors.ImageSharp.Image.Identify(imgpath);
                                       var tagresult = Newtonsoft.Json.JsonConvert.DeserializeObject<TagResult>(File.ReadAllText(imgjsonpath));
                                       if (tagresult != null && tagresult.TagCropResults != null)
                                       {
                                           var str = new StringBuilder();
                                           foreach (var item in tagresult.TagCropResults)
                                           {
                                               var re = getrectinfo(item.CropResult, imginfo.Height, imginfo.Width);
                                               var xcenter = (re.xmax + re.xmin) / 2.0;
                                               var ycenter = (re.ymax + re.ymin) / 2.0;
                                               var width = re.xmax - re.xmin;
                                               var height = re.ymax - re.ymin;

                                               str.AppendLine($"{Array.IndexOf(classlist, item.Tag)} {xcenter / imginfo.Width} {ycenter / imginfo.Height} {width / imginfo.Width} {height / imginfo.Height}");
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
            _ = app.Command("calc", (command) =>
               {
                   var folder = command.Argument("folder", string.Empty);
                   command.OnExecute(() =>
                   {
                       int missingtag = 0;
                       int backgroundimg = 0;
                       Dictionary<string, int> calctag = new Dictionary<string, int>();
                       foreach(var one in classlist)
                       {
                           calctag[one] = 0;
                       }
                       var folderval = folder.Value;
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
                                       calctag[crop.Tag]++;
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
            return app.Execute(args);
        }
    }
}
