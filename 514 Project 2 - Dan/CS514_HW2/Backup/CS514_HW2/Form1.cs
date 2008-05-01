using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LSCollections;
using TachyonLabs.SharpSpell.UI;

namespace CS514_HW2
{
    [QS.Fx.Reflection.ComponentClass("1`1", "Editable Chat", "Editable Chat Window Project")]
    public sealed partial class Form1 : UserControl, QS.Fx.Object.Classes.IUI,
        QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage,IChatState>
    {
        public Form1(
            [QS.Fx.Reflection.Parameter("channel",QS.Fx.Reflection.ParameterClass.Value)]
            QS.Fx.Object.IReference<QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<IMessage, IChatState>> channel)
        {
            InitializeComponent();
            this.myendpoint = QS.Fx.Endpoint.Internal.Create.ExportedUI(this);
            this.channelproxy = channel.Object;
            this.mychannelendpoint = QS.Fx.Endpoint.Internal.Create.DualInterface<
                QS.Fx.Interface.Classes.ICheckpointedCommunicationChannel<IMessage, IChatState>,
            QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage, IChatState>>(this);
            this.mychannelendpoint.Connect(this.channelproxy.Channel);

            lockTimers = lockTimers = new PriorityQueue(icmp);

            customUnderlines = new CustomPaintTextBox(textBox1);
        }

        private QS.Fx.Endpoint.Internal.IExportedUI myendpoint;
        private QS.Fx.Object.Classes.ICheckpointedCommunicationChannel<IMessage, IChatState> channelproxy;
        private QS.Fx.Endpoint.Internal.IDualInterface<
            QS.Fx.Interface.Classes.ICheckpointedCommunicationChannel<IMessage, IChatState>,
            QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage, IChatState>> mychannelendpoint;
        private QS.Fx.Endpoint.IConnection connectiontochannel;

        //number of nodes in the system
        private int nodes;

        //node number of this node
        private int myNodeNum = 0;
        
        //number of messages that have been sent by this node;
        private int messages = 0;
        
        //threading timer to handle calls to the timer class.
        private System.Threading.Timer timer = null;

        /** CURRENT EDITING STATE VARIABLES **/

        private bool editState = false; //not editing by default

        private Lock activeLock; //currently active lock element

        /** **/


        CustomPaintTextBox customUnderlines;

        //comparison operator for PriorityQueue
        private LockComparer icmp = new LockComparer();

        //message queue
        private Queue<IMessage> incoming = new Queue<IMessage>();

        
        //Queue to allow timer timeouts on locks.
        private PriorityQueue lockTimers;

        //List of Active Locks (refrenced by Lock.toString() method as keys
        private IDictionary<string, Lock> lockList = new Dictionary<string,Lock>();

        
        //List of Viewable Messages (ordered by insertion)
        private List<Message> messageList = new List<Message>();

        //Index to Viewable Messages (not necissarily ordered correctly)
        private IDictionary<string, Message> messageIndex = new Dictionary<string, Message>();


        //METHOD TO MANAGE TIMER THREAD
        private void StartTimer()
        {
            if (timer==null) //timer not currently running, else, wait
            {
                System.Threading.TimerCallback cb = new System.Threading.TimerCallback(Timer);
                //begins new timer after 1 second, with 
                timer = new System.Threading.Timer(cb, null, 1000, 1000);
            }
        }
        private void StopTimer()
        {
            timer.Dispose();
            timer = null;
        }

        //Timer Thread to Monitor lockTimers queue and 
        private void Timer(object obj)
        {
            bool didupdate = false;
            if (editState) refreshCountdown();
            lock (lockTimers)
            {
                //remove all expired timers
                while (DateTime.Compare(((Lock)lockTimers.Peek()).expires, DateTime.Now) < 0)
                {
                    //alert that atleast one update occurred
                    didupdate = true;
                    //remove lock from the queue and from the lockList
                    lockList.Remove(((Lock)lockTimers.Dequeue()).toString());
                }
                if (lockTimers.Count == 0 && !editState) StopTimer(); //end the timer if none remain
            }
            if (didupdate) refreshView();

            //end, will be relaunched in delta* time
        }

        //Invokes a method to refresh the current "chat view"
        private void refreshView()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.Refresh), null);
            else
                this.Refresh(null, null);
        }

        //Asynchronous Refresh of Text window (invoke protected)
        private void Refresh(object sender, EventArgs args)
        {
            richTextBox1.Clear();
            foreach (Message m in messageList)
            {
                AppendText("[" + m.id.src_id + "]: ",(m.id.src_id==myNodeNum?1:2));
                AppendText(m.text + "\r\n", 0);
            }
            richTextBox1.ScrollToCaret();
            label1.Text = "updated "+myNodeNum;
            label1.Visible = true;
        }

        private void AppendText(string message,int type)
        {
            // Start a new selection from the end in the rich text box.
            richTextBox1.SelectionStart = richTextBox1.Text.Length;

            // If the message contains the word 'error', then this line will be set to red color. 
            // If the word 'error' does not appear in the text, then Black color will be used.
            if (type == 1)
            {
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.SelectionFont = new Font("Courier New", 15, FontStyle.Bold);
            }
            else if (type == 2)
            {
                richTextBox1.SelectionColor = Color.Blue;
                richTextBox1.SelectionFont = new Font("Arial", 15, FontStyle.Regular);
            }
            else
            {
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.SelectionFont = new Font("Arial", 15, FontStyle.Regular);
            }

            richTextBox1.AppendText(message);
        }

        //Invokes a method to refresh the current "time remaining"
        private void refreshCountdown()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.refreshCountdownRemain), null);
            else
                this.refreshCountdownRemain(null, null);
        }

        //Asynchronous Refresh of Text window (invoke protected)
        private void refreshCountdownRemain(object sender, EventArgs args)
        {
           TimeSpan ts = activeLock.expires-DateTime.Now;
           label1.Text = "Lease Remaining: "+((double)Math.Round(ts.TotalSeconds)).ToString() + " seconds.";
        }

        private void doEdit()
        {

        }

        private void endEdit()
        {

        }

        private bool tryLock(Lock lok)
        {
            bool conflict = false;
            lock (incoming)
            {
                lock (lockTimers)
                {
                    //check to see if lock violates any others
                    foreach (Lock l in lockList.Values)
                    {
                        //test for overlap in locks
                        if (l.id.toString() == lok.id.toString() && ((l.startC > lok.startC && l.startC < lok.endC) || (l.endC < lok.endC && l.endC > lok.startC)))
                        {
                            conflict = true;
                            break;
                        }
                    }
                    if (!conflict)
                    {
                        //send lock request
                        this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_LOCK, DateTime.Now, lok, null, 0));
                    }
                    else
                    {
                        //cannot lock!
                        return false;
                    }
                }
            }
            return true;
        }

        //METHOD TO BEGIN MESSAGE DIGESTER THREAD (runs on form thread)
        private void StartDigester()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.Digester), null);
            else
                this.Digester(null, null);
        }

        //Clears out the message queue of new meessages
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
                        case messageTypes.MSG_TYPE_SEND_MSG:
                            if (msg.message != null)
                            {
                                //simply add to the message list
                                messageList.Add(msg.message);
                                messageIndex.Add(msg.message.id.toString(),msg.message);
                            }
                            this.Refresh(null, null); //redraw for new message
                            break;

                        //Lock Request
                        case messageTypes.MSG_TYPE_LOCK:
                            if (msg.Ilock != null)
                            {
                                Lock exist;
                                if(lockList.TryGetValue(msg.Ilock.toString(),out exist)) {
                                    //does exist (remove current and replace w/ new) this allows leases to be extended
                                    lockList.Remove(msg.Ilock.toString());
                                    lockTimers.Remove(exist);
                                }
                                //initalize timer
                                msg.Ilock.expires = new DateTime();
                                msg.Ilock.expires = DateTime.Now;
                                msg.Ilock.expires.AddSeconds(msg.Ilock.time);

                                //add lock to the timers list
                                lockTimers.Enqueue(msg.Ilock);

                                //add lock to the indexed list
                                lockList.Add(msg.Ilock.toString(), msg.Ilock);

                                this.Refresh(null, null); //redraw for new message
                            }
                            else
                            {
                                //bad lock? dunno
                                //-throw exception
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
                                        string end = text.Substring(msg.Ilock.endC+1);
                                        string txt = begin + msg.message.text + end;
                                        //finally set value
                                        messageIndex[msg.Ilock.id.toString()].text = (txt!=""? txt : " [message text deleted]");
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
                            {
                                this.nodes = msg.value;
                            }
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

        #region IUI Members

        QS.Fx.Endpoint.Classes.IExportedUI QS.Fx.Object.Classes.IUI.UI
        {
            get { return this.myendpoint; }
        }

        #endregion

        #region ICheckpointedCommunicationChannelClient<IMessage, IChatState> Members

        IChatState QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage, IChatState>.Checkpoint()
        {
            //return new IChatState();
            return new IChatState(messageList.ToArray(),(new List<Lock>(this.lockList.Values)).ToArray(),nodes);
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage, IChatState>.Initialize(IChatState _checkpoint)
        {
            lock (incoming)
            {
                if (_checkpoint != null) //not first to join
                {
                        //load messages
                    if (_checkpoint.messages != null)
                    {
                        foreach (Message m in _checkpoint.messages)
                        {
                            messageList.Add(m);
                            messageIndex.Add(m.id.toString(), m);
                        }
                    }
                    if (_checkpoint.locks != null)
                    {
                        //load locks
                        foreach (Lock l in _checkpoint.locks)
                        {
                            lockTimers.Enqueue(l);
                            lockList.Add(l.toString(), l);
                        }
                    }
                   
                    myNodeNum = _checkpoint.nodes; //set my number
                    this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_EXPOSE, DateTime.Now, null, null, (myNodeNum + 1)));
                }
                else //am first to join
                {
                    nodes = 1; //total nodes in group
                    //that's it, everything else should be initalized already
                }
                refreshView();
            }
        }

        void QS.Fx.Interface.Classes.ICheckpointedCommunicationChannelClient<IMessage, IChatState>.Receive(IMessage _message)
        {
            //upon receiving a message, simply throw it into the queue and let the digester pick it up
            if (this.InvokeRequired)
            {
                bool pendingcallback = incoming.Count > 0;
                incoming.Enqueue(_message);
                if (!pendingcallback)
                    this.StartDigester();
            }
        }

        #endregion
        //submit button
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (!editState) //send message button
            {
                if (textBox1.Text != string.Empty)
                {
                    this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_SEND_MSG, DateTime.Now, null, new Message(new MessageId(myNodeNum, messages++), textBox1.Text), 0));
                    textBox1.Text = "";
                }
            }
            else //update values
            {

            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        //delete button
        private void button2_Click(object sender, EventArgs e)
        {
            if (editState)
            {
             //todo, add confirmation dialog
                
            }
        }
        //renew button
        private void button3_Click(object sender, EventArgs e)
        {
            if (editState)
            {

            }
        }
    }
}
