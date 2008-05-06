using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VideoMonitor_Proj3
{
    using VMid = System.Int32;

    public class messageType
    {
        //Simple Frame Packet
        public const int MSG_TYPE_SEND_FRAME = 1;
        //Simple Control Message
        public const int MSG_TYPE_CONTROL_RFC = 2;
        //Message to Expose local service to the network
        public const int MSG_TYPE_EXPOSE_SVC = 4;
        //Message sent to parent to test that it still exists in the network
        public const int MSG_TYPE_CONFIRM_LIVE = 8;
        //Message sent from parent to confirm that it is alive
        public const int MSG_TYPE_RESPOND_LIVE = 16;
        //Message sent from new node to parent in network to request the network layout
        public const int MSG_TYPE_REQUEST_NETWORK = 32;
        //Message sent to a new node with payload of network layout
        public const int MSG_TYPE_RESPOND_NETWORK = 64;

    }

    [QS.Fx.Reflection.ValueClass("1`1", "VMMessage")]
    public sealed class VMMessage
    {
        public VMMessage(int type, int id, DateTime sent, VMParameter[] parameters, string rfc_command, VMImage image, FrameID fid, VMService service, VMNetwork network, VMAddress srcAddr, VMAddress dstAddr, int count, int max_count)
        {
            this.type = type;
            this.id = id;
            this.sent = sent;
            this.parameters = parameters;
            this.rfc_command = rfc_command;
            this.image = image;
            this.service = service;
            this.network = network;
            this.srcAddr = srcAddr;
            this.dstAddr = dstAddr;
            this.fid = fid;
            this.count = count;
            this.max_count = max_count;
        }

        public VMMessage()
        {
        }
        //Message identifiers
        [XmlElement]
        public int id; //message id
        [XmlElement]
        public int count; //represents the sub-id of a message, or the number of times it has attempted to send
        [XmlElement]
        public int max_count; //maximum number of times the message will try
        [XmlElement]
        public int type; //message type
        [XmlAttribute]
        public DateTime sent; //sent time
        
        //Optional payloads
        [XmlElement]
        public string rfc_command;  //network command
        [XmlAttribute]
        public VMParameter[] parameters; //command parameters
        [XmlElement]
        public VMImage image; //image frame
        [XmlAttribute]
        public FrameID fid; //id for image frame
        [XmlAttribute]
        public VMNetwork network; //network model
        [XmlAttribute]
        public VMService service; //availiable service model

        //Message SRC/DEST info
        [XmlAttribute]
        public VMAddress srcAddr;
        [XmlAttribute]
        public VMAddress dstAddr;

    }

    [QS.Fx.Reflection.ValueClass("9`1", "VMParameter")]
    public sealed class VMParameter
    {
        public VMParameter(string name, string val)
        {
            this.name = name;
            this.val = val;
        }
        public VMParameter()
        {

        }
        [XmlElement]
        public string name;
        [XmlElement]
        public string val;
    }

    [QS.Fx.Reflection.ValueClass("4`1", "VMAddress")]
    public sealed class VMAddress
    {
        public VMAddress(VMid[] id)
        {
            this.id = id;
        }

        public VMAddress()
        {

        }
        [XmlAttribute]
        public VMid[] id; //source or destination address.. can be multi-part... next/prev should be in id[0] and should be updated/routed accordingly

        [XmlAttribute]
        public bool notSet;
    }

    //null class to force ignore of checkpoint packets
    [QS.Fx.Reflection.ValueClass("5`1", "NullC")]
    public sealed class NullC
    {
        public NullC()
        {
        }
    }

    [QS.Fx.Reflection.ValueClass("6`1", "FrameID")]
    public sealed class FrameID
    {
        public FrameID(DateTime time, int id)
        {
            this.time = time;
            this.id = id;
        }

        public FrameID()
        {
         
        }
        [XmlAttribute]
        public DateTime time;
        [XmlElement]
        public int id;
         
    }

    [QS.Fx.Reflection.ValueClass("7`1", "VMNetwork")]
    public sealed class VMNetwork
    {
        public VMNetwork(VMService[] services)
        {
            this.services = services;
        }

        public VMNetwork()
        {
               
        }
        [XmlAttribute]
        public VMService[] services; //local services

    }

    [QS.Fx.Reflection.ValueClass("8`1", "VMService")]
    public sealed class VMService
    {
        public VMService(VMAddress addr, int svc_type, int svc_avail, VMService subServices)
        {
            this.svc_type = svc_type;
            this.svc_avail = svc_avail;
        }

        public VMService()
        {

        }

        public class ServiceType
        {
            //video source service provider
            public const int SVC_TYPE_VIDEO_SOURCE = 1;
            //video server service provider
            public const int SVC_TYPE_VIDEO_SERVER = 2;
            //video viewer service provider
            public const int SVC_TYPE_VIDEO_VIEWER = 4;
        }

        public class AvailService
        {
            //video source services
            public const int SVC_AVAIL_VIDEO_SOURCE = 1; //video camera source
            public const int SVC_AVAIL_PZT_CAMERA_C = 2; //pan-zoom-tilt compatable camera
            //video server services
            public const int SVC_AVAIL_VIDEO_MUX_DX = 4; //video / command mux
            public const int SVC_AVAIL_VFRAME_CNTRL = 8; //frame rate control
            //video viewer services
            public const int SVC_AVAIL_VIEWER_USR_C = 16; //viewer availiable
        }
        [XmlAttribute]
        public VMAddress svc_addr; //service address for re-refrence

        [XmlElement]
        public int svc_type;  //local service type

        [XmlElement]
        public int svc_avail; //availiable sub-services

        [XmlAttribute]
        public VMService[] subServices; //for server element, holds services of adjacent channel availiable
    }

    //deligate callback type for alarms
    public delegate void VMAlarmCallback(VMParameter[] parameters);

    public sealed class VMAlarm
    {
        public VMAlarm(int type, DateTime expires, int delay, VMMessage toSend, VMAlarmCallback callback, VMParameter[] callbackParams, bool repeats)
        {
            this.expires = expires;
            this.type = type;
            this.message = toSend;
            this.repeats = repeats;
            this.delay = delay;
            if (callback != null)
            {
                this.callback = new VMAlarmCallback(callback);
                this.callbackParams = callbackParams;
           }
           id = System.Guid.NewGuid().ToString();
        }

        public VMAlarm()
        {
            id = System.Guid.NewGuid().ToString();
        }

        public class AlarmType
        {
            public const int ALM_TYPE_MESSAGE = 1; //sends out message in message
            public const int ALM_TYPE_CALLBCK = 2; //calls function given

            public const int ALM_TYPE_CONTMSG = 4; //Contingent message, calls function only if max send count is exceeded;
        }

        public int type; //type of alarm

        public DateTime expires; //expiration of alarm

        public int delay; //in ms : delay of alarm (only necissary if repeats)

        public VMMessage message; //message to be sent after alarm expires if not removed

        public VMAlarmCallback callback; //callback to be called on expiration

        public VMParameter[] callbackParams; //parameters to be passed to callback function

        public bool repeats; //if alarm is to repeat

        public string id;
    }

    public class AlarmComparer : System.Collections.IComparer
    {

        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            return DateTime.Compare(((VMAlarm)y).expires, ((VMAlarm)x).expires); // we want y to be bigger than x, thus x happens sooner
        }

    }

    public class ServiceComparer : System.Collections.IComparer
    {

        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            return ((VMService)y).svc_addr.id[0] - ((VMService)x).svc_addr.id[0]; // we want y to be bigger than x, thus x happens sooner
        }

    }

}
