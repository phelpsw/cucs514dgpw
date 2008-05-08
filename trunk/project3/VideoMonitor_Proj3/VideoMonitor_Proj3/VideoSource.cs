using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ComponentClass("1`1","VideoSource","This component provides a video source and provides an endpoint for a VideoServer")]
    public partial class VideoSource : 
        UserControl,
        QS.Fx.Object.Classes.IUI,
        IVMAppFunc
    {
        public VideoSource(
            [QS.Fx.Reflection.Parameter("streamProcessor", QS.Fx.Reflection.ParameterClass.Value)] 
            QS.Fx.Object.IReference<IVideoStream> streamProcessor)
        {
            InitializeComponent();
            this.uiendpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
            this.streamEndPoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMCommInt, IVMAppFunc>(this);
            this.sourceConnection = this.streamEndPoint.Connect(streamProcessor.Object.VideoProcessor);
        
            // Define static image for testing
            /* several image sources: http://webcams.goedgeluk.nl/list.html */
            // Altadena ca, http://www.westphalfamily.com/webcam.jpg
            // sjsu: http://www.met.sjsu.edu/cam_directory/latest.jpg
            static_image = new VMImage();
            static_image.Picture = getNetworkImage("http://www.met.sjsu.edu/cam_directory/latest.jpg");
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> streamEndPoint;
        private QS.Fx.Endpoint.IConnection sourceConnection;

        private VMImage static_image;
        

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.uiendpoint; }
        }

        #endregion


        #region IVMAppFunc Members

        // ignore
        void IVMAppFunc.RecieveFrame(VMImage frame, FrameID id, string origID)
        { 
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, VMParameters parameters, string origID)
        {
            throw new NotImplementedException();
        }

        VMService IVMAppFunc.GetLocalService(string origID)
        {
            return new VMService(null, VMService.ServiceType.SVC_TYPE_VIDEO_SOURCE, VMService.AvailService.SVC_AVAIL_VIDEO_SOURCE, null);
        }

        VMServices IVMAppFunc.GetRemoteServices(string origID)
        {
            return null;
        }

        void IVMAppFunc.Ready()
        {
            throw new NotImplementedException();
        }

        void IVMAppFunc.OnNetworkUpdate(string origID)
        {
        }

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            // display what is transmitted by this channel
            pictureBox1.Image = static_image.Picture;

            // send frame on source channel
            this.streamEndPoint.Interface.SendFrame(static_image, new FrameID(DateTime.Now, 0, streamEndPoint.Interface.GetMyAddress()));
        }

        private void startStream_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }

        private void endStream_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
        }

        private Bitmap getNetworkImage(string sourceURL)
        {
            //string sourceURL = "http://webcam.mmhk.cz/axis-cgi/jpg/image.cgi";
            byte[] buffer = new byte[100000];
            int read, total = 0;
            // create HTTP request

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sourceURL);
            // get response

            WebResponse resp = req.GetResponse();
            // get response stream

            Stream stream = resp.GetResponseStream();
            // read data from stream

            while ((read = stream.Read(buffer, total, 1000)) != 0)
            {
                total += read;
            }
            // get bitmap

            Bitmap bmp = (Bitmap)Bitmap.FromStream(
                          new MemoryStream(buffer, 0, total));
            return bmp;
        }

    }
}
