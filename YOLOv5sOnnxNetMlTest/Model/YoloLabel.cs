using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YOLOv5sOnnxNetMlTest.Model
{
    /// <summary>
    /// Label of detected object.
    /// </summary>
    public class YoloLabel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }

        public YoloLabel()
        {
            Color = Color.Green;
        }
    }
}
