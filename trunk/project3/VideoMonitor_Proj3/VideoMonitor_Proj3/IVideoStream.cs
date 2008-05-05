using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ObjectClass("1`1", "VideoStream")]
    public interface IVideoStream : QS.Fx.Object.Classes.IObject
    {
        [QS.Fx.Reflection.Endpoint("VideoStream")]
        QS.Fx.Endpoint.Classes.IDualInterface<IVMAppFunc, IVMCommInt> VideoProcessor
        {
            get;
        }
    }
}
