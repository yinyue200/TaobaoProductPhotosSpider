using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YOLOv5sOnnxNetMlTest.Model
{
    class DetectionBox
    {
        public System.Drawing.RectangleF Rect { get; set; }
        public float Score { get; set; }
        public int Label { get; set; }
    }
}
