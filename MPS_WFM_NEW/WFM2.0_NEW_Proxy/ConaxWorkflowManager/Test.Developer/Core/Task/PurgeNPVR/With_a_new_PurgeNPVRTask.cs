using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Cubi;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.MPP;
using Machine.Specifications;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.Task.PurgeNPVR
{
    public class With_a_new_PurgeNPVRTask
    {
        protected static PurgeNPVRTask PurgeNPVRTask;
        protected static FakeMPPService mppService;
        protected static MppXmlTranslator translator = new MppXmlTranslator();
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


            PurgeNPVRTask = new PurgeNPVRTask();
            mppService = (FakeMPPService)
                MPPIntegrationServiceManager.InstanceWithPassiveEvent.MPPService;
        };
    }

    public class When_A_EPG_IS_MARKED_AS_PURGE_Using_CodeShop_and_SeaWell : With_a_new_PurgeNPVRTask
    {
        private Because of = () =>
        {
            var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.CodeShopSmoothCatchupHandler";
            mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.SeaWellHLSCatchupHandler";
            mppConfig.DBWrapperAssembly = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer";
            mppConfig.DBWrapper = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.WFM.FAKEDBWrapper";
            mppService.AddContentToMemory(624, "epg_content_624_archived.xml");
            PurgeNPVRTask.DoExecute();

        };

        private It Should_Update_5_Assets_With_NotArchived_State = () =>
                                                                       {
                                                                           String test = null;
            String contentXML = mppService.GetContentForObjectId("", 624);
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(contentXML);
            ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];

            List<Property> NPVRAssetProperties = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content, NPVRAssetArchiveState.Purged);

            NPVRAssetProperties.Count.ShouldEqual(5);
        };

        private Cleanup Test = () =>
        {
            mppService.CleanMemory();
        };
    }

    public class When_A_EPG_IS_MARKED_AS_PURGE_Using_Harmonic_and_NullHLS : With_a_new_PurgeNPVRTask
    {
        private Because of = () =>
        {
            var mppConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.SingleOrDefault(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager);
            mppConfig.ConfigParams["SmoothCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.HarmonicSmoothCatchupHandler";
            mppConfig.ConfigParams["HLSCatchUpHandler"] = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup.NullHLSCatchupHandler";
            mppConfig.DBWrapperAssembly = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer";
            mppConfig.DBWrapper = "MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.WFM.FAKEDBWrapper";
            mppService.AddContentToMemory(624, "epg_content_624_archived.xml");
            PurgeNPVRTask.DoExecute();

        };

        private It Should_Update_5_Assets_With_NotArchived_State = () =>
        {
            String contentXML = mppService.GetContentForObjectId("", 624);
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(contentXML);
            ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];

            List<Property> NPVRAssetProperties = ConaxIntegrationHelper.GetAllNPVRAssetArchiveStateByState(content, NPVRAssetArchiveState.Purged);

            NPVRAssetProperties.Count.ShouldEqual(5);
        };

        private Cleanup Test = () =>
        {
            mppService.CleanMemory();
        };
    }

    public class When_Future_EPG_Was_Reported_As_Deleted_By_Cubi : With_a_new_PurgeNPVRTask
    {
        private Because of = () =>
        {
            CubiEPG cubiEPG = new CubiEPG();
            cubiEPG.ExternalID = "010621-100-201308180050";
            cubiEPG.ID = 1;
            ((FakeCubiTVMWServiceWrapper)CubiTVMiddlewareManager.Instance(3343361)).AddcubiEPGForPurge(cubiEPG);
            mppService.AddContentToMemory(2715611137, "epg_content_624_With_future_date.xml");            
            
            
            PurgeNPVRTask.DoExecute();
            
            
        };

        private It Should_Ignore_It_And_Dont_Change_State_Of_The_Future_EPG = () =>
        {
            String contentXML = mppService.GetContentForObjectId("", 2715611137);
            XmlDocument contentDoc = new XmlDocument();
            contentDoc.LoadXml(contentXML);
            ContentData content = translator.TranslateXmlToContentData(contentDoc)[0];

            Property ReadyToNPVRPurgeProperty = ConaxIntegrationHelper.GetReadyToNPVRPurgeProperty(content);

            ReadyToNPVRPurgeProperty.ShouldBeNull();
        };

        private Cleanup Test = () =>
        {
            mppService.CleanMemory();
        };
    }
}
