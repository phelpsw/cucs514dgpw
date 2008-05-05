﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

        void IVMAppFunc.Ready()
        {
            throw new NotImplementedException();
        }

        void IVMAppFunc.RecieveFrame(Image frame, FrameID id)
        {
            throw new NotImplementedException();
        }

        void IVMAppFunc.RecieveCommand(VMAddress src, string rfc_command, Parameter[] parameters)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}