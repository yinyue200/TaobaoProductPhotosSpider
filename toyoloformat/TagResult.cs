using System;
using System.Collections.Generic;
using System.Text;

namespace toyoloformat
{
    public class TagResult
    {
        public string Path { get; set; }
        public List<string> PhotosTags { get; set; }
        public string LastEditBy { get; set; }
        public List<TagCropResult> TagCropResults { get; set; }
    }
    public class TagCropResult
    {
        public string Tag { get; set; }
        public CropResult CropResult { get; set; }
    }
    public class CropResult
    {
        public double x { get; set; }
        public double y { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public double rotate { get; set; }
        public double scaleX { get; set; }
        public double scaleY { get; set; }
    }
}
