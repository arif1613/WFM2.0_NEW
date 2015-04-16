using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml;

namespace Test
{
    
    
    /// <summary>
    ///This is a test class for MppXmlTranslatorTest and is intended
    ///to contain all MppXmlTranslatorTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MppXmlTranslatorTest
    {


        private TestContext testContextInstance;
        private static ContentData _contentData;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            //_contentData = new ContentData();

            //_contentData.ObjectID = 101;
            //_contentData.HostID = "0001";
            //_contentData.Name = "Lord of the rings";
            //_contentData.ExternalID = "202";

            //_contentData.EventPeriodFrom = new DateTime(2012,1,2, 10,11,12);
            //_contentData.EventPeriodTo = new DateTime(2013,4,5, 6,7,8);
            
            //PublishInfo publishInfo1 = new PublishInfo();
            //publishInfo1.from = new DateTime(2014,5,6, 7,8,9); 
            //publishInfo1.to = new DateTime(2015,6,7, 8,9,10); 
            //publishInfo1.Region = "World";
            //publishInfo1.PublishState = PublishState.Created;
            //publishInfo1.DeliveryMethod = DeliveryMethod.Stream;
            //_contentData.PublishInfos.Add(publishInfo1);
                   
            //PublishInfo publishInfo2 = new PublishInfo();
            //publishInfo2.from = new DateTime(2016,7,8, 9,10,11); 
            //publishInfo2.to = new DateTime(2017,8,9, 10,11,12); 
            //publishInfo2.Region = "SWE";
            //publishInfo2.PublishState = PublishState.Created;
            //publishInfo2.DeliveryMethod = DeliveryMethod.Stream;
            //_contentData.PublishInfos.Add(publishInfo2);
                
            //_contentData.ProductionYear = 2014;
            //_contentData.RunningTime = new TimeSpan(1, 30, 10);
        
            //_contentData.Properties.Add(new Property("Author", "J.R.R. Tolkien"));
            //_contentData.Properties.Add(new Property("Director", "Peter Jackson"));
            
            //LanguageInfo languageInfo = new LanguageInfo();
            //languageInfo.ISO = "ENG";
            //languageInfo.Title = "Lord of the rings";
            //languageInfo.ShortDescription = "A hobbits journey.";
            //languageInfo.LongDescription = "An innocent hobbit of The Shire journeys with eight companions to the fires of Mount Doom to destroy the One Ring and the dark lord Sauron forever.";
            //languageInfo.SubtitleURL = "ContentData/SubtitleFiles/tvnorge/278400_1.smi";
            //_contentData.LanguageInfos.Add(languageInfo);

            //Image image = new Image();
            //image.ClientGUIName = "Generic GUI";
            //image.Classification = "SmallImage";
            //image.URI = "test11_850_0_SmallImage_THUMB.jpg";
            //languageInfo.Images.Add(image);

            //Image image2 = new Image();
            //image2.ClientGUIName = "Generic GUI";
            //image2.Classification = "LargeImage";
            //image2.URI = "test11_850_0_LargeImage_THUMB.jpg";
            //languageInfo.Images.Add(image2);
        
        
            //Asset asset = new Asset();
            //asset.ObjectID = 1001;
            //asset.Name = "movie_1.mpg";
            //asset.DeliveryMethod = DeliveryMethod.Stream;
            //asset.FileSize = 1000;
            //asset.Bitrate = 850;
            //asset.IsTrailer = false;
            //asset.LanguageISO = "ENG";
            //asset.Properties.Add(new Property("DeviceType", "TV"));
            //asset.Properties.Add(new Property("DeviceType", "Iphone"));
            //asset.Properties.Add(new Property("RunningTime", "1:10:10"));
            //_contentData.Assets.Add(asset);
        
            //Asset asset2 = new Asset();
            //asset2.ObjectID = 1002;
            //asset2.Name = "movie_2.mpg";
            //asset2.DeliveryMethod = DeliveryMethod.Stream;
            //asset2.FileSize = 2000;
            //asset2.Bitrate = 1250;
            //asset2.IsTrailer = false;
            //asset2.LanguageISO = "ENG";
            //asset2.Properties.Add(new Property("DeviceType", "TV"));
            //asset2.Properties.Add(new Property("DeviceType", "Iphone"));
            //asset2.Properties.Add(new Property("RunningTime", "1:10:10"));
            //_contentData.Assets.Add(asset2);
                    
        
            //ContentRightsOwner contentRightsOwner = new ContentRightsOwner();
            //contentRightsOwner.ObjectID = 2001;
            //contentRightsOwner.Name = "MPS";
            //_contentData.ContentRightsOwner = contentRightsOwner;
   
            //ContentAgreement contentAgreements = new ContentAgreement();
            //contentAgreements.ObjectID = 2101;
            //contentAgreements.Name = "MPS VOD";
            //_contentData.ContentAgreements.Add(contentAgreements);
        
            //ContentAgreement contentAgreements2 = new ContentAgreement();
            //contentAgreements2.ObjectID = 2102;
            //contentAgreements2.Name = "MPS Free";
            //_contentData.ContentAgreements.Add(contentAgreements2);
        }
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for TranslateContentDataToXml
        ///</summary>
        [TestMethod()]
        public void TranslateContentDataToXmlTest()
        {
            MppXmlTranslator target = new MppXmlTranslator(); // TODO: Initialize to an appropriate value
            ContentData contentData = _contentData;
            XmlDocument expected = null; // TODO: Initialize to an appropriate value
            XmlDocument actual;
            actual = target.TranslateContentDataToXml(contentData);
            //Assert.AreEqual(expected, actual);
            //Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for TranslateXmlToContentData
        ///</summary>
        [TestMethod()]
        public void TranslateXmlToContentDataTest()
        {
            MppXmlTranslator target = new MppXmlTranslator(); // TODO: Initialize to an appropriate value
            ContentData contentData = _contentData;
            XmlDocument contentXml = null; // TODO: Initialize to an appropriate value
            ContentData expected = null; // TODO: Initialize to an appropriate value
            ContentData actual;

            contentXml = target.TranslateContentDataToXml(contentData);
            //actual = target.TranslateXmlToContentData(contentXml);
            //Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
