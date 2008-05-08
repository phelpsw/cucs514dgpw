using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.InterfaceClass("1`1", "VMCommInt")]
    public interface IVMCommInt : QS.Fx.Interface.Classes.IInterface
    {
        [QS.Fx.Reflection.Operation("Ready")]
        bool Ready();

        [QS.Fx.Reflection.Operation("NetworkStats")]
        VMNetwork NetworkStats(); //returns entire network status and availiable services

        [QS.Fx.Reflection.Operation("SendFrame")]
        void SendFrame(VMImage frame, FrameID id);

        [QS.Fx.Reflection.Operation("SendGlobalCommand")]
        void SendGlobalCommand(string rfc_command, VMParameters parameters);

        [QS.Fx.Reflection.Operation("SendCommand")]
        void SendCommand(VMAddress dest, string rfc_command, VMParameters parameters);

        [QS.Fx.Reflection.Operation("SendLocalServices")]
        void SendLocalServices(VMService service);

        [QS.Fx.Reflection.Operation("GetNetworkServices")]
        VMServices GetNetworkServices();

        [QS.Fx.Reflection.Operation("GetInstanceID")]
        string GetInstanceID();

        [QS.Fx.Reflection.Operation("GetMyAddress")]
        VMAddress GetMyAddress();
    }
}
