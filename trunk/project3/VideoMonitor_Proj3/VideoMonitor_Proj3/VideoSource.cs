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
        }

        private QS.Fx.Endpoint.Internal.IExportedUI uiendpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<IVMCommInt, IVMAppFunc> streamEndPoint;
        private QS.Fx.Endpoint.IConnection sourceConnection;

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.uiendpoint; }
        }

        #endregion


        #region IVMAppFunc Members


        void IVMAppFunc.RecieveFrame(Image frame, FrameID id, string origID)
        { // ignore
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, Parameter[] parameters, string origID)
        {
            throw new NotImplementedException();
        }

        VMService IVMAppFunc.GetLocalService(string origID)
        {
            return new VMService(null, VMService.ServiceType.SVC_TYPE_VIDEO_SOURCE, VMService.AvailService.SVC_AVAIL_VIDEO_SOURCE, null);
        }

        VMService[] IVMAppFunc.GetRemoteServices(string origID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
