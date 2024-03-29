﻿using System;
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
        //send checkup message  //contains network of root node
        public const int MSG_TYPE_CHECKUP_MESSAGE = 128;
        //send checkup response //contains a network w/ services to add
        public const int MSG_TYPE_CHECKUP_RESPOND = 256;
        //send message to remove a dead node from the network
        public const int MSG_TYPE_REMOVE_DEAD = 512;

    }

    [QS.Fx.Reflection.ValueClass("1`1", "VMMessage")]
    public sealed class VMMessage
    {
        public VMMessage(int type, int id, DateTime sent, VMParameters parameters, string rfc_command, VMImage image, FrameID fid, VMService service, VMNetwork network, VMAddress srcAddr, VMAddress dstAddr, int count, int max_count)
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
        [XmlElement]
        public VMParameters parameters; //command parameters
        [XmlElement]
        public VMImage image; //image frame
        [XmlElement]
        public FrameID fid; //id for image frame
        [XmlElement]
        public VMNetwork network; //network model
        [XmlElement]
        public VMService service; //availiable service model

        //Message SRC/DEST info
        [XmlElement]
        public VMAddress srcAddr;
        [XmlElement]
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

    [QS.Fx.Reflection.ValueClass("11`1", "VMParameters")]
    public sealed class VMParameters
    {
        public VMParameters(VMParameter[] parameters)
        {
            this.parameters = parameters;
        }
        public VMParameters()
        {
        }

        [XmlElement]
        public VMParameter[] parameters;
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
        [XmlElement]
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
        public FrameID(DateTime time, int id, VMAddress src)
        {
            this.time = time;
            this.id = id;
            this.src = src;
        }

        public FrameID()
        {

        }
        [XmlAttribute]
        public DateTime time;
        [XmlElement]
        public int id;
        [XmlElement]
        public VMAddress src;

    }


    [QS.Fx.Reflection.ValueClass("7`1", "VMNetwork")]
    public sealed class VMNetwork
    {
        public VMNetwork(VMServices services)
        {
            this.services = services;
        }

        public VMNetwork()
        {
               
        }

        //string comparer object
        private ServiceComparer scmp = new ServiceComparer();

        //get a service's index by it's address
        public VMService getServicesByID(VMAddress addr)
        {
            if (addr == null) return null;
            foreach (VMService svc in services.serviceSet.ToArray())
            {
                if (svc.svc_addr.id[0] == addr.id[0])
                {
                    return svc;
                }
            }
            return null; //not found
        }

        //remove a service by it's service object
        public bool removeService(VMService service)
        {
            foreach (VMService svc in services.services)
            {
                if (svc.svc_addr.id[0] == service.svc_addr.id[0])
                {
                    services.serviceSet.Remove(svc);
                    return true;
                }
            }
            return false; //not found
        }

        //remove a service by it's address
        public bool removeService(VMAddress addr)
        {
            foreach (VMService svc in services.services)
            {
                if (svc.svc_addr.id[0] == addr.id[0])
                {
                    services.serviceSet.Remove(svc);
                    return true;
                }
            }
            return false; //not found
        }

        //adds a service to the network, returns true if service existed previously, false else
        public bool addService(VMService service) {
            VMService exist = getServicesByID(service.svc_addr);
            //if service currently exists, remove it
            if (exist != null) 
                removeService(service);

            //add new service to end of list
            this.services.serviceSet.Add(service);

            //resort the array
            services.serviceSet.Sort(scmp);

            if (exist != null) return true;
            return false;
        }

        //get root element of the array
        public VMService root()
        {
            return services.serviceSet.First();
        }

        //get tail element of the array
        public VMService tail()
        {
            return services.serviceSet.Last();
        }

        //get parent element of a given address
        public VMService parent(VMAddress myaddr)
        {
            int myindex = services.serviceSet.IndexOf(getServicesByID(myaddr));
            return services.serviceSet[(myindex==0?services.serviceSet.Count()-1:myindex-1)];
        }

        [XmlElement]
        public VMServices services; //local services

    }

    [QS.Fx.Reflection.ValueClass("8`1", "VMService")]
    public sealed class VMService
    {
        public VMService(VMAddress addr, int svc_type, int svc_avail, VMServices subServices)
        {
            this.svc_addr = addr;
            this.svc_type = svc_type;
            this.svc_avail = svc_avail;
            this.subServices = subServices;
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

        public string printServiceType(int val)
        {
            switch (val)
            {
                case ServiceType.SVC_TYPE_VIDEO_SOURCE:
                    return "Source";
                case ServiceType.SVC_TYPE_VIDEO_SERVER:
                    return "Server";
                case ServiceType.SVC_TYPE_VIDEO_VIEWER:
                    return "Viewer";
                default:
                    return "Unknown Type";
            }
        }

        public string printAvailableServices(int val)
        {
            string output = "";
            if ((val | AvailService.SVC_AVAIL_VIDEO_SOURCE) > 0) // stupid c# doesn't treat this as a boolean
                output += "Video Source ";
            if ((val | AvailService.SVC_AVAIL_PZT_CAMERA_C) > 0)
                output += "Pan Zoom Tilt Camera ";
            if ((val | AvailService.SVC_AVAIL_VFRAME_CNTRL) > 0)
                output += "Frame Rate Control ";
            if ((val | AvailService.SVC_AVAIL_VIDEO_MUX_DX) > 0)
                output += "Video Multiplexer ";
            if ((val | AvailService.SVC_AVAIL_VIEWER_USR_C) > 0)
                output += "Video Viewer ";
            return output;
        }


        [XmlElement]
        public VMAddress svc_addr; //service address for re-refrence

        [XmlElement]
        public int svc_type;  //local service type

        [XmlElement]
        public int svc_avail; //availiable sub-services

        [XmlElement]
        public VMServices subServices; //for server element, holds services of adjacent channel availiable
    }

    [QS.Fx.Reflection.ValueClass("12`1", "VMService")]
    public sealed class VMServices
    {
        public VMServices(VMService[] services)
        {
            serviceSet = new List<VMService>();
            this.services = services;
        }

        public VMServices()
        {
            serviceSet = new List<VMService>();
        }

        public List<VMService> serviceSet;

        [XmlElement]
        public VMService[] services
        {
            get { return serviceSet.ToArray(); }
            set { serviceSet.Clear(); foreach (VMService svc in value) serviceSet.Add(svc); }
        }
    }

    //deligate callback type for alarms
    public delegate void VMAlarmCallback(VMParameters parameters);

    public sealed class VMAlarm
    {
        public VMAlarm(int type, DateTime expires, int delay, VMMessage toSend, VMAlarmCallback callback, VMParameters callbackParams, bool repeats)
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

        public VMParameters callbackParams; //parameters to be passed to callback function

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

    public class ServiceComparer : System.Collections.Generic.IComparer<VMService>
    {

        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int System.Collections.Generic.IComparer<VMService>.Compare(VMService x, VMService y)
        {
            return x.svc_addr.id[0] - y.svc_addr.id[0];
        }

    }

}
