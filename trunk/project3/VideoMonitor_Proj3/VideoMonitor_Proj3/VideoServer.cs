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
    public partial class VideoServer : UserControl, QS.Fx.Object.Classes.IUI
    {
        public VideoServer()
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
    }
}
