using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VideoMonitor_Proj3
{
    class TypeClasses
    {
    }

    public class messageTypes
    {
        //Type for Simple Chat Send
        public const int MSG_TYPE_SEND_FRAME = 1;
        //Type for Lock Request
        public const int MSG_TYPE_CONTROL_RFC = 2;
        //Message that exposes all nodes to a new node, indicating the number in the set
        public const int MSG_TYPE_EXPOSE = 4;
    }

    [QS.Fx.Reflection.ValueClass("1`1", "VMMessage")]
    public sealed class VMMessage
    {
        public VMMessage(int type, DateTime sent, Parameter[] parameters, string rfc_command, Image image, AddressClass srcAddr, AddressClass dstAddr)
        {
            this.type = type;
            this.sent = sent;
            this.parameters = parameters;
            this.rfc_command = rfc_command;
            this.image = image;
            this.srcAddr = srcAddr;
            this.dstAddr = dstAddr;
        }

        public VMMessage()
        {
        }


        [XmlAttribute]
        public int type; //message type
        [XmlAttribute]
        public DateTime sent; //sent time
        [XmlAttribute]
        public Parameter[] parameters;
        [XmlAttribute]
        public string rfc_command;
        [XmlElement]
        public VMImage image;
        [XmlElement]
        public AddressClass srcAddr;
        [XmlElement]
        public AddressClass dstAddr;

    }

    public class VMImage
    {

    }

    public class Parameter
    {
        string description;
        int param;
    }

    public class AddressClass
    {
        int[] id;
        int type;
    }
}
