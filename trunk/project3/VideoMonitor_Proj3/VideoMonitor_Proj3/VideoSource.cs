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
using AviFile;
using System.Reflection;

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
            // eiffel tower: http://www.parislive.net/eiffelwebcam1.jpg

            // AVI Video source: http://www.charleslindbergh.com/movies/index.asp

            static_image = new VMImage();
            static_image.Picture = getNetworkImage("http://www.parislive.net/eiffelwebcam1.jpg");
            frameindex = 0;
            
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> streamEndPoint;
        private QS.Fx.Endpoint.IConnection sourceConnection;

        private VMImage static_image;
        private AviManager aviManager;
        private int frameindex;

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

        // Howto render WMV Frame by Frame: http://www.codeproject.com/KB/audio-video/avifilewrapper.aspx
        private void timer1_Tick(object sender, EventArgs e)
        {
            frameindex++;
            VMImage frame = new VMImage();

            if (optsrcWebcam.Checked)
            {
                static_image.Picture = getNetworkImage("http://www.parislive.net/eiffelwebcam1.jpg");
                pictureBox1.Image = static_image.Picture;
                frame.Picture = static_image.Picture;
            }
            else if (optsrcWMV.Checked)
            {
                
                VideoStream aviStream = this.aviManager.GetVideoStream();
                aviStream.GetFrameOpen();

                if (frameindex >= aviStream.CountFrames)
                    frameindex = 0;

                frame.Picture = aviStream.GetBitmap(Convert.ToInt32(frameindex));
                aviStream.GetFrameClose();
                
            }
            else
            {
                return;
            }

            // display what is transmitted
            pictureBox1.Image = frame.Picture;
            // send frame on sourceframe.Picture channel
            this.streamEndPoint.Interface.SendFrame(frame, new FrameID(DateTime.Now, frameindex, streamEndPoint.Interface.GetMyAddress()));
        }

        private void startStream_Click(object sender, EventArgs e)
        {
            
            // prompt for wmv file to display
            if (optsrcWMV.Checked)
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    aviManager = new AviManager(openFileDialog1.FileName, true);
                    timer1.Enabled = true;
                    streamGroupBox1.Enabled = false;
                }
            }
            else if (optsrcWebcam.Checked)
            {
                timer1.Enabled = true;
                streamGroupBox1.Enabled = false;
            }
        }

        

        private void endStream_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            streamGroupBox1.Enabled = true;
        }

        private Bitmap getNetworkImage(string sourceURL)
        {

            byte[] buffer = new byte[100000];
            int read, total = 0;
            Stream stream;
            // create HTTP request
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(sourceURL);
                // get response

                WebResponse resp = req.GetResponse();
                // get response stream

                stream = resp.GetResponseStream();
                // read data from stream
            }
            catch (Exception)
            {
                return new Bitmap(1, 1);
                /* Error image
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://www.williamslabs.com/error.jpg");
                    // get response

                    WebResponse resp = req.GetResponse();
                    // get response stream

                    stream = resp.GetResponseStream();
                }
                catch (Exception)
                {
                    return new Bitmap(1,1);
                }
                 */
            }

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
