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
        public VMNetwork network;

        //local address
        private VMAddress myAddress;

        //number of messages that have been sent by this node;
        private int messages = 0;
        
        //parent whom is checked on to make sure they are still live
        private int myParent = 0;

        //threading timer to handle calls to the timer class.
        private System.Threading.Timer timer = null;


        /** STRUCTURES TO HANDLE MESSAGE MANIPULATION AND FLOW **/

        //comparison operator for PriorityQueue
        private AlarmComparer acmp = new AlarmComparer();

        private ServiceComparer scmp = new ServiceComparer();

        //message queue for incoming recieved messages
        private Queue<VMMessage> incoming = new Queue<VMMessage>();

        //Queue to allow timer timeouts on alarms.
        private PriorityQueue alarmTimers;

        //List of Active Alarms (refrenced by Alarms.toString() method as keys
        private IDictionary<string, VMAlarm> alarmList = new Dictionary<string, VMAlarm>();


        //List of Viewable Messages (ordered by insertion)
        //private List<VMMessage> messageList = new List<VMMessage>();

        //Index to Viewable Messages (not necissarily ordered correctly, but easily referenced)
        //private IDictionary<string, VMMessage> messageIndex = new Dictionary<string, VMMessage>();



        //When Connection to Channel is established, the following method will be called to set up the local connection
        private void networkReady()
        {
            if (!isInitialized)
            {
                //broadcast network query message
                lock (messages)
                {
                    //create message
                    VMMessage msg = new VMMessage(messageType.MSG_TYPE_REQUEST_NETWORK, //message type
                        messages++, DateTime.Now, //message id and time sent
                        null, null, //payload relating to control message    (parameters, command,)
                        null, null, //payload relating to image frame        (image, frameid,)
                        null, null, //payload relating to service or network (service, network,)
                        null, null, //message addressing information         (source, destination,)
                        1, VMMsgConfig.netowrk_model_wait_count); //send counters (count, maximum count)
                    //send message
                    this.channelendpoint.Interface.Send(msg);
                }

                //ad an alarm to resend the request the given number of times after waiting the specified time.
                AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CONTMSG, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                    DateTime.Now.AddMilliseconds(VMMsgConfig.netowrk_model_wait_ttl), VMMsgConfig.netowrk_model_wait_ttl, //set delay
                    msg, setAsRoot,null,false)); //message and callback stuff w/ repeat
            }
        }

        private VMService rasterMyService()
        {
            //collect service info from local service object
            VMService mySvc = this.interfaceEndpoint.Interface.GetLocalService(this.instanceID);

            //get my local address
            mySvc.svc_addr = this.myAddress;

            //get services on other side of any gateway (for server, otherwise null)
            mySvc.subServices = this.interfaceEndpoint.Interface.GetRemoteServices(this.instanceID);

            return mySvc;
        }


        //callback if no response is recieved from network request from networkReady
        public static void setAsRoot(Parameter[] parameters)
        {
            //initialize my address
            this.myAddress = new VMAddress(new int[] { 0 });

            VMService svc = rasterMyService(); //collects components of local service model

            //initialize network object
            network = new VMNetwork(new VMService[] { svc });

            isInitialized = true;  //is now initialized and ready
        }

        //retrieve all of the alarms corrisponding to the given message id
        public List<VMAlarm> getAlarmsByMessage(int msg_id)
        {
            List<VMAlarm> list = new List<VMAlarm>();

            foreach (VMAlarm l in alarmList.Values)
            {
                if (l.message.id.toString() == msg_id.toString())
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
                        case VMAlarm.AlarmType.ALM_TYPE_MESSAGE: //send message on expire
                            VMMessage msg = alarm.message;
                            this.channelendpoint.Interface.Send(msg);
                            break;

                        case VMAlarm.AlarmType.ALM_TYPE_CALLBCK: //simply call method on expirie
                            alarm.callback(alarm.callbackParams);
                            break;

                        case VMAlarm.AlarmType.ALM_TYPE_CONTMSG:
                            alarm.message.count++;
                            if (alarm.message.count > alarm.message.max_count) //if max number of attempts reached, run callback function
                            {
                                alarm.callback(alarm.callbackParams);
                            }
                            else //resend
                            {
                                VMMessage msg = alarm.message;
                                this.channelendpoint.Interface.Send(msg);
                            }
                            break;
                    }
                    //remove alarm from the queue and from the listist
                    alarmList.Remove(alarm.toString());

                    //reinsert alarm if it repeats
                    if (alarm.repeats)
                    {
                        alarm.expires = DateTime.Now.AddMilliseconds(alarm.delay);
                        AddAlarm(alarm);
                    }
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
                    VMService local_svc = rasterMyService(); //get my local service info
                    switch (msg.type)
                    {
                        /*MESSAGES DEALING WITH NON-NETWORK RELATED FUNCTIONS*/

                        //control message recieved, pass to interface
                        case messageType.MSG_TYPE_CONTROL_RFC:
                            if (msg.rfc_command != null && isInitialized)
                            {
                                //simply pass to endpoint
                                this.interfaceEndpoint.Interface.RecieveCommand(msg.srcAddr, msg.rfc_command, msg.parameters, this.instanceID);
                            }
                            break;

                        //frame message recieved, pass to interface
                        case messageType.MSG_TYPE_SEND_FRAME:
                            if (msg.image != null && isInitialized)
                            {
                                //simply pass to endpointo
                                this.interfaceEndpoint.Interface.RecieveFrame(msg.image, msg.fid,this.instanceID);
                            }
                            break;

                        /*MESSAGES DEALING WITH NETWORK RELATED FUNCTIONS*/

                        //when asked for the network, return local network model
                        case messageType.MSG_TYPE_REQUEST_NETWORK:
                            if (msg != null && isInitialized)
                            {
                                //create message
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_RESPOND_LIVE, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    new Parameter[] { new Parameter("rcv_id", msg.id.ToString()) }, null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    null, network, //payload relating to service or network (service, network,)
                                    myAddress, msg.srcAddr, //message addressing information         (source, destination,)
                                    1, VMMsgConfig.netowrk_model_wait_count); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(msg);
                            }
                            break;

                        //Network layout recieved, only react if ist first layout
                        case messageType.MSG_TYPE_RESPOND_NETWORK:
                            if (msg.network != null && !isInitialized) //only recieve once, before initialized
                            {
                                network = msg.network; //set my local netowrk
                                //extract my address from the network, last id +1 !
                                myAddress = msg.network.services[msg.network.services.GetLength(0)].svc_addr.id[0]+1;

                                //remove alarm in system:
                                List<VMAlarm> alarms = getAlarmsByMessage(int.Parse(msg.parameters[0].val)); //previous message id store in parameter 0.val
                                foreach (VMAlarm alarm in alarms)
                                {
                                    alarmList.Remove(alarm.toString());
                                    alarmTimers.Remove(alarm);
                                }
                                //now add self to the network object
                                VMService mySvc = rasterMyService(); //get my local service

                                network.services += mySvc; //add myself to the local network

                                Array.Sort(network.services, scmp); //sort the local network by id

                                //finally, expose self to network
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_EXPOSE_SVC, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    null, null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    mySvc, null, //payload relating to service or network (service, network,)
                                    myAddress,null, //message addressing information         (source, destination,)
                                    1, 1); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(msg);
                                isInitialized = true;
                            }
                            break;

                        //when asked if still live, respond 
                        case messageType.MSG_TYPE_CONFIRM_LIVE:
                            if (msg.srcAddr != null && isInitialized)
                            {
                                //create message
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_RESPOND_LIVE, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    new Parameter[] {new Parameter("rcv_id",msg.id.ToString())}, null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    null, null, //payload relating to service or network (service, network,)
                                    myAddress, msg.srcAddr, //message addressing information         (source, destination,)
                                    1, 1); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(msg);
                            }
                            break;

                        //when confirmed live, remove any alarms associated with this request
                        case messageType.MSG_TYPE_RESPOND_LIVE:
                            if (msg.srcAddr != null && isInitialized)
                            {
                                //get any alarms associated w/ this message
                                List<VMAlarm> alarms = getAlarmsByMessage(int.Parse(msg.parameters[0].val)); //previous message id store in parameter 0.val
                                foreach (VMAlarm alarm in alarms)
                                {
                                    alarmList.Remove(alarm.toString());
                                    alarmTimers.Remove(alarm);
                                }

                            }
                            break;

                        //when new network service recieved, add to network model
                        case messageType.MSG_TYPE_EXPOSE_SVC:
                            if (msg.srcAddr != null && isInitialized)
                            {
                                //check if service exists: 
                                int pos = checkExistingService(msg.service.svc_addr);
                                if (pos == null) //doesn't exist
                                {
                                    network.services += msg.service; //addto list
                                }
                                else //exists already
                                {
                                    network.services[pos] = msg.service; //place in existing position
                                }
                                Array.Sort(network.services, scmp); //sort the local network by id
                            }
                            break;
                    }
                }
            }
        }

        //retrieve check to see if a service exists already, if so, return it's id in the list
        public int checkExistingService(VMAddress addr)
        {
            for (int i=0;i<network.services.GetLength(0);i++)
            {
                if (network.services[i].svc_addr.id.ToString() == addr.id.ToString())
                    return i;
            }
            return null;
        }

        /** END DIGESTOR FUNCTIONS **/

        //** COMMINTERFACE EXPORTED FUNCTIONS (IVMCommInt) **//

        //return if the network is ready to send
        bool IVMCommInt.Ready()
        {
            return isInitialized;
        }

        //gets the local network configuration
        VMNetwork IVMCommInt.NetworkStats()
        {
            return network;
        }

        void IVMCommInt.SendFrame(VMImage frame, FrameID id) //send a frame 
        {
            //send frame message
            if(this.isInitialized)
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_SEND_FRAME, messages++, DateTime.Now, null, null, frame, id, null, null, myAddress, null, 1, 1));
        }

        void IVMCommInt.SendGlobalCommand(string rfc_command, Parameter[] parameters)
        {
            //send command w/ no destination
            if (this.isInitialized)
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_CONTROL_RFC, messages++, DateTime.Now, parameters, rfc_command, null, null, null, null, myAddress, null, 1, 1));
        }

        void IVMCommInt.SendCommand(VMAddress dest, string rfc_command, Parameter[] parameters)
        {
            //send command with given destination
            if (this.isInitialized)
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_CONTROL_RFC, messages++, DateTime.Now, parameters, rfc_command, null, null, null, null, myAddress, dest, 1, 1));
        }

        void IVMCommInt.SendLocalServices(VMService service)
        {
            if (this.isInitialized)
            {
                //update local address
                service.svc_addr = myAddress;
                //send local service to network (forces update over network)
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_EXPOSE_SVC, messages++, DateTime.Now, null, null, null, null, service, null, myAddress, null, 1, 1));
            }
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

        NullC QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<VMMessage, NullC>.Checkpoint()
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
                //if message has generic address or is addressed to me, process, otherwise ignore, also block loopback
                if ((_message.dstAddr == null || _message.dstAddr.id[0] == myAddress.id[0]) && _message.srcAddr.id[0]!=myAddress.id[0])
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
        }

        #endregion
    }
}
