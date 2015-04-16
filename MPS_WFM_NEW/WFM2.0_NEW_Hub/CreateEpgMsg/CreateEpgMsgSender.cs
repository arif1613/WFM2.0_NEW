using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NodaTime;
using WfmHubWorker.Models;

namespace CreateEpgMsg
{
    public class CreateEpgMsgSender : RoleEntryPoint
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent _runCompleteEvent = new ManualResetEvent(false);
        private bool isstopped;
        private TopicClient _client;

        public override void Run()
        {
            Trace.TraceInformation("CreateEpgMsg is running");
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
                        Timestamp = Instant.FromDateTimeUtc(DateTime.Now.ToUniversalTime())
                    };
                    DateTime dt = tcommand.Timestamp.ToDateTimeUtc();
                    var message = new BrokeredMessage
                    {
                        CorrelationId = tcommand.CorrelationId.ToString(),
                        MessageId = tcommand.MessageId.ToString()
                    };
                    message.Properties.Add("CmdMessage", "EPG Ingest Task");
                    message.Properties.Add("CmdMsgId", tcommand.MessageId);
                    message.Properties.Add("TimeStamp", dt);
                    message.Properties.Add("CausationId", tcommand.CausationId);
                    _client.SendAsync(message);
                    
                    ////2 min interval
                    for (int i = 0; i < 2; i++)
                    {
                        Thread.Sleep(60000);
                    }

                    //Thread.Sleep(10000);
                    
                    Trace.Flush();
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
            bool result = base.OnStart();
            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("CreateEpgMsg is stopping");

            _cancellationTokenSource.Cancel();
            //_runCompleteEvent.WaitOne();
            base.OnStop();

            Trace.TraceInformation("CreateEpgMsg has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
