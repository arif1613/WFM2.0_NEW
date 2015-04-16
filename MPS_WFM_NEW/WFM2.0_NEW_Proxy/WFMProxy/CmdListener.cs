using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml;
using Microsoft.ServiceBus.Messaging;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;

namespace WFMProxy
{
    public class CmdListener
    {
        private static string ProxyStatus { get; set; }
        private static SubscriptionClient _cmdReceiverClient;
        private static BrokeredMessage _message;
        private static DateTime _dt;

        public CmdListener()
        {
            ProxyStatus = "Free";
            const string connectionString =
                "Endpoint=sb://mpswfm.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=NAtTBQ6YbyQ2tdRcYTrslNQTmuRId/IfafHHgXSFljU=";
            _cmdReceiverClient = SubscriptionClient.CreateFromConnectionString(connectionString, "WfmCmdSender",
                "WfmCmdSenderSub", ReceiveMode.ReceiveAndDelete);
            var d = new XmlDocument();
            string path = ConfigurationSettings.AppSettings["WFMConfigFileName"];
            d.Load(path);
            //creating configuration steps
            Config.Init(d);
        }

        public void WfmCmdListener()
        {
            string command = null;
            while (ProxyStatus == "Free")
            {
                _message = _cmdReceiverClient.Receive();
                if (_message != null)
                {
                    ProxyStatus = "Busy";
                    if (_message.Properties.ContainsKey("CmdMessage"))
                    {
                        command = _message.Properties["CmdMessage"].ToString();
                        _dt = Convert.ToDateTime(_message.Properties["TimeStamp"]);
                    }
                }

                if (_message != null)
                {
                    Console.WriteLine("Task Running now: " + command);

                    if (_message.Properties.ContainsKey("FileName"))
                    {
                        //string xmlFileName = _message.Properties["FileName"].ToString();
                        //switch (command)
                        //{
                        //    case "Move To Work Folder":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            new MoveToWorkFoldermsg(_message, _dt);
                        //        }
                        //        break;
                        //    case "Move To Reject Folder":
                        //        Thread.Sleep(1000);

                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            new MoveToRejectFoldermsg(_message, _dt);
                        //        }
                        //        break;
                        //    case "Create ContegoVODcontent And Encode":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            var ms = new CreateContegoVODmsg(_message, _dt);
                        //            ms.CreateContegoVOD();
                        //        }
                        //        break;
                        //    case "Move Channel Files To Work Folder":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            new ChannelFileMovingmsg(_message, _dt);
                        //        }
                        //        break;
                        //    //case "Clean Up Folders":
                        //    //    Thread.Sleep(1000);
                        //    //    new RejectFolderCleanUp(_message, _dt);
                        //    //    break;
                        //    case "Publish Channel to MPP5":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            var publishChannelToMpp = new CreateMpp5Content(_message, _dt);
                        //            publishChannelToMpp.CreateMppChannelContent();
                        //            //publishChannelToMpp.createA_LiveEventinMPP();

                        //            //Add channel content in RavenDB
                        //            //ContentData liveEventModel = publishChannelToMpp.GetEventContentData();

                        //            ChannelXmlTranslator channelXmlTranslator = new ChannelXmlTranslator();
                        //            CreateContegoVODmsg createContegoVoDmsg = new CreateContegoVODmsg(xmlFileName);
                        //            FileInfo configFileInfo = createContegoVoDmsg.getConfigFileInfo();
                        //            IngestConfig ingestConfig = createContegoVoDmsg.GetIngestConfig(configFileInfo);
                        //            XmlDocument xd=new XmlDocument();
                        //            xd.Load(xmlFileName);
                        //            ContentData cd = channelXmlTranslator.TranslateXmlToContentData(ingestConfig,xd);
                        //            new AddChannelToRavenDB(cd);

                        //        }
                        //        break;
                        //    case "Publish Vod to MPP5":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            var publishVodToMpp = new CreateMpp5Content(_message, _dt);
                        //            publishVodToMpp.CreateMppVod();
                        //            if (!publishVodToMpp.createA_VodinMPP())
                        //            {
                        //                new MoveToRejectFoldermsg(_message, _dt);
                        //            }
                        //        }
                        //        break;

                        //    case "MPP sync task":
                        //        Thread.Sleep(1000);
                        //        if (File.Exists(xmlFileName))
                        //        {
                        //            bool isHidden = ((File.GetAttributes(xmlFileName) & FileAttributes.Hidden) == FileAttributes.Hidden);

                        //            if (!isHidden)
                        //            {
                        //                Stopwatch totalTimer = new Stopwatch();
                        //                totalTimer.Start();
                        //                Console.WriteLine("Mpp Sync for feed {0} started....", xmlFileName);
                        //                //adding programs to RavenDB
                        //                var mppSyncTaskMsg = new MppSyncTaskMsg(xmlFileName);
                        //                mppSyncTaskMsg.GetAllProgramsFromEpgFeed();
                        //                mppSyncTaskMsg.AddProgramsInRavenDb();
                        //                File.SetAttributes(xmlFileName, FileAttributes.Hidden);
                        //                totalTimer.Stop();
                        //                Console.WriteLine("Epg Ingest finished in " + totalTimer.ElapsedMilliseconds / 1000 + " sec.");
                        //                totalTimer.Reset();
                        //            }
                        //        }
                        //        break;
                    }

                    else
                    {
                        switch (command)
                        {
                            case "FileWatchTask":
                                Thread.Sleep(1000);
                                var fileWatchTask = new FileWatchTask();
                                fileWatchTask.DoExecute();
                                break;
                            case "Check Work Folder":
                                Thread.Sleep(1000);
                                //new CheckWorkFolderMsg(_message, _dt);
                                break;
                            case "EPG Ingest Task":
                                Thread.Sleep(1000);
                                //var epgIngestTaskMsg = new EpgIngestTaskMsg(_message, _dt);

                                //if (string.IsNullOrEmpty(epgIngestTaskMsg.validateChannelConfigFile()))
                                //{
                                //    epgIngestTaskMsg.Epg_Ingest_in_MPP();
                                //}
                                //else
                                //{
                                //    Console.WriteLine(epgIngestTaskMsg.validateChannelConfigFile());
                                //}
                                break;
                        }
                    }
                }
                ProxyStatus = "Free";
            }
        }

    }
}

