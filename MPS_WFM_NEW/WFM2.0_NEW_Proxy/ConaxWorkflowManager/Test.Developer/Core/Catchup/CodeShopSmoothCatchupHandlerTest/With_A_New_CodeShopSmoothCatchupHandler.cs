using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using Machine.Specifications;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.Catchup.CodeShopSmoothCatchupHandlerTest
{
    public class With_A_New_CodeShopSmoothCatchupHandler
    {
        protected static CodeShopSmoothCatchupHandler handler;
        protected static ContentData content;
        protected static UInt64 serviceObjectId;
        protected static String languageIso;
        protected static NPVRRecording recording;
        protected static EPGChannel channel;

        private Establish Context = () =>
                                        {
                                            String appPath;
                                            appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                                            appPath = appPath.Replace(@"bin\Debug", @"Core\TestData").Replace(@"file:\", "");

                                            String wfmConfigXMlPath = Path.Combine(appPath,
                                                                                   "ConaxWorkflowManagerConfig.xml");
                                            XmlDocument confDoc = new XmlDocument();
                                            confDoc.Load(wfmConfigXMlPath);
                                            Config.Init(confDoc);

                                            // set epg channel config
                                            var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
                                            mppConfig.EPGChannelConfigXML = Path.Combine(appPath,
                                                                                   "EPGChannelConfig.xml");

                                            handler = new CodeShopSmoothCatchupHandler();
                                            languageIso = "ENG";
                                            // setup content
                                            Asset asset = new Asset();
                                            asset.Name = "2020";
                                            asset.LanguageISO = languageIso;
                                            asset.Properties.Add(new Property(VODnLiveContentProperties.DeviceType, DeviceType.STB.ToString()));
                                            asset.Properties.Add(new Property(VODnLiveContentProperties.AssetType, AssetType.NPVR.ToString()));
                                            asset.Properties.Add(new Property(CatchupContentProperties.NPVRAssetStarttime, "2013-10-10 09:57:00"));
                                            asset.Properties.Add(new Property(CatchupContentProperties.NPVRAssetEndtime, "2013-10-10 10:35:00"));

                                             
           
                                            content = new ContentData();
                                            content.ID = 1010;
                                            content.Assets.Add(asset);

                                            channel = CatchupHelper.GetAllEPGChannels()[0];
                                            serviceObjectId = 3343361;
                                        };

        public static Int32 GetTime(String timeString, String pattern)
        {
            Match match = Regex.Match(timeString,
                                               pattern,
                                               RegexOptions.IgnoreCase);
            if (match.Success)
            {
                String timeStr = match.Value.Split('=')[1];
                return Int32.Parse(timeStr);
            }
            return -1;
        }
    }

    public class When_Having_Recording_With_Guards_1Min_and_5Min :
        With_A_New_CodeShopSmoothCatchupHandler
    {
        static String result;
        private Because of = () =>
                                 {
                                     recording = new NPVRRecording();
                                     recording.Start = new DateTime(2013, 10, 10, 9, 59, 0);
                                     recording.End = new DateTime(2013, 10, 10, 10, 35, 0);
                                     
                                     result = handler.GetAssetUrl(content, serviceObjectId, languageIso, DeviceType.STB, recording, channel);
                                 };

        private It Should_Have_Correct_vbegin_And_vend = () =>
                                     {
                                         Int32 vbegin = GetTime(result, @"vbegin=\d+");
                                         Int32 vend = GetTime(result, @"vend=\d+");

                                         vbegin.ShouldEqual(120);
                                         vend.ShouldEqual(2280);
                                     };
    }

    public class When_Having_Recording_With_Guards_3Min_And_1Min :
        With_A_New_CodeShopSmoothCatchupHandler
    {
        static String result;
        private Because of = () =>
        {
            recording = new NPVRRecording();
            recording.Start = new DateTime(2013, 10, 10, 9, 57, 0);
            recording.End = new DateTime(2013, 10, 10, 10, 31, 0);

            result = handler.GetAssetUrl(content, serviceObjectId, languageIso, DeviceType.STB, recording, channel);
        };

        private It Should_Have_Correct_vbegin_And_vend = () =>
        {
            Int32 vbegin = GetTime(result, @"vbegin=\d+");
            Int32 vend = GetTime(result, @"vend=\d+");

            vbegin.ShouldEqual(0);
            vend.ShouldEqual(2040);
        };
    }
}
