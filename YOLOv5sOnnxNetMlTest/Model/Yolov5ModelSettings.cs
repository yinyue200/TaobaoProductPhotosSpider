using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YOLOv5sOnnxNetMlTest.Model
{
    struct Yolov5ModelSettings
    {
        // for checking yolo Model input and  output  parameter names,
        // you can use tools like Netron, 
        // which is installed by Visual Studio AI Tools

        // input tensor name
        public const string ModelInput = "images";

        // output tensor name
        public const string ModelOutput = "output";
    }
}
