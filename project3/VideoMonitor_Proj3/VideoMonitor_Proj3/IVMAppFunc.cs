using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.InterfaceClass("2`1", "VMAppFunc")]
    public interface IVMAppFunc : QS.Fx.Interface.Classes.IInterface
    {
        [QS.Fx.Reflection.Operation("Ready")]
        void Ready();

        [QS.Fx.Reflection.Operation("RecieveFrame")]
        void RecieveFrame(VMImage frame, FrameID id, string origID);

        [QS.Fx.Reflection.Operation("RecieveCommand")]
        void RecieveCommand(VMAddress src, string rfc_command, Parameter[] parameters, string origID);

        //should return the VMService object of the local service
        [QS.Fx.Reflection.Operation("GetLocalService")]
        VMService GetLocalService(string origID);

        [QS.Fx.Reflection.Operation("GetRemoteServices")]
        VMService[] GetRemoteServices(string origID); 

    }
}
