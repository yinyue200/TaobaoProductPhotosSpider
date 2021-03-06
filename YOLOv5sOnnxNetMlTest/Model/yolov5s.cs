using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YOLOv5sOnnxNetMlTest.Model
{
    class Yolov5s : YoloModel
    {
        public override int Width { get; set; } = 640;
        public override int Height { get; set; } = 640;
        public override int Depth { get; set; } = 3;

        public override int Dimensions { get; set; } = 10;

        public override float[] Strides { get; set; } = new float[] { 8, 16, 32 };

        public override float[][][] Anchors { get; set; } = new float[][][]
        {
            new float[][] { new float[] { 010, 13 }, new float[] { 016, 030 }, new float[] { 033, 023 } },
            new float[][] { new float[] { 030, 61 }, new float[] { 062, 045 }, new float[] { 059, 119 } },
            new float[][] { new float[] { 116, 90 }, new float[] { 156, 198 }, new float[] { 373, 326 } }
        };

        public override int[] Shapes { get; set; } = new int[] { 80, 40, 20 };

        public override float Confidence { get; set; } = 0.20f;
        public override float MulConfidence { get; set; } = 0.25f;
        public override float Overlap { get; set; } = 0.45f;

        public override string[] Outputs { get; set; } = new[] { "output" };

        public override List<YoloLabel> Labels { get; set; } = new List<YoloLabel>()
        {
            new YoloLabel { Id = 1, Name = "person" },
            new YoloLabel { Id = 2, Name = "bicycle" },
            new YoloLabel { Id = 3, Name = "car" },
            new YoloLabel { Id = 4, Name = "motorcycle" },
            new YoloLabel { Id = 5, Name = "airplane" },
        };

        public override bool UseDetect { get; set; } = true;
    }
}
