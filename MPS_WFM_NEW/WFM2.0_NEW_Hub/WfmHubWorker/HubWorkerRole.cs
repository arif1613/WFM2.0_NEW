using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using NodaTime;
using WfmHubWorker.Models;

namespace WfmHubWorker
{
    public class HubWorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private bool isstopped;
        private TopicClient _client, _client1, _client2;
        public override void Run()
        {
            Trace.TraceInformation("WFM Status:MPS Wfm is running....");
            while (!isstopped)
            {
                try
                {
                    var tcommand = new TopicCommand
                    {
                        CausationId = Guid.NewGuid(),
                        CorrelationId = Guid.NewGuid(),
                        Id = Guid.NewGuid(),
                        MessageId = Guid.NewGuid(),
                        Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow)
                    };
                    string command = ConfigurationSettings.AppSettings["FileWatchTask"];
                    DateTime dt = tcommand.Timestamp.ToDateTimeUtc();
                    var message = new BrokeredMessage
                    {
                        CorrelationId = tcommand.CorrelationId.ToString(),
                        MessageId = tcommand.MessageId.ToString()
                    };
                    message.Properties.Add("CmdMessage", command);
                    message.Properties.Add("CmdMsgId", tcommand.MessageId);
                    message.Properties.Add("TimeStamp", dt);
                    message.Properties.Add("CausationId", tcommand.CausationId);
                    _client.SendAsync(message);
                    Thread.Sleep(30000);

                    var message1 = new BrokeredMessage
                    {
                        CorrelationId = tcommand.CorrelationId.ToString(),
                        MessageId = tcommand.MessageId.ToString()
                    };
                    message1.Properties.Add("CmdMessage", "Check Work Folder");
                    message1.Properties.Add("CmdMsgId", tcommand.MessageId);
                    message1.Properties.Add("TimeStamp", dt);
                    message1.Properties.Add("CausationId", tcommand.CausationId);
                    _client1.SendAsync(message1);
                    
                    //2 min interval
                    //for (int i = 0; i < 2; i++)
                    //{
                    //    Thread.Sleep(60000);
                    //}
                    //Thread.Sleep(10000);

                    //var message2 = new BrokeredMessage
                    //{
                    //    CorrelationId = tcommand.CorrelationId.ToString(),
                    //    MessageId = tcommand.MessageId.ToString()
                    //};
                    //message2.Properties.Add("CmdMessage", "Clean Up Folders");
                    //message2.Properties.Add("CmdMsgId", tcommand.MessageId);
                    //message2.Properties.Add("TimeStamp", dt);
                    //message2.Properties.Add("CausationId", tcommand.CausationId);
                    //_client2.SendAsync(message2);
                    //Trace.Flush();
                }
                catch (MessagingException e)
                {
                    if (!e.IsTransient)
                    {
                        Trace.WriteLine(e.Message);
                        throw;
                    }
                }
                catch (OperationCanceledException e)
                {
                    if (!isstopped)
                    {
                        Trace.WriteLine(e.Message);
                    }
                }
                _runCompleteEvent.Set();
            }
        }
        public override bool OnStart()
        {
            //send command message to topic
            var connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");
            const string topicName = "WfmCmdSender";
            _client = TopicClient.CreateFromConnectionString(connectionString, topicName);
            _client1 = TopicClient.CreateFromConnectionString(connectionString, topicName);
            _client2 = TopicClient.CreateFromConnectionString(connectionString, topicName);
            bool result = base.OnStart();
            return result;
        }
        public override void OnStop()
        {
            Trace.TraceInformation("WfmHubWorker is stopping");
            _cancellationTokenSource.Cancel();
            //_runCompleteEvent.WaitOne();
            base.OnStop();
            Trace.TraceInformation("WfmHubWorker has stopped");
        }
    }
}
