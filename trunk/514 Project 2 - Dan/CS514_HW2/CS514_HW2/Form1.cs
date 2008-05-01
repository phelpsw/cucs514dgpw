/**
 * Daniel Gicklhorn
 * CS514 Spring 2007
 * Cornell University
 * Homework #2 Implementation (Primary)
 * */

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

            //customUnderlines = new CustomPaintTextBox(textBox1);
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

        public int STD_LEASE = 30; //standard lease time for a lock

        /** CURRENT EDITING STATE VARIABLES **/

        private bool editState = false; //not editing by default

        private Lock activeLock = null; //currently active lock element

        /** **/

        //structure to hold preemptive lock based on selection
        private Lock provisionalLock = null;

        //comparison operator for PriorityQueue
        private LockComparer icmp = new LockComparer();

        //comparison operator for Sorting Locks
        private LockPosComparer poscmp = new LockPosComparer();

        //message queue for incoming recieved messages
        private Queue<IMessage> incoming = new Queue<IMessage>();

        //Queue to allow timer timeouts on locks.
        private PriorityQueue lockTimers;

        //List of Active Locks (refrenced by Lock.toString() method as keys
        private IDictionary<string, Lock> lockList = new Dictionary<string,Lock>();

        
        //List of Viewable Messages (ordered by insertion)
        private List<Message> messageList = new List<Message>();

        //Index to Viewable Messages (not necissarily ordered correctly)
        private IDictionary<string, Message> messageIndex = new Dictionary<string, Message>();


        //retrieve all of the locked items in a specific message
        public List<Lock> getLocksByMessage(MessageId id)
        {
            List<Lock> list = new List<Lock>();

            foreach (Lock l in lockList.Values)
            {
                if (l.id.toString() == id.toString())
                {
                    list.Add(l);
                }
            }
            list.Sort(poscmp);
            return list;
        }

        //Invocation call to start the timer thread
        private void StartTimer()
        {
            if (timer==null) //timer not currently running, else, wait
            {
                System.Threading.TimerCallback cb = new System.Threading.TimerCallback(Timer);
                //begins new timer after 1 second, with 
                timer = new System.Threading.Timer(cb, null, 100, 1000);
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
            bool didupdate = false;
            if (editState) refreshCountdown();
            lock (lockTimers)
            {
                //remove all expired timers
                while (lockTimers.Count != 0 && ((Lock)lockTimers.Peek()).expires.Subtract(DateTime.Now).Seconds < 0)
                {
                    //alert that atleast one update occurred
                    didupdate = true;

                    // if removing currently locked element from local process
                    if (lockList[((Lock)lockTimers.Peek()).toString()].owner == myNodeNum)
                    {
                        //expire current thread
                        endEditInvoke();
                    }

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
            int prev_pos = 0;
            richTextBox1.Clear();
            foreach (Message m in messageList)
            {
                string messageLabel = "[" + m.recieveTime.Hour + ":" + m.recieveTime.Minute + ":" + m.recieveTime.Second + "] Win" + m.id.src_id + ": ";
                string messageText = m.text + "\r\n";
                AppendText(messageLabel,(m.id.src_id==myNodeNum?1:2));
                List<Lock> locks = getLocksByMessage(m.id);
                int prevpos = 0;
                foreach (Lock l in locks)
                {
                    AppendText(messageText.Substring(prevpos, l.startC - prevpos),0);
                    prevpos += (l.startC-prevpos);
                    AppendText(messageText.Substring(prevpos, l.endC - l.startC),3);
                    prevpos += (l.endC - l.startC);
                }
                AppendText(messageText.Substring(prevpos, messageText.Length-prevpos), 0);

                m.char_pos = prev_pos;
                m.label_length = messageLabel.Length;
                m.total_length = messageText.Length + messageLabel.Length;
                prev_pos += messageText.Length + messageLabel.Length;
            }
            richTextBox1.ScrollToCaret();
        }

        //Simply a colorig / Formatting function to write to the richTextBox
        private void AppendText(string message,int type)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;

            if (type == 1) //MY MESSAGE COLOR
            {
                richTextBox1.SelectionColor = Color.Red;
                richTextBox1.SelectionFont = new Font("Courier New", 12, FontStyle.Bold);
            }
            else if (type == 2) //OTHER'S MESSAGE COLOR
            {
                richTextBox1.SelectionColor = Color.Blue;
                richTextBox1.SelectionFont = new Font("Courier New", 12, FontStyle.Bold);
            }
            else if (type == 3) //LOCKED TEXT COLOR
            {
                richTextBox1.SelectionColor = Color.Orange;
                richTextBox1.SelectionFont = new Font("Arial", 12, FontStyle.Regular);
            }
            else //NORMAL TEXT COLOR (BLACK)
            {
                richTextBox1.SelectionColor = Color.Black;
                richTextBox1.SelectionFont = new Font("Arial", 12, FontStyle.Regular);
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

            int time = DateTime.Now.Subtract(activeLock.expires).Seconds;
           if(time  > 0) time = 0;
           label1.Text = "Lease Remain: "+(0-time).ToString() + " seconds.";
        }

        //Invocation method for altering the state to edit mode
        private void startEditInvoke()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.startEdit), null);
            else
                this.startEdit(null, null);
        }

        //Alters the editing sate to begin editing the selected shared text
        private void startEdit(object sender, EventArgs args)
        {
            if (activeLock != null)
            {
                lock (lockTimers)
                {
                    //activeLock should already been loaded to lock requested before running
                    button2.Visible = true;
                    button3.Visible = true;
                    label1.Visible = true;
                    button4.Enabled = false;
                    button1.Text = "Update";
                    editState = true;
                    string text = messageIndex[activeLock.id.toString()].text;
                    textBox1.Text = text.Substring(activeLock.startC, activeLock.endC - activeLock.startC);
                }
            }
        }

        //Invocation method of alering the state from edit mode
        private void endEditInvoke()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new EventHandler(this.endEdit), null);
            else
                this.endEdit(null, null);
        }

        //Alters the editing state to complete editing the shared text
        private void endEdit(object sender, EventArgs args)
        {
            lock (lockTimers)
            {
                button2.Visible = false;
                button3.Visible = false;
                button4.Enabled = false;
                label1.Visible = false;
                button1.Text = "Send";
                editState = false;
                activeLock = null;
                textBox1.Clear();
            }
        }

        //Try and impose the given lock on the overall state,
        //if acceptable, send to the channel
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
                        //update expiration info for local copy of lock
                        lok.expires = DateTime.Now.AddSeconds(lok.time);
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
                        case messageTypes.MSG_TYPE_SEND_MSG:
                            if (msg.message != null)
                            {
                                msg.message.recieveTime = DateTime.Now;
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
                                msg.Ilock.expires = DateTime.Now.AddSeconds(msg.Ilock.time);

                                //add lock to the timers list
                                lockTimers.Enqueue(msg.Ilock);

                                //add lock to the indexed list
                                lockList.Add(msg.Ilock.toString(), msg.Ilock);

                                if (timer==null) StartTimer(); //begin the timer thread

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
        
        //Handle Submit / Update Button
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
                this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_UNLOCK, DateTime.Now, activeLock, new Message(activeLock.id,textBox1.Text), 0));
                endEditInvoke();
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        //Handle Delete Button Click
        private void button2_Click(object sender, EventArgs e)
        {
            if (editState)
            {
             //todo, add confirmation dialog if time :: setting the message parameter to null deletes the specified message
             this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_UNLOCK, DateTime.Now, activeLock, null, 0));
             endEditInvoke();
            }
        }

        //Handle Renew Request Button
        private void button3_Click(object sender, EventArgs e)
        {
            if (editState)
            {
                //extend on this end
                activeLock.expires = DateTime.Now.AddSeconds(activeLock.time); 
                //send out globally
                this.mychannelendpoint.Interface.Send(new IMessage(messageTypes.MSG_TYPE_LOCK, DateTime.Now, activeLock, null, 0));
            }
        }

        //Handle Lock BUttion
        private void button4_Click(object sender, EventArgs e)
        {

            if (tryLock(provisionalLock)) //yes, successful;
            {
                activeLock = provisionalLock;
                provisionalLock = null;
                label1.Visible = false;
                startEditInvoke();
            } else { //fail
                label1.Text = "The Lock conflicts.";
                label1.Visible = true;
            }
        }

        //activate button4 (Lock Button) or not if selected text is acceptable
        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength > 0 && !editState)
            {
                bool notfound = true;
                Message select;
                foreach (Message m in messageList)
                {
                    //see if the selection is contained within a displayed message
                    if (richTextBox1.SelectionStart >= m.char_pos && richTextBox1.SelectionLength <= m.total_length - (richTextBox1.SelectionStart-m.char_pos))
                    {
                        if ((richTextBox1.SelectionStart-m.char_pos) > m.label_length-2) //make sure not selectin the label
                        {
                            List<Lock> ls = getLocksByMessage(m.id);
                            bool conflict = false;

                            int startpos = richTextBox1.SelectionStart - (m.char_pos + m.label_length);
                            startpos = (startpos < 0 ? 0 : startpos);
                            int endpos = startpos + richTextBox1.SelectionLength;
                            endpos = (endpos > m.text.Length ? m.text.Length : endpos);

                            foreach (Lock l in ls)
                            {
                                if((l.startC < startpos && l.endC > startpos) //start falls within range
                                     || (l.startC <= endpos && l.endC >= endpos) //end falls within range
                                     || (l.startC <= startpos && l.endC >= endpos) //encapsulate a 
                                     || (l.startC >= startpos && l.endC <= endpos)) //encapsulate b
                                {
                                    conflict = true;
                                    break;
                                }
                            }
                            if (!conflict)
                            {
                                notfound = false;
                                button4.Enabled = true;
                                provisionalLock = new Lock(myNodeNum, m.id, startpos, endpos, this.STD_LEASE);
                            }
                   
                        }
                        break;
                    }
                }
                if (notfound)
                {
                    button4.Enabled = false;
                    provisionalLock = null;
                }
            }
        }

        //HANDLE ALTERNATE CLICK EVENTS TO ALTER SELECTION STATUS
        private void richTextBox1_MouseClick(object sender, MouseEventArgs e)
        {
            richTextBox1_SelectionChanged(null, null);
        }
    }
}
