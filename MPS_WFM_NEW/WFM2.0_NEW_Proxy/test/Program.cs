using System;
using System.Configuration;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using WFMProxy;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = new XmlDocument();
            string path = ConfigurationSettings.AppSettings["WFMConfigFileName"];
            d.Load(path);
            Config.Init(d);
            //CreateEpgMsg ce=new CreateEpgMsg();
            //Console.ReadLine();
            var cm = new CmdListener();
            cm.WfmCmdListener();

           //PublishVodToMPPMsg pm = new PublishVodToMPPMsg();
            //pm.getAllVods();
            //pm.getAllLive();
            //pm.getALiveEvent("f7c184b7469d4030d513dce72aaa0e30");
            //pm.createMppChannelContent();
           // pm.getAVod();
           //pm.getAVod_byname("Conax");
            //AddEpGsToRavenDb addEpGsToRavenDb=new AddEpGsToRavenDb();
            //CreateMpp5Content createMpp5Content=new CreateMpp5Content();
            //createMpp5Content.createMppVod();
            //createMpp5Content.deleteAVod("3175cc5a63004c2da794e9ee75a7bf6a");
            //Console.ReadKey();

            //epg feed testing
            //MppSyncTaskMsg mppSyncTaskMsg = new MppSyncTaskMsg(@"C:\MPS\conax\Ingest\SampleEPGFeed\xmltv_file.xml");
            //var e = mppSyncTaskMsg.GetAllProgramsFromEpgFeed();
            //var q = e.Take(5);
            //foreach (var v in q)
            //{
            //    Console.WriteLine(v.ChannelId);
            //    Console.WriteLine(v.MppId);
            //    Console.WriteLine(v.EpisodeNumber.episodeNum);
            //    Console.WriteLine(v.ProgramCreditList.Count);
            //    Console.WriteLine(v.Description);
            //    Console.WriteLine("-------------");
            //}

            Console.ReadLine();
        }
    }
}
