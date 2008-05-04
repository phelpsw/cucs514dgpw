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
    public partial class VideoSource : UserControl, QS.Fx.Object.Classes.IUI,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, SourceState>
    {
        public VideoSource(
            [QS.Fx.Reflection.Parameter("channel", QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<VMMessage, SourceState>> channel)
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
