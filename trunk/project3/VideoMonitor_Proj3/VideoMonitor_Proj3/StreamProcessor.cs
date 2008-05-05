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

             //set local id
            this.instanceID = System.Guid.NewGuid().ToString();
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
        enum VMMsgConfig
        {
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

        public string instanceID = "";

        //if true: this node is currently the root-node of the system (only needed for circular watches)
        private bool amRoot = false;

        //if node has recieved network info packet yet
        private bool isInitialized = false;

        //local copy of the network configuration
        private VMNetwork network;

        //local address
        private VMAddress myAddress;

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

        //Index to Viewable Messages (not necissarily ordered correctly, but easily referenced)
        private IDictionary<string, VMMessage> messageIndex = new Dictionary<string, VMMessage>();



        //When Connection to Channel is established, the following method will be called to set up the local connection
        private void networkReady()
        {
            if (!isInitialized)
            {
                //broadcast network query message
                lock (messages)
                {
                    //create message
                    VMMessage msg = new VMMessage(messageType.MSG_TYPE_REQUEST_NETWORK, messages++, DateTime.Now, null, null, null, null, null, null, null, 1, VMMsgConfig.netowrk_model_wait_count);
                    //send message
                    this.channelendpoint.Interface.Send(msg);
                }
                //ad an alarm to resend the request the given number of times after waiting the specified time.

                AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CONTMSG, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                    DateTime.Now.AddMilliseconds(VMMsgConfig.netowrk_model_wait_ttl), //set delay
                    msg, setAsRoot, null)); //message and callback stuff
            }
        }

        //callback if no response is recieved from network request from networkReady
        public static void setAsRoot(Parameter[] parameters)
        {
            //initialize my address
            this.myAddress = new VMAddress(new int[] {0});

            VMService mySvc = this.interfaceEndpoint.Interface.GetLocalService(this.instanceID);
 
            mySvc.svc_addr = this.myAddress;

            mySvc.subServices = this.interfaceEndpoint.Interface.GetRemoteServices(this.instanceID);

            //initialize network object
            network = new VMNetwork(new VMService[] { mySvc });
        }

        //retrieve all of the alarms corrisponding to the given message id
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

        //Add an alarm and start the timer if not already
        private void AddAlarm(VMAlarm alarm)
        {
            //add alarm to the timers list
            alarmTimers.Enqueue(alarm);

            //add alarm to the indexed list
            alarmList.Add(alarm.ToString(), alarm);

            if (timer == null) StartTimer(); //timer not currently running, start it
        }

        //Timer Thread to Monitor lockTimers queue and 
        private void Timer(object obj)
        {
            lock (alarmTimers)
            {
                //remove all expired timers
                while (alarmTimers.Count != 0 && ((VMAlarm)alarmTimers.Peek()).expires.Subtract(DateTime.Now).Seconds < 0)
                {
                    VMAlarm alarm = (VMAlarm)alarmTimers.Dequeue();
                    //now route alarm
                    switch (alarm.type)
                    {
                        case VMAlarm.AlarmType.ALM_TYPE_MESSAGE:
                            VMMessage msg = alarm.message;
                            msg.sent++; //incriment sent count
                            break;

                        case VMAlarm.AlarmType.ALM_TYPE_CALLBCK:
                            break;
                    }

                    //remove alarm from the queue and from the listist
                    alarmList.Remove(alarm.toString());
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
                    VMMessage msg = incoming.Dequeue();
                    switch (msg.type)
                    {
                        //Normal Frame Recieved
                        case messageType.MSG_TYPE_SEND_FRAME:
                            if (msg.image != null)
                            {
                                this.interfaceEndpoint.Interface.RecieveFrame(msg.image, msg.fid,this.instanceID);
                            }
                            break;


                        //Network layout recieved, only react if ist first layout
                        case messageType.MSG_TYPE_RESPOND_NETWORK:
                            if (msg.image != null)
                            {
                                this.interfaceEndpoint.Interface.RecieveFrame(msg.image, msg.fid, this.instanceID) ;
                            }
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

        //** COMMINTERFACE EXPORTED FUNCTIONS (IVMCommInt) **//

        //return if the network is ready to send
        bool IVMCommInt.Ready()
        {

        }

        //gets the local network configuration
        VMNetwork IVMCommInt.NetworkStats()
        {
            return network;
        }

        void IVMCommInt.SendFrame(Image frame, FrameID id) //send a frame 
        {
            //send frame message
            this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_SEND_FRAME, messages++, DateTime.Now, null, null, frame, id, null, myAddress, null, 1, 1));
        }

        void IVMCommInt.SendGlobalCommand(string rfc_command, Parameter[] parameters)
        {
            //send command w/ no destination
            this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_CONTROL_RFC, messages++, DateTime.Now, parameters, rfc_command, null, null, null, myAddress, null, 1, 1));
        }

        void IVMCommInt.SendCommand(VMAddress dest, string rfc_command, Parameter[] parameters)
        {
            //send command with given destination
            this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_CONTROL_RFC, messages++, DateTime.Now, parameters, rfc_command, null, null, null, myAddress, dest, 1, 1));
        }

        void IVMCommInt.SendLocalServices(VMService service)
        {
            //update local address
            service.svc_addr = myAddress;
            //send local service to network (forces update over network)
            this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_EXPOSE_SVC, messages++, DateTime.Now, null, null, null, null, service, myAddress, null, 1, 1));
        }

        VMService[] IVMCommInt.GetNetworkServices()
        {
            return this.network.services;
        }

        string IVMCommInt.GetInstanceID()
        {
            return this.instanceID;
        }


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
