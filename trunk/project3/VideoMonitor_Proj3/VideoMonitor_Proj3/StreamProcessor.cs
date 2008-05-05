using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LSCollections;

namespace VideoMonitor_Proj3
{
    [QS.Fx.Reflection.ComponentClass("2`1", "StreamProcessor")]
    public sealed class StreamProcessor :
        IVideoStream,
        IVMCommInt,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>
    {

         public StreamProcessor(
            [QS.Fx.Reflection.Parameter("channel", QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<
                QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<VMMessage, NullC>> channel)
        {
            this.interfaceEndpoint = QS.Fx.Endpoint.Internal.Create.DualInterface<IVMAppFunc, IVMCommInt>(this);
            this.channelendpoint = QS.Fx.Endpoint.Internal.Create.DualInterface<
                QS.Fx.Interface.Classes.ICheckpointedCommunicationChannel<VMMessage, NullC>,
                QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>>(this);
            this.channelconnection = this.channelendpoint.Connect(channel.Object.Channel);
        }

        private bool ready;

        private QS.Fx.Endpoint.Internal.IDualInterface<IVMAppFunc, IVMCommInt> interfaceEndpoint;
        private QS.Fx.Endpoint.Internal.IDualInterface<
            QS.Fx.Interface.Classes.ICheckpointedCommunicationChannel<VMMessage, NullC>,
            QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>> channelendpoint;
        private QS.Fx.Endpoint.IConnection channelconnection;

        QS.Fx.Endpoint.Classes.IDualInterface<IVMAppFunc, IVMCommInt> IVideoStream.VideoProcessor
        {
            get { return this.interfaceEndpoint; }
        }

       
        //Configure Message Behaviors Here
        enum VMMsgConfig {
            //Standard time between check for expired alarms
            alarmCheck = 100,

            //standard time between checks of parent
            parent_live_check_delay = 2000,
            //timeout on check responses before repeat
            parent_live_check_ttl = 200,
            //number of times to parent doesn't respond before listing as dead
            parent_live_check_count = 3,

            //time to wait for reponses to network status packet before repeat
            netowrk_model_wait_ttl = 200,
            //number of times to attempt request of status packet before marking dead and asking again
            netowrk_model_wait_count = 3

        }

        //if true: this node is currently the root-node of the system (only needed for circular watches)
        private bool amRoot = false;

        //number of nodes in the system
        private int nodes;

        //node number of this node
        private int myNodeNum = 0;

        //number of messages that have been sent by this node;
        private int messages = 0;
        
        //parent whom is checked on to make sure they are still live
        private int myParent = 0;

        //threading timer to handle calls to the timer class.
        private System.Threading.Timer timer = null;


        /** STRUCTURES TO HANDLE MESSAGE MANIPULATION AND FLOW **/

        //comparison operator for PriorityQueue
        private AlarmComparer acmp = new AlarmComparer();

        //message queue for incoming recieved messages
        private Queue<VMMessage> incoming = new Queue<VMMessage>();

        //Queue to allow timer timeouts on locks.
        private PriorityQueue alarmTimers;

        //List of Active Alarms (refrenced by Alarms.toString() method as keys
        private IDictionary<string, VMAlarm> alarmList = new Dictionary<string, VMAlarm>();


        //List of Viewable Messages (ordered by insertion)
        private List<VMMessage> messageList = new List<VMMessage>();

        //Index to Viewable Messages (not necissarily ordered correctly)
        private IDictionary<string, VMMessage> messageIndex = new Dictionary<string, VMMessage>();



        //When Connection to Channel is established, the following method will be called to set up the local connection
        void networkReady()
        {
            //broadcast query packet
            lock(messages) {
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_REQUEST_NETWORK,messages++,DateTime.Now,null,null,null,null,
                (messageTypes.MSG_TYPE_EXPOSE, DateTime.Now, null, null, (myNodeNum + 1)));
            }

        }


        //retrieve all of the locked items in a specific message
        public List<VMAlarm> getAlarmsByMessage(int msg_id)
        {
            List<VMAlarm> list = new List<VMAlarm>();

            foreach (VMAlarm l in alarmList.Values)
            {
                if (l.id.toString() == id.toString())
                {
                    list.Add(l);
                }
            }
            list.Sort(poscmp); // add back poscomp TODO
            return list;
        }
       
        /** TIMER FUNCTIONALITY FOR HANDLING RECURRING EVENTS AND MESSAGE RESPONSES **/

        //Invocation call to start the timer thread
        private void StartTimer()
        {
            if (timer == null) //timer not currently running, else, wait
            {
                System.Threading.TimerCallback cb = new System.Threading.TimerCallback(Timer);
                //begins new timer thread
                timer = new System.Threading.Timer(cb, null, 100, VMMsgConfig.alarmCheck);
            }
        }

        //call to halt the timer thread
        private void StopTimer()
        {
            timer.Dispose();
            timer = null;
        }

        //Timer Thread to Monitor lockTimers queue and 
        private void Timer(object obj)
        {
            lock (alarmTimers)
            {
                //remove all expired timers
                while (alarmTimers.Count != 0 && ((VMAlarm)alarmTimers.Peek()).expires.Subtract(DateTime.Now).Seconds < 0)
                {
                    //now re-send the message that has expired

                    //remove lock from the queue and from the lockList
                    alarmList.Remove(((VMAlarm)alarmTimers.Dequeue()).toString());
                }
                if (alarmList.Count == 0 && !editState) StopTimer(); //end the timer if none remain
            }
            //end, will be relaunched in delta* time
        }

        /** END TIMER FUNCTIONS **/

        /** DIGESTOR / MESSAGE HANDLER FUNCIONALITY FOR MANAGING MESSAGE ROUTING **/

        //METHOD TO BEGIN MESSAGE DIGESTER THREAD (runs on form thread)
        private void StartDigester()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.Digester), null);
            else
                this.Digester(null, null);
        }

        //Clears out the message queue of new meessages and routes them according to type and parameters
        private void Digester(object sender, EventArgs args)
        {
            lock (incoming)
            {
                while (incoming.Count > 0)
                {
                    IMessage msg = incoming.Dequeue();
                    switch (msg.type)
                    {
                        //Case for normal Chat Messages
                        case messageType.MSG_TYPE_SEND_MSG:
                            if (msg.message != null)
                            {
                                msg.message.recieveTime = DateTime.Now;
                                //simply add to the message list
                                messageList.Add(msg.message);
                                messageIndex.Add(msg.message.id.toString(), msg.message);
                            }
                            this.Refresh(null, null); //redraw for new message
                            break;

                        //Lock Request
                        case messageTypes.MSG_TYPE_LOCK:
                            if (msg.Ilock != null)
                            {
                                Lock exist;
                                if (lockList.TryGetValue(msg.Ilock.toString(), out exist))
                                {
                                    //does exist (remove current and replace w/ new) this allows leases to be extended
                                    lockList.Remove(msg.Ilock.toString());
                                    lockTimers.Remove(exist);
                                }
                                //initalize timer
                                msg.Ilock.expires = DateTime.Now.AddSeconds(msg.Ilock.time);

                                //add lock to the timers list
                                lockTimers.Enqueue(msg.Ilock);

                                //add lock to the indexed list
                                lockList.Add(msg.Ilock.toString(), msg.Ilock);

                                if (timer == null) StartTimer(); //begin the timer thread

                                this.Refresh(null, null); //redraw for new message
                            }
                            else
                            {
                                //bad lock? dunno
                                //-throw exception if needed
                            }
                            break;

                        //Unlock / Update 
                        case messageTypes.MSG_TYPE_UNLOCK:
                            if (msg.Ilock != null)
                            {
                                Lock exist;
                                if (lockList.TryGetValue(msg.Ilock.toString(), out exist))
                                {
                                    //does exist (remove current)
                                    lockList.Remove(msg.Ilock.toString());
                                    lockTimers.Remove(exist);

                                    //update message
                                    if (msg.message != null)
                                    {
                                        //rebuild text string
                                        string text = messageIndex[msg.Ilock.id.toString()].text;
                                        string begin = text.Substring(0, msg.Ilock.startC);
                                        string end = text.Substring(msg.Ilock.endC);
                                        string txt = begin + msg.message.text + end;
                                        //finally set value
                                        messageIndex[msg.Ilock.id.toString()].text = (txt != "" ? txt : " [message text deleted]");
                                    }
                                    else
                                    {
                                        //if message structure was null, delete this message
                                        messageList.Remove(messageIndex[msg.Ilock.id.toString()]);
                                        messageIndex.Remove(msg.Ilock.id.toString());
                                    }
                                }
                                else
                                {
                                    //lock may have previously expired... shouldn't happen but concievably could...
                                    //simply ignore in this case
                                }
                            }
                            else
                            {
                                //bad lock? dunno
                                //-throw exception
                            }
                            this.Refresh(null, null); //redraw for new message
                            break;

                        //Handles update to network size
                        case messageTypes.MSG_TYPE_EXPOSE:
                            if (msg.value != 0)
                                this.nodes = msg.value;
                            break;
                        case messageTypes.MSG_TYPE_LOAD_TEST:
                            richTextBox1.Text = msg.message.id.toString() + "\r\n" + msg.message.text;
                            break;
                        default:
                            //random message? ignore
                            break;
                    }
                }
            }
        }

        /** END DIGESTOR FUNCTIONS **/

        #region ICheckpointedCommunicationChannelClient<IMessage, IChatState> Members

        IChatState QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>.Checkpoint()
        {
            //does nothing...
            return new NullC();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>.Initialize(NullC _checkpoint)
        {
            //do nothing, simply use as alert that the network is now "ready" (connected)
            networkReady();
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>.Receive(VMMessage _message)
        {
            //upon receiving a message, simply throw it into the queue and let the digester pick it up
            if (this.InvokeRequired)
            {
                bool pendingcallback = incoming.Count > 0;
                lock (incoming)
                {
                    incoming.Enqueue(_message);
                }
                if (!pendingcallback)
                    this.StartDigester();
            }
        }

        #endregion
    }
}
