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
    [QS.Fx.Reflection.ComponentClass("3`1", "VideoViewer", "This component provides an interface to view selected indexed video sources")]
    public partial class VideoViewer : UserControl, QS.Fx.Object.Classes.IUI,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, ViewerState>
    {
        public VideoViewer(
            [QS.Fx.Reflection.Parameter("channel", QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<VMMessage, ViewerState>> channel)
        {
            InitializeComponent();
            this.internal_endpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
        }

        private QS.Fx.Endpoint.Internal.IExportedUI internal_endpoint;

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.internal_endpoint; }
        }

        #endregion

        #region ICheckpointedCommunicationChannelClient<VMMessage,ViewerState> Members

        ViewerState QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, ViewerState>.Checkpoint()
        {
            throw new NotImplementedException();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, ViewerState>.Initialize(ViewerState _checkpoint)
        {
            throw new NotImplementedException();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, ViewerState>.Receive(VMMessage _message)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
