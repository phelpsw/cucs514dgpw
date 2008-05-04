using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ComponentClass("1`1","VideoSource","This component provides a video source and provides an endpoint for a VideoServer")]
    public partial class VideoSource : UserControl
    {
        public VideoSource()
        {
            InitializeComponent();
        }
    }
}
