using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YOLOv5sOnnxNetMlTest.Common
{
    static class RectangleFExt
    {
        public static float Area(this System.Drawing.RectangleF rect)
        {
            return rect.Height * rect.Width;
        }
    }
}
