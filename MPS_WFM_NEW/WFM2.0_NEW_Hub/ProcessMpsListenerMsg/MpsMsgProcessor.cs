using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace ProcessMpsListenerMsg
{
    public class MpsMsgProcessor : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private static string _connectionstring;
        private static string _cmdtopicName;
        static string _msgtopicName;
        private static TopicClient _topicClient;
        private static SubscriptionClient _msgReceiverClient;
        public static NamespaceManager NamespaceManager { get; set; }
        //private bool isstopped;
        public override void Run()
        {
            //_msgReceiverClient.ReceiveAsync();
            while (true)
            {
                BrokeredMessage message = _msgReceiverClient.Receive();
                if (message != null)
                {
                    try
                    {
                        BrokeredMessage br = new BrokeredMessage(message)
                    {
                        CorrelationId = message.CorrelationId
                    };
                        br.Properties.Add("CmdMessage", message.Properties["CmdMessage"]);
                        br.Properties.Add("CmdMsgId", message.Properties["CmdMsgId"]);
                        br.Properties.Add("FileName", message.Properties["FileName"]);
                        br.Properties.Add("TimeStamp", message.Properties["TimeStamp"]);
                        br.Properties.Add("CausationId", Guid.NewGuid());
                        Trace.TraceInformation(br.Properties["CmdMessage"].ToString());
                        Trace.TraceInformation(br.Properties["FileName"].ToString());
                        _topicClient.SendAsync(br);
                    }
                    catch (MessagingException e)
                    {
                        if (!e.IsTransient)
                        {
                            Trace.WriteLine(e.Message);
                            throw;
                        }
                    }
                    _runCompleteEvent.Set();
                }
            }
        }
        public override bool OnStart()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            _connectionstring = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            NamespaceManager = NamespaceManager.CreateFromConnectionString(_connectionstring);
            _cmdtopicName = "WfmCmdSender";
            _msgtopicName = "MpsMsgListener";
            //to send command
            _topicClient = TopicClient.CreateFromConnectionString(_connectionstring, _cmdtopicName);
            //to receive message
            _msgReceiverClient = SubscriptionClient.CreateFromConnectionString(_connectionstring, _msgtopicName, "MpsMsgListenerSub", ReceiveMode.ReceiveAndDelete);
            bool result = base.OnStart();
            return result;
        }
        public override void OnStop()
        {
            Trace.TraceInformation("ProcessMpsListenerMsg is stopping");
            this._cancellationTokenSource.Cancel();
            this._runCompleteEvent.WaitOne();
            base.OnStop();
            Trace.TraceInformation("ProcessMpsListenerMsg has stopped");
        }
    }
}

