using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// IOutgoing = IVMAppFunc
// IIncoming = IVMCommInt
// IBank = IVideoStream

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ComponentClass("2`1", "VideoServer", "This component provides a centralized source index and source distributor")]
    public partial class VideoServer : 
        UserControl, 
        QS.Fx.Object.Classes.IUI,
        IVMAppFunc
    {

        public VideoServer(
            [QS.Fx.Reflection.Parameter("streamProcessor", QS.Fx.Reflection.ParameterClass.Value)] 
            QS.Fx.Object.IReference<IVideoStream> streamProcessor)
        {
            InitializeComponent();
            this.uiendpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
            this.streamEndPoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMCommInt, IVMAppFunc>(this);
            this.channelConnection = this.streamEndPoint.Connect(streamProcessor.Object.VideoProcessor);
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> streamEndPoint;
        private QS.Fx.Endpoint.IConnection channelConnection;

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.uiendpoint; }
        }

        #endregion

        #region IVMAppFunc Members


        void IVMAppFunc.RecieveFrame(VMImage frame, FrameID id, string origID)
        {
            textBox1.Text += "hello world";
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, VMParameters parameters, string origID)
        {
            throw new NotImplementedException();
        }

        VMService IVMAppFunc.GetLocalService(string origID)
        {
            return new VMService(null, VMService.ServiceType.SVC_TYPE_VIDEO_SERVER, 0, null);
        }

        VMServices IVMAppFunc.GetRemoteServices(string origID)
        {
            return streamEndPoint.Interface.GetNetworkServices();
        }

        void IVMAppFunc.Ready()
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}
