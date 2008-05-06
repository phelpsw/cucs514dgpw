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
            [QS.Fx.Reflection.Parameter("sourceStreamProcessor", QS.Fx.Reflection.ParameterClass.Value)] 
            QS.Fx.Object.IReference<IVideoStream> sourceStreamProcessor,
            [QS.Fx.Reflection.Parameter("viewerStreamProcessor", QS.Fx.Reflection.ParameterClass.Value)] 
            QS.Fx.Object.IReference<IVideoStream> viewerStreamProcessor)
        {
            InitializeComponent();
            this.uiendpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
            this.sourceStreamEndPoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMCommInt, IVMAppFunc>(this);
            this.viewerStreamEndPoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMCommInt, IVMAppFunc>(this);
            this.sourceConnection = this.sourceStreamEndPoint.Connect(sourceStreamProcessor.Object.VideoProcessor);
            this.viewerConnection = this.viewerStreamEndPoint.Connect(viewerStreamProcessor.Object.VideoProcessor);
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> sourceStreamEndPoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> viewerStreamEndPoint;
        private QS.Fx.Endpoint.IConnection sourceConnection;
        private QS.Fx.Endpoint.IConnection viewerConnection;

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.uiendpoint; }
        }

        #endregion

        #region IVMAppFunc Members


        void IVMAppFunc.RecieveFrame(VMImage frame, FrameID id, string origID)
        {
            if (origID == sourceStreamEndPoint.Interface.GetInstanceID())
                viewerStreamEndPoint.Interface.SendFrame(frame, id);
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, Parameter[] parameters, string origID)
        {
            throw new NotImplementedException();
        }

        VMService IVMAppFunc.GetLocalService(string origID)
        {
            return new VMService(null, VMService.ServiceType.SVC_TYPE_VIDEO_SERVER, 0, null);
        }

        VMService[] IVMAppFunc.GetRemoteServices(string origID)
        {
            if (origID == sourceStreamEndPoint.Interface.GetInstanceID())
                return sourceStreamEndPoint.Interface.GetNetworkServices();
            else if (origID == viewerStreamEndPoint.Interface.GetInstanceID())
                return viewerStreamEndPoint.Interface.GetNetworkServices();
            else
                return null; // we might want to throw an exception in this situation
        }

        void IVMAppFunc.Ready()
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}
