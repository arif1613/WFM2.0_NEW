using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.TestData.Services.Cubi;
using Machine.Specifications;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Test.Developer.Core.Communication.CubiTVMiddlewareServiceWrapperTest
{
    public class With_a_new_CubiServiceWrapper
    {
        protected static CubiTVMiddlewareServiceWrapper cubiWrapper;
        protected static FakeMiddleWareRestApiCaller fakeMiddleWareRestApiCaller;

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

                                            var serviceConfig = Config.GetConfig().ServiceConfigs.First();

                                            fakeMiddleWareRestApiCaller = new FakeMiddleWareRestApiCaller();
                                            cubiWrapper = new CubiTVMiddlewareServiceWrapper(serviceConfig, fakeMiddleWareRestApiCaller);
                                        };
    }

    public class When_Update_Subscription_Price_For_A_New_Channel_Content_With_Catchup_And_NPVR_Enabled : With_a_new_CubiServiceWrapper
    {
        private Because of = () =>
                                 {
                                     MultipleServicePrice subPrice = new MultipleServicePrice();
                                     subPrice.LongDescription = "ServiceExtPriceID=1010,ConaxContegoProductID=1011";
                                     subPrice.Title = "Test Sub";
                                     subPrice.Price = 0;
                                     subPrice.ContentsIncludedInPrice = new List<UInt64>(){621};
                                     
                                     ContentData channelContent = new ContentData();
                                     channelContent.Properties.Add(new Property(VODnLiveContentProperties.ServiceExtContentID, "513697793:1001"));
                                     

                                     cubiWrapper.UpdateSubscriptionPrice(subPrice, channelContent,
                                                                         UpdatePackageOfferType.AddContent);

                                 };

        private It Should_Update_Price_With_Channel_Catchup_NPVR_Ids = () =>
                                             {
                                                 String offerXML = fakeMiddleWareRestApiCaller.DB["package_offers/1010"];
                                                 XmlDocument offerDoc = new XmlDocument();
                                                 offerDoc.LoadXml(offerXML);

                                                 Boolean channelIdFound = false;
                                                 foreach (XmlNode channelIdNode in offerDoc.SelectNodes("//channel-id"))
                                                 {
                                                     if (channelIdNode.InnerText == "1001")
                                                         channelIdFound = true;
                                                 }
                                                 channelIdFound.ShouldBeTrue();

                                                 Boolean catchupChannelIdFound = false;
                                                 foreach (XmlNode catchuupChannelIdNode in offerDoc.SelectNodes("//catchup-channel-id"))
                                                 {
                                                     if (catchuupChannelIdNode.InnerText == "1002")
                                                         catchupChannelIdFound = true;
                                                 }
                                                 catchupChannelIdFound.ShouldBeTrue();

                                                 Boolean npvrChannelIdFound = false;
                                                 foreach (XmlNode npvrChannelIdNode in offerDoc.SelectNodes("//npvr-channel-id"))
                                                 {
                                                     if (npvrChannelIdNode.InnerText == "1003")
                                                         npvrChannelIdFound = true;
                                                 }
                                                 npvrChannelIdFound.ShouldBeTrue();
                                             };

        private Cleanup Test = () =>
        {
            fakeMiddleWareRestApiCaller.ClearMemory();
        };
    }

    public class When_EPG_Has_2_Pages_Of_NPVR_Recordings_To_Fetch : With_a_new_CubiServiceWrapper
    {
        static List<NPVRRecording> npvrRecordings = new List<NPVRRecording>();

        private Because of = () =>
        {            

            npvrRecordings = cubiWrapper.GetNPVRRecording("010");
        };

        private It Should_Fetch_All_pages_Successfully = () =>
        {
            npvrRecordings.Count.ShouldEqual(4);
        };

        private Cleanup Test = () =>
        {
            fakeMiddleWareRestApiCaller.ClearMemory();
        };
    }
}
