using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace CS514_HW2
{
    public class messageTypes
    {
        //Type for Simple Chat Send
        public const int MSG_TYPE_SEND_MSG = 1;
        //Type for Lock Request
        public const int MSG_TYPE_LOCK     = 2;
        //Type for Lock Unlock / Update
        public const int MSG_TYPE_UNLOCK   = 4;
        //Message that exposes all nodes to a new node, indicating the number in the set
        public const int MSG_TYPE_EXPOSE   = 8;

        public const int MSG_TYPE_LOAD_TEST   = 16;
    }


    [QS.Fx.Reflection.ValueClass("1`1", "IMessage")]
    public sealed class IMessage
    {
        public IMessage(int type, DateTime sent, Lock Ilock, Message message,int value)
        {
            this.type = type;
            this.sent = sent;
            this.Ilock = Ilock;
            this.message = message;
            this.value = value;
        }

        public IMessage()
        {
        }

        [XmlAttribute]
        public int type; //message type
        [XmlAttribute]
        public DateTime sent; //sent time
        [XmlAttribute]
        public int value; //value to be sent if needed
        [XmlElement]
        public Lock Ilock; //lock to be sent (can be null)
        [XmlElement]
        public Message message; //message to be sent

    }

    [QS.Fx.Reflection.ValueClass("2`1", "IChatState")]
    public sealed class IChatState
    {
        public IChatState(Message[] messages, Lock[] locks,int nodes)
        {
            this.messages = messages;
            this.locks = locks;
            this.nodes = nodes;
        }

        public IChatState()
        {
        }

        [XmlAttribute]
        public int nodes; //number of nodes in the group (only for numbering, no decriment for leaving)
        [XmlElement]
        public Message[] messages; //list of messages in the link
        [XmlElement]
        public Lock[] locks; //list of active locks
    }

    [QS.Fx.Reflection.ValueClass("3`1", "Lock")]
    public sealed class Lock
    {
        public Lock(MessageId id,int startC,int endC,int time)
        {
            this.id = id;
            this.startC = startC;
            this.endC = endC;
            this.time = time;
        }

        public Lock()
        {
        }

        public string toString() {
            return id.toString() + ":" + startC.ToString() + ":" + endC.ToString() + ":" + time.ToString();
        }

        [XmlElement]
        public MessageId id; //message id to lock
        [XmlAttribute]
        public int startC, endC; //beginning & ending position of lock
        [XmlAttribute]
        public int time; //time to maintain lock for (in seconds)

        public DateTime expires; //time set locally for expiration of the lock
        /** Implementation Note:
         * I would like to note that while it was specified to have congruent expirations, for this context
         * where it is not 100% pertinent that a node observe the immediate timely expiration of a lock,
         * and clock syncrony is not gurenteed, it seems more prudent to allow each node to simply time out
         * a value after time step, thus the originating node will have relinquished the lock first, before
         * any others, and thus linarity and security of locks is preserved.
         **/
    }

    

    public class LockComparer : System.Collections.IComparer
    {

        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int System.Collections.IComparer.Compare(Object x, Object y)
        {
            return DateTime.Compare(((Lock)y).expires, ((Lock)x).expires); // we want y to be bigger than x, thus x happens sooner
        }

    }

    [QS.Fx.Reflection.ValueClass("4`1", "Message")]
    public sealed class Message
    {
        public Message(MessageId id, string text)
        {
            this.id = id;
            this.text = text;
        }

        public Message()
        {
        }

        [XmlElement]
        public MessageId id;
        [XmlAttribute]
        public string text;
    }

    [QS.Fx.Reflection.ValueClass("5`1", "MessageId")]
    public sealed class MessageId
    {
        public MessageId(int src_id, int message_id)
        {
            this.src_id = src_id;
            this.message_id = message_id;
        }

        public MessageId()
        {
        }

        public string toString() {
            return src_id.ToString() + ":" + message_id.ToString();
        }

        [XmlAttribute]
        public int src_id; //src id for node sending message
        [XmlAttribute]
        public int message_id; //message # from that src node
    }

}
