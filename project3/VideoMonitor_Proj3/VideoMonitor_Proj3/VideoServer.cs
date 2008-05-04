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
    [QS.Fx.Reflection.ComponentClass("2`1", "VideoServer", "This component provides a centralized source index and source distributor")]
    public partial class VideoServer : UserControl, QS.Fx.Object.Classes.IUI,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, ViewerState>,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, SourceState>
    {
        public VideoServer(
            [QS.Fx.Reflection.Parameter("sourceChannel", QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<VMMessage, SourceState>> sourceChannel,
            [QS.Fx.Reflection.Parameter("viewChannel", QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<VMMessage, ViewerState>> viewChannel)
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

        #region ICheckpointedCommunicationChannelClient<VMMessage,SourceState> Members

        SourceState QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, SourceState>.Checkpoint()
        {
            throw new NotImplementedException();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, SourceState>.Initialize(SourceState _checkpoint)
        {
            throw new NotImplementedException();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, SourceState>.Receive(VMMessage _message)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
