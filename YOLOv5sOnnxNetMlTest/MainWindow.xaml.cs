using Microsoft.ML;
using Microsoft.ML.Data;
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
using YOLOv5sOnnxNetMlTest.Model;
using DotNet.Collections.Generic;
using YOLOv5sOnnxNetMlTest.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing;
using System.IO;

namespace YOLOv5sOnnxNetMlTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Start();
        }
        void Start()
        {
            MLContext mlContext = new MLContext();

            var pipeline = mlContext.Transforms.LoadImages(outputColumnName: "image", imageFolder: "", inputColumnName: nameof(ImageNetData.ImagePath))
                .Append(mlContext.Transforms.ResizeImages(outputColumnName: "image", imageWidth: ImageNetSettings.imageWidth, imageHeight: ImageNetSettings.imageHeight, inputColumnName: "image"))
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "images",inputColumnName:"image"))
                .Append(mlContext.Transforms.ApplyOnnxModel(modelFile: @"C:\Users\yinyu\Source\Repos\yolov5\runs\train\kouhong_brand_test_2\weights\best.onnx",
                outputColumnNames: new[] { Yolov5ModelSettings.ModelOutput }, inputColumnNames: new[] { Yolov5ModelSettings.ModelInput }));

            var data = mlContext.Data.LoadFromEnumerable(new List<ImageNetData>());
            var model = pipeline.Fit(data);

            var image = ImageNetData.ReadFromFile(@"D:\reports\口红\【官方正品】MAC魅可全色号子弹头口红唇膏大牌女 小辣椒正红色\0ae3c97f679015e0ed98da7a98aa5c0e.jpg");
            var imagedata = SixLabors.ImageSharp.Image.Load(image.ImagePath);
            var realdata = mlContext.Data.LoadFromEnumerable(new ImageNetData[] { image });
            var result = model.Transform(realdata);
            //var 
            var boxes=processresult(result.GetColumn<float[]>("output").First(), new System.Drawing.Size(imagedata.Width, imagedata.Height));
            IPen pen = Pens.Solid(SixLabors.ImageSharp.Color.Green, 5);
            var newimg = imagedata.Clone(img =>
              {
                  foreach (var item in boxes)
                  {
                      img.DrawLines(pen, new PointF(item.Rect.X, item.Rect.Y), new PointF(item.Rect.X + item.Rect.Width, item.Rect.Y),
                          new PointF(item.Rect.X + item.Rect.Width, item.Rect.Y + item.Rect.Height), new PointF(item.Rect.X, item.Rect.Y + item.Rect.Height),new PointF(item.Rect.X, item.Rect.Y));
                  }
              });

            MemoryStream memoryStream = new MemoryStream();
            newimg.SaveAsBmp(memoryStream);

            imgctrl.Source = BitmapFrame.Create(memoryStream);
        }
        void nms(IReadOnlyList<System.Drawing.RectangleF> srcRects, List<System.Drawing.RectangleF> resRects, List<int> resIndexs,float thresh)
        {
            resRects.Clear();
            int size = srcRects.Count;
            if (size <= 0) return;
            // Sort the bounding boxes by the bottom - right y - coordinate of the bounding box
            List<(float, int)> idxs = new();
            for (var i = 0; i < size; ++i)
            {
                idxs.Add((srcRects[i].Y + srcRects[i].Height, i));
            }
            idxs.Sort((a, b) => Comparer<float>.Default.Compare(a.Item1, b.Item1));
            // keep looping while some indexes still remain in the indexes list
            while (idxs.Count > 0)
            {
                // grab the last rectangle
                var lastElem = idxs[idxs.Count - 1];
                var last = srcRects[lastElem.Item2];
                resIndexs.Add(lastElem.Item2);
                resRects.Add(last);
                idxs.RemoveAt(idxs.Count - 1);
                for(var posi=0; posi < idxs.Count; posi++)
                {
                    var pos = idxs[posi];
                    // grab the current rectangle
                    System.Drawing.RectangleF current = srcRects[pos.Item2];
                    float intArea = System.Drawing.RectangleF.Intersect(last,current).Area();
                    float unionArea = last.Area() + current.Area() - intArea;
                    float overlap = intArea / unionArea;
                    // if there is sufficient overlap, suppress the current bounding box
                    if (overlap > thresh)
                    {
                        idxs.RemoveAt(posi);
                    }
                    else ++posi;
                }
            }
        }
        List<DetectionBox> processresult(float[] output,System.Drawing.Size imgsize)
        {
            int size = 1 * 25200 * 10;
            int dimensions = 10; // 0,1,2,3 ->box,4->confidence，5-10 -> coco classes confidence 
            int rows = size / dimensions; //25200
            int confidenceIndex = 4;
            int labelStartIndex = 5;
            float modelWidth = 640.0f;
            float modelHeight = 640.0f;
            float xGain = modelWidth / imgsize.Width;
            float yGain = modelHeight / imgsize.Height;

            List<System.Numerics.Vector4> locations = new();
            List<int> labels = new();
            List<float> confidences = new();

            List<System.Drawing.RectangleF> src_rects = new();
            List<System.Drawing.RectangleF> res_rects = new();
            List<int> res_indexs = new();

            System.Drawing.RectangleF rect;
            System.Numerics.Vector4 location;
            for (int i = 0; i < rows; ++i)
            {
                int index = i * dimensions;
                if (output[index + confidenceIndex] <= 0.4f) continue;

                for (int j = labelStartIndex; j < dimensions; ++j)
                {
                    output[index + j] = output[index + j] * output[index + confidenceIndex];
                }

                for (int k = labelStartIndex; k < dimensions; ++k)
                {
                    if (output[index + k] <= 0.5f) continue;

                    location.X = (output[index] - output[index + 2] / 2) / xGain;//top left x
                    location.Y = (output[index + 1] - output[index + 3] / 2) / yGain;//top left y
                    location.Z = (output[index] + output[index + 2] / 2) / xGain;//bottom right x
                    location.W = (output[index + 1] + output[index + 3] / 2) / yGain;//bottom right y

                    locations.Add(location);

                    rect = new System.Drawing.RectangleF(location.X, location.Y,
                                    location.Z - location.X, location.W - location.Y);
                    src_rects.Add(rect);
                    labels.Add(k - labelStartIndex);


                    confidences.Add(output[index + k]);
                }

            }

            nms(src_rects, res_rects, res_indexs, 0.45f);

            List<DetectionBox> detectionBoxes = new(res_indexs.Count);
            for (int i = 0; i < res_indexs.Count; i++)
            {
                int res_index = res_indexs[i];
                detectionBoxes.Add(new DetectionBox() { Label = labels[res_index], Score = confidences[res_index], Rect = res_rects[i] });
            }
            return detectionBoxes;
        }
    }
}
