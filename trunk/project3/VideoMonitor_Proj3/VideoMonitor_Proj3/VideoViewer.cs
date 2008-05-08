using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ComponentClass("3`1", "VideoViewer", "This component provides an interface to view selected indexed video sources")]
    public partial class VideoViewer : 
        UserControl, 
        QS.Fx.Object.Classes.IUI,
        IVMAppFunc
    {
        public VideoViewer(
            [QS.Fx.Reflection.Parameter("streamProcessor", QS.Fx.Reflection.ParameterClass.Value)] 
            QS.Fx.Object.IReference<IVideoStream> streamProcessor)
        {
            InitializeComponent();
            this.uiendpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
            this.streamEndPoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMCommInt, IVMAppFunc>(this);
            this.viewerConnection = this.streamEndPoint.Connect(streamProcessor.Object.VideoProcessor);
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> streamEndPoint;
        private QS.Fx.Endpoint.IConnection viewerConnection;

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.uiendpoint; }
        }

        #endregion

        #region IVMAppFunc Members

        int frames_rcv = 0;

        double total_tme = 0.0;

        void IVMAppFunc.RecieveFrame(VMImage frame, FrameID id, string origID)
        {

            if (treeView1.SelectedNode != null)
            {
                if((int)treeView1.SelectedNode.Tag == id.src.id[0])
                    pictureBox1.Image = frame.Picture;

                total_tme += DateTime.Now.Subtract(id.time).Milliseconds;

                frames_rcv++;

                textBox1.Text = "";

                textBox1.Text += "Total Frames:\r\n\t"+frames_rcv.ToString()+"\r\n";

                textBox1.Text += "Average Time To Send\r\n\t"+Math.Round((total_tme/frames_rcv),4).ToString()+"";
            }
            // buffer image
            // use timer to grab from buffer
            // handle ordering during buffer insert possibly
            
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, VMParameters parameters, string origID)
        {
            throw new NotImplementedException();
        }

        VMService IVMAppFunc.GetLocalService(string origID)
        {
            return new VMService(null, VMService.ServiceType.SVC_TYPE_VIDEO_VIEWER, VMService.AvailService.SVC_AVAIL_VIEWER_USR_C, null);
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
            renderServiceTree(streamEndPoint.Interface.GetNetworkServices().services);
        }

        #endregion

        private void renderServiceTree(VMService[] services)
        {
            this.BeginInvoke((ThreadStart)delegate()
            {
                treeView1.Nodes.Clear();
                foreach (VMService svc in services)
                {
                    TreeNode mainNode = new TreeNode();
                    mainNode.Text = "[" + svc.svc_addr.id[0].ToString() + "] " + svc.printServiceType(svc.svc_type);
                    mainNode.ToolTipText = svc.printAvailableServices(svc.svc_avail);
                    mainNode.Tag = svc.svc_addr.id[0];
                    treeView1.Nodes.Add(mainNode);
                }
            });
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            //clear statistics
            frames_rcv = 0;
            total_tme = 0.0;

            if (pictureBox1.Image != null)
            {
                pictureBox1.Image = null;
                pictureBox1.Invalidate();
            }
        }
    }
}
