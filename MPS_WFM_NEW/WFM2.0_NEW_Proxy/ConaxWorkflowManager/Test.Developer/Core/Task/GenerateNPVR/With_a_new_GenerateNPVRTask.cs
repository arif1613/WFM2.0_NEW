using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.MPP;
using Machine.Specifications;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.MPP;


namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.Task.GenerateNPVR
{
    public class With_a_new_GenerateNPVRTask
    {
        protected static GenerateNPVRTask GenerateNpvrTask;
        protected static FakeMPPService mppService;
        protected static MppXmlTranslator translator = new MppXmlTranslator();
        private Establish Context = () =>
                                        {
                                            CubiTVMiddlewareManager.Clean();
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


                                            GenerateNpvrTask = new GenerateNPVRTask();
                                            mppService = (FakeMPPService)
                                                MPPIntegrationServiceManager.InstanceWithPassiveEvent.MPPService;
                                        };
    }

    public class When_A_EPG_IS_AIRED_Using_CodeShop_and_SeaWell : With_a_new_GenerateNPVRTask
    {
        private Because of = () =>
                                 {
                                     var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
                                     mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.CodeShopSmoothCatchupHandler";
                                     mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.SeaWellHLSCatchupHandler";
                                     mppService.AddContentToMemory(624, "epg_content_624.xml");
                                     GenerateNpvrTask.DoExecute();

                                 };

        private It Should_successfully_executed_With_5_Assets_Archived = () =>
                                                      {
                                                          String contentXML = mppService.GetContentForObjectId("", 624);
                                                          XmlDocument contentDoc = new XmlDocument();
                                                          contentDoc.LoadXml(contentXML);
                                                          ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];

                                                          List<Property> NPVRAssetProperties = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content, NPVRAssetArchiveState.Archived);
                                                          
                                                          NPVRAssetProperties.Count.ShouldEqual(5);
                                                      };

        private Cleanup Test = () =>
                                   {
                                       mppService.CleanMemory();
                                   };
    }

    public class When_A_EPG_IS_AIRED_Using_Harmonic_and_NullHLS : With_a_new_GenerateNPVRTask
    {
        private Because of = () =>
        {
            var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.HarmonicSmoothCatchupHandler";
            mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.NullHLSCatchupHandler";
            mppService.AddContentToMemory(624, "epg_content_624.xml");
            GenerateNpvrTask.DoExecute();

        };

        private It Should_successfully_executed_With_3_Assets_Archived = () =>
        {
            String contentXML = mppService.GetContentForObjectId("", 624);
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(contentXML);
            ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];

            List<Property> NPVRAssetProperties = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content, NPVRAssetArchiveState.Archived);

            NPVRAssetProperties.Count.ShouldEqual(3);
        };

        private Cleanup Test = () =>
        {
            mppService.CleanMemory();
        };
    }

    public class When_A_EPG_IS_AIRED_Run_GenerateNPVR_3_Times_Using_Harmonic_and_NullHLS : With_a_new_GenerateNPVRTask
    {
        private Because of = () =>
                                 {
                                     var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
                                     mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.HarmonicSmoothCatchupHandler";
                                     mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.NullHLSCatchupHandler";
                                     mppConfig.RequestForContentRecordingsTimeout = 0;
                                     mppService.AddContentToMemory(624, "epg_content_624.xml");
                                     GenerateNpvrTask.DoExecute();
                                     GenerateNpvrTask.DoExecute();
                                     GenerateNpvrTask.DoExecute();
                                     
                                 };

        private It Should_Have_Archived_As_NPVRRecordingsstState_ = () =>
                                   {                                       
                                       MppXmlTranslator translator = new MppXmlTranslator();
                                       String xmlString = mppService.GetContentForObjectId("", 624);
                                       XmlDocument contentDoc = new XmlDocument();
                                       contentDoc.LoadXml(xmlString);
                                       ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];
                                       Property NPVRRecordingsstStateProerpty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRRecordingsstState));
                                       NPVRRecordingsstStateProerpty.Value.ShouldBeEqualIgnoringCase(NPVRRecordingsstState.Archived.ToString());
                                   };

        private Cleanup Test = () =>
                                   {
                                       mppService.CleanMemory();   
                                   };
    }


    public class When_A_EPG_IS_AIRED_Run_GenerateNPVR_5_Times_When_Cubi_Is_Down_Using_Harmonic_and_NullHLS : With_a_new_GenerateNPVRTask
    {
        private Because of = () =>
        {
            var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.HarmonicSmoothCatchupHandler";
            mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.NullHLSCatchupHandler";

            String cubiwrapperName =
                "MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Cubi.FakeCubiTVMWServiceWrapperWithError";
            foreach (ServiceConfig serviceConfig in Config.GetConfig().ServiceConfigs)
            {
                if (serviceConfig.ConfigParams.ContainsKey("CubiServiceWrapper"))
                {
                    serviceConfig.ConfigParams["CubiServiceWrapper"] = cubiwrapperName;
                }
                else
                {
                    serviceConfig.ConfigParams.Add("CubiServiceWrapper", cubiwrapperName);
                }
            }

            mppConfig.RequestForContentRecordingsTimeout = 0;
            mppService.AddContentToMemory(624, "epg_content_624.xml");
            GenerateNpvrTask.DoExecute();
            GenerateNpvrTask.DoExecute();
            GenerateNpvrTask.DoExecute();
            GenerateNpvrTask.DoExecute();
            GenerateNpvrTask.DoExecute();
        };

        private It Should_Have_Ongoing_As_NPVRRecordingsstState_ = () =>
        {
            MppXmlTranslator translator = new MppXmlTranslator();
            String xmlString = mppService.GetContentForObjectId("", 624);
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(xmlString);
            ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];
            Property NPVRRecordingsstStateProerpty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.NPVRRecordingsstState));
            NPVRRecordingsstStateProerpty.Value.ShouldBeEqualIgnoringCase(NPVRRecordingsstState.Ongoing.ToString());
        };

        private Cleanup Test = () =>
        {
            mppService.CleanMemory();
        };
    }
}
