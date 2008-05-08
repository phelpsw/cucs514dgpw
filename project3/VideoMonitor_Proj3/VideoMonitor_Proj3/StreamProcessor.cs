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
    [QS.Fx.Reflection.ComponentClass("4`1", "StreamProcessor")]
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

             //initialize address
            myAddress = new VMAddress(new int[] { -1 });

            alarmTimers = new PriorityQueue(acmp);

            digester = new Thread(new ThreadStart(Digester));
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
        public class VMMsgConfig
        {
            //Standard time between check for expired alarms
            public const int alarmCheck = 100;

            //standard time between checks of parent
            public const int parent_live_check_delay = 2000;
            //timeout on check responses before repeat
            public const int parent_live_check_ttl = 200;
            //number of times to parent doesn't respond before listing as dead
            public const int parent_live_check_count = 3;

            //time to wait for reponses to network status packet before repeat
            public const int netowrk_model_wait_ttl = 300;
            //number of times to attempt request of status packet before marking dead and asking again
            public const int netowrk_model_wait_count = 5;

            //time between checkup message packets
            public const int checkup_msg_delay = 8000;

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

        //threading timer to handle calls to the timer class.
        private System.Threading.Thread digester = null;


        /** STRUCTURES TO HANDLE MESSAGE MANIPULATION AND FLOW **/

        //comparison operator for PriorityQueue
        private AlarmComparer acmp = new AlarmComparer();

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
                //create message
                VMMessage msg = new VMMessage(messageType.MSG_TYPE_REQUEST_NETWORK, //message type
                    messages++, DateTime.Now, //message id and time sent
                    null, null, //payload relating to control message    (parameters, command,)
                    null, null, //payload relating to image frame        (image, frameid,)
                    null, null, //payload relating to service or network (service, network,)
                    myAddress, null, //message addressing information         (source, destination,)
                    1, VMMsgConfig.netowrk_model_wait_count); //send counters (count, maximum count)
                //send message
                this.channelendpoint.Interface.Send(msg);

                //ad an alarm to resend the request the given number of times after waiting the specified time.
                AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CONTMSG, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                    DateTime.Now.AddMilliseconds(VMMsgConfig.netowrk_model_wait_ttl), VMMsgConfig.netowrk_model_wait_ttl, //set delay
                    msg, setAsRoot, null,false)); //message and callback stuff w/ repeat
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

        //retrieve all of the alarms corrisponding to the given message id
        public List<VMAlarm> getAlarmsByMessage(int msg_id)
        {
            List<VMAlarm> list = new List<VMAlarm>();

            foreach (VMAlarm l in alarmList.Values)
            {
                if ((l.type==VMAlarm.AlarmType.ALM_TYPE_CONTMSG || l.type==VMAlarm.AlarmType.ALM_TYPE_MESSAGE) && l.message.id == msg_id)
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
            alarmList.Add(alarm.id, alarm);

            //MessageBox.Show("Alarm Added, Length:"+alarm.delay+" Repeats"+alarm.repeats.ToString());

            if (timer == null) StartTimer(); //timer not currently running, start it
        }

        //Remove any alarms related to the message id:
        void RemoveAlarmsByMessageID(int messageid)
        {
            List<VMAlarm> alarms = getAlarmsByMessage(messageid); //previous message id store in parameter 0.val
            foreach (VMAlarm alarm in alarms)
            {
                alarmList.Remove(alarm.id);
                alarmTimers.Remove(alarm);
            }
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
                            //MessageBox.Show("ALARM");
                            break;

                        case VMAlarm.AlarmType.ALM_TYPE_CONTMSG:
                            alarm.message.count++;
                            if (alarm.message.count > alarm.message.max_count) //if max number of attempts reached, run callback function
                            {
                                alarm.callback(alarm.callbackParams);
                                alarmList.Remove(alarm.id);
                            }
                            else //resend
                            {
                                VMMessage alm_msg = alarm.message;
                                this.channelendpoint.Interface.Send(alm_msg);

                                alarm.expires = DateTime.Now.AddMilliseconds(alarm.delay);
                                if(!alarm.repeats) alarmTimers.Enqueue(alarm);
                            }
                            break;
                    }
                    //remove alarm from the queue and from the listist
                    

                    //reinsert alarm if it repeats
                    if (alarm.repeats)
                    {
                        alarm.expires = DateTime.Now.AddMilliseconds(alarm.delay);
                        alarmTimers.Enqueue(alarm);
                    }
                    else
                    {
                        alarmList.Remove(alarm.id);
                    }
                }
                //if (alarmList.Count == 0) StopTimer(); //end the timer if none remain
            }
            //end, will be relaunched in delta* time
        }

        /** END TIMER FUNCTIONS **/

        /** INITIALIZATION FUNCTIONS **/

        void onInitialize()
        {
            //MessageBox.Show("Initalized, my address:" + myAddress.id[0].ToString()); 
            //start timer to check on parent
            beginParentCheckTimer();

            //if I am the root node, the start the checkup timer
            if (network.root().svc_addr.id[0] == myAddress.id[0])
            {
                beginCheckupTimer();
            }

            //alert ui that network has changed
            this.interfaceEndpoint.Interface.OnNetworkUpdate(this.instanceID);
        }

        void beginParentCheckTimer()
        {
            return;
            //add an alarm to check the parent every set miliseconds
            AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CALLBCK, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                DateTime.Now.AddMilliseconds(VMMsgConfig.parent_live_check_delay), VMMsgConfig.parent_live_check_delay, //set delay
                null, parentLiveCheck, null, true)); //message and callback stuff w/ repeat
        }

        void beginCheckupTimer()
        {
            return;
            //add an alarm to check the parent every set miliseconds
            AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CALLBCK, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                DateTime.Now.AddMilliseconds(VMMsgConfig.checkup_msg_delay), VMMsgConfig.checkup_msg_delay, //set delay
                null, checkupMessageSend, null, true)); //message and callback stuff w/ repeat
        }

        /** END INITIALIZATION FUNCTIONS **/

        /** TIMER CALLBACK FUNCTIONS **/

        //callback if no response is recieved from network request from networkReady
        public void setAsRoot(VMParameters parameters)
        {
            if (!isInitialized)
            {
                //initialize my address
                myAddress = new VMAddress(new int[] { 0 });

                VMService svc = rasterMyService(); //collects components of local service model

                //initialize network object and add first element
                network = new VMNetwork(new VMServices(new VMService[] { svc }));

                onInitialize();

                isInitialized = true;  //is now initialized and ready
            }
        }

        //send messages to check parent on network
        public void parentLiveCheck(VMParameters parameters)
        {
            if (network.services.services.GetLength(0) == 1) return;
            //create a message to send to parent
            VMService parent = network.parent(myAddress); //get my parent (implicit::each time)
            //create message
            VMMessage msg = new VMMessage(messageType.MSG_TYPE_CONFIRM_LIVE, //message type
                messages++, DateTime.Now, //message id and time sent
                null, null, //payload relating to control message    (parameters, command,)
                null, null, //payload relating to image frame        (image, frameid,)
                null, null, //payload relating to service or network (service, network,)
                myAddress, parent.svc_addr, //message addressing information         (source, destination,)
                1, VMMsgConfig.netowrk_model_wait_count); //send counters (count, maximum count)
            //send
            this.channelendpoint.Interface.Send(msg);

            //add an alarm to check the parent every set miliseconds
            AddAlarm(new VMAlarm(VMAlarm.AlarmType.ALM_TYPE_CONTMSG, //send alarm type of contingent message (will monitor max send and call deligate on max_count)
                DateTime.Now.AddMilliseconds(VMMsgConfig.checkup_msg_delay), VMMsgConfig.checkup_msg_delay, //set delay
                msg, failParentLiveCheck, new VMParameters(new VMParameter[] {new VMParameter("parentid",parent.svc_addr.id[0].ToString())} ), true)); //message and callback stuff w/ repeat
        }

        //call if timeout on repeat for parent live check
        public void failParentLiveCheck(VMParameters parameters)
        {
            //we now want to remove the parent element from the network, 
            //  send a remove message to the channel

            int parentid = int.Parse(parameters.parameters[0].val);
            //re-create the parent address
            VMAddress addr = new VMAddress(new int[] { parentid });
            //get the parent's service
            VMService parentsvc = network.getServicesByID(addr);

            if (parentsvc == null) return; //if parent no longer exists, ignore ((FIXXII))
            //remove the parent service locally
            network.removeService(parentsvc);

            //create message (Send service to remove)
            VMMessage msg = new VMMessage(messageType.MSG_TYPE_REMOVE_DEAD, //message type
                messages++, DateTime.Now, //message id and time sent
                null, null, //payload relating to control message    (parameters, command,)
                null, null, //payload relating to image frame        (image, frameid,)
                parentsvc, null, //payload relating to service or network (service, network,)
                myAddress, null, //message addressing information         (source, destination,)
                1, 1); //send counters (count, maximum count)
            //send
            this.channelendpoint.Interface.Send(msg);
        }

        public void checkupMessageSend(VMParameters parameters)
        {
            //create message (Send my service object)
            VMMessage msg = new VMMessage(messageType.MSG_TYPE_CHECKUP_MESSAGE, //message type
                messages++, DateTime.Now, //message id and time sent
                null, null, //payload relating to control message    (parameters, command,)
                null, null, //payload relating to image frame        (image, frameid,)
                null, network, //payload relating to service or network (service, network,)
                myAddress, null, //message addressing information         (source, destination,)
                1, 1); //send counters (count, maximum count)
            //send
            this.channelendpoint.Interface.Send(msg);
        }

        /** END TIMER CALLBACK FUNCTIONS **/

        /** DIGESTOR / MESSAGE HANDLER FUNCIONALITY FOR MANAGING MESSAGE ROUTING **/

        //METHOD TO BEGIN MESSAGE DIGESTER THREAD (runs on form thread)
        private void StartDigester()
        {
            //if(!digester.IsAlive) {
            //    digester.Start();
            //}
            Digester();
        }

        //Clears out the message queue of new meessages and routes them according to type and parameters
        private void Digester()
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
                            //only respond if current am the tail node
                            if (msg != null && isInitialized /*&& myAddress.id[0] == network.tail().svc_addr.id[0]*/)
                            {
                                //create message
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_RESPOND_NETWORK, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    new VMParameters(new VMParameter[] { new VMParameter("rcv_id", msg.id.ToString()) }), null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    null, network, //payload relating to service or network (service, network,)
                                    myAddress, msg.srcAddr, //message addressing information         (source, destination,)
                                    1, VMMsgConfig.netowrk_model_wait_count); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(smsg);
                                //MessageBox.Show("Sending Network W/ " + network.services.serviceSet.Count.ToString() + " services");
                                
                                //add the new service to the network provsionally
                                VMService nsvc = new VMService(new VMAddress(new int[] { (myAddress.id[0]+1) }),0,0,null);
                                network.addService(nsvc);

                                //-- this will keep this service from responding to any new requests


                            }
                            break;

                        //Network layout recieved, only react if ist first layout
                        case messageType.MSG_TYPE_RESPOND_NETWORK:
                            if (msg.network != null && !isInitialized) //only recieve once, before initialized
                            {
                                //MessageBox.Show("Recv. Network Model, Size Prev: "+msg.network.services.serviceSet.Count.ToString());
                                network = msg.network; //set my local netowrk
                                //extract my address from the network, last id +1 !

                                myAddress.id[0] = msg.network.tail().svc_addr.id[0] + 1;

                                //remove alarm in system:
                                RemoveAlarmsByMessageID(int.Parse(msg.parameters.parameters[0].val));

                                //now add self to the network object
                                VMService mySvc = rasterMyService(); //get my local service
                                network.addService(mySvc);

                                //finally, expose self to network
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_EXPOSE_SVC, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    null, null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    mySvc, null, //payload relating to service or network (service, network,)
                                    myAddress,null, //message addressing information         (source, destination,)
                                    1, 1); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(smsg);

                                //now with a local network model and have exposed self to the network,
                                // initialized locally and running

                                isInitialized = true;

                                onInitialize(); //run local timers etc...
                            }
                            break;

                        //when asked if still live, respond 
                        case messageType.MSG_TYPE_CONFIRM_LIVE:
                            if (msg.srcAddr != null && isInitialized)
                            {
                                //create message
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_RESPOND_LIVE, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    new VMParameters(new VMParameter[] { new VMParameter("rcv_id", msg.id.ToString()) }), null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    null, null, //payload relating to service or network (service, network,)
                                    myAddress, msg.srcAddr, //message addressing information         (source, destination,)
                                    1, 1); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(smsg);
                            }
                            break;

                        //when confirmed live, remove any alarms associated with this request
                        case messageType.MSG_TYPE_RESPOND_LIVE:
                            if (msg.srcAddr != null && isInitialized)
                            { 
                                //get any alarms associated w/ this message
                                RemoveAlarmsByMessageID(int.Parse(msg.parameters.parameters[0].val));
                            }
                            break;

                        //when new network service recieved, add to network model
                        case messageType.MSG_TYPE_EXPOSE_SVC:
                            if (msg.service != null && isInitialized)
                            {
                                //add the new exposed service to the model
                                bool exists = network.addService(msg.service);
                                this.interfaceEndpoint.Interface.OnNetworkUpdate(this.instanceID);
                            }
                            break;

                        //when a checkup message is recieved, send back disparaties in the networks
                        case messageType.MSG_TYPE_CHECKUP_MESSAGE:
                            if (msg.network != null && isInitialized)
                            {
                                int mylen = network.services.services.GetLength(0);
                                int rootlen = msg.network.services.services.GetLength(0);

                                List<VMService> tosend = new List<VMService>();

                                int i,j;  // i = iterator for my network, j = iterator for root's network

                                for (i = j = 0; i < (mylen > rootlen ? mylen : rootlen );)
                                {
                                    if (i >= mylen)
                                    {
                                        //anything left in j (root) will not be in i (local)
                                        // push any of these into local array
                                        network.addService(msg.network.services.services[j]);
                                        j++;i++;
                                    }
                                    else if (j >= rootlen)
                                    {
                                        //anything left in i (local) will not be in j (root)
                                        tosend.Add(network.services.services[i]);
                                        j++;i++;
                                    }
                                    else
                                    {
                                        int loc_id = network.services.services[i].svc_addr.id[0];
                                        int root_id = msg.network.services.services[j].svc_addr.id[0];
                                        if(loc_id != root_id) { //if they are different
                                            if(loc_id > root_id) {
                                                //if the local service is greater,
                                                //then it is obviously in the local but not the root
                                                tosend.Add(network.services.services[i]);
                                                i++;
                                                   
                                            } else {
                                                //then it is in the root but not local
                                                network.addService(msg.network.services.services[j]);
                                                j++;
                                            }
                                        }
                                    }

                                }

                                //create disparity network to send
                                VMNetwork toSend = new VMNetwork(new VMServices(tosend.ToArray()));


                                //create message
                                VMMessage smsg = new VMMessage(messageType.MSG_TYPE_CHECKUP_RESPOND, //message type
                                    messages++, DateTime.Now, //message id and time sent
                                    new VMParameters(new VMParameter[] { new VMParameter("rcv_id", msg.id.ToString()) }), null, //payload relating to control message    (parameters, command,)
                                    null, null, //payload relating to image frame        (image, frameid,)
                                    null, toSend, //payload relating to service or network (service, network,)
                                    myAddress, msg.srcAddr, //message addressing information         (source, destination,)
                                    1, 1); //send counters (count, maximum count)
                                //send
                                this.channelendpoint.Interface.Send(smsg);
                                //check differences w/ local network
                                //   add any services in root model but not mine
                                //   send a network model w/ anything in my model but not root
                            }
                            break;
                        //only the root should be recieving this message
                        case messageType.MSG_TYPE_CHECKUP_RESPOND:
                            if (msg.network != null && isInitialized)
                            {
                                //add each service to the local object
                                foreach(VMService svc in msg.network.services.services) {
                                    network.addService(svc);
                                }

                                //send checkup message
                                checkupMessageSend(null);
                            }
                            break;
                        case messageType.MSG_TYPE_REMOVE_DEAD:
                            if (msg.service != null && isInitialized)
                            {
                                //simply remove the service on recieve
                                bool didremove = network.removeService(msg.service);
                                
                                //if it was I who was delete (oh crap! i been bad)
                                if(msg.service.svc_addr.id[0] == myAddress.id[0]) {
                                    //redo all of the add stuff :-(

                                    //now add self to the network object
                                    VMService mySvc = rasterMyService(); //get my local service
                                    network.addService(mySvc);

                                    //finally, expose self to network
                                    VMMessage smsg = new VMMessage(messageType.MSG_TYPE_EXPOSE_SVC, //message type
                                        messages++, DateTime.Now, //message id and time sent
                                        null, null, //payload relating to control message    (parameters, command,)
                                        null, null, //payload relating to image frame        (image, frameid,)
                                        mySvc, null, //payload relating to service or network (service, network,)
                                        myAddress,null, //message addressing information         (source, destination,)
                                        1, 1); //send counters (count, maximum count)
                                    //send
                                    this.channelendpoint.Interface.Send(smsg);
                                }

                                //if i am now the root
                                if (myAddress.id[0] == network.root().svc_addr.id[0])
                                {
                                    //begin duties at the root node
                                    beginCheckupTimer();
                                }
                            }
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

        void IVMCommInt.SendGlobalCommand(string rfc_command, VMParameters parameters)
        {
            //send command w/ no destination
            if (this.isInitialized)
                this.channelendpoint.Interface.Send(new VMMessage(messageType.MSG_TYPE_CONTROL_RFC, messages++, DateTime.Now, parameters, rfc_command, null, null, null, null, myAddress, null, 1, 1));
        }

        void IVMCommInt.SendCommand(VMAddress dest, string rfc_command, VMParameters parameters)
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

        VMServices IVMCommInt.GetNetworkServices()
        {
            return this.network.services;
        }

        string IVMCommInt.GetInstanceID()
        {
            return this.instanceID;
        }

        VMAddress IVMCommInt.GetMyAddress()
        {
            return this.myAddress;
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
            if (_message == null)
                throw new Exception("Message is null");

            if (_message.srcAddr == null)
                throw new Exception("Message SourceAddress is null");

            if (myAddress == null)
               throw new Exception("myAddress is null");

            //if(_message.srcAddr != null)
                //MessageBox.Show("MsgID:"+_message.srcAddr.id[0].ToString() + " Send Count: "+ _message.count.ToString()); 
            //upon receiving a message, simply throw it into the queue and let the digester pick it up
            //if message has generic address or is addressed to me, process, otherwise ignore, also block loopback
            if (isInitialized || _message.type == messageType.MSG_TYPE_RESPOND_NETWORK)
            {
                if ((_message.dstAddr == null && (_message.srcAddr.id[0] != myAddress.id[0])) || (_message.dstAddr != null && _message.dstAddr.id[0] == myAddress.id[0]))
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
            /*
            if ((_message.dstAddr == null || _message.dstAddr.id[0] == myAddress.id[0]) && (_message.dstAddr == null || _message.srcAddr.id[0] != myAddress.id[0]))
            {
                
            }*/
        }

        #endregion
    }
}
