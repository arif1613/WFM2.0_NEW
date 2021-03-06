﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using System.Xml;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.XmlFunctionality.Plugins
{
    public class ChannelXmlTranslator
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;

        public ContentData TranslateXmlToContentData(IngestConfig ingestConfig, XmlDocument contentXml)
        {
            ContentData content = new ContentData();

            // default values
            content.HostID = ingestConfig.HostID;

            if (!String.IsNullOrEmpty(contentXml.BaseURI))
            {
                String tmpurl = contentXml.BaseURI.Replace("\\", "/");
                String[] tmpurls = tmpurl.Split('/');
                if (tmpurls.Length > 0)
                    content.Properties.Add(new Property(VODnLiveContentProperties.IngestXMLFileName, Path.Combine(tmpurls[tmpurls.Length - 3], tmpurls[tmpurls.Length - 2], tmpurls[tmpurls.Length - 1])));
            }            

            ContentAgreement contentAgreement = new ContentAgreement();
            contentAgreement.Name = ingestConfig.ContentAgreement;
            content.ContentAgreements.Add(contentAgreement);

            content.ContentRightsOwner = new ContentRightsOwner();
            content.ContentRightsOwner.Name = ingestConfig.ContentRightsOwner;
            
            content.Properties.Add(new Property(VODnLiveContentProperties.EnableQA, ingestConfig.EnableQA.ToString()));
            content.Properties.Add(new Property(VODnLiveContentProperties.URIProfile, ingestConfig.URIProfile));
            content.Properties.Add(new Property(VODnLiveContentProperties.MetadataMappingConfigurationFileName, ingestConfig.MetadataMappingConfigurationFileName));
            content.Properties.Add(new Property(VODnLiveContentProperties.ContentType, ContentType.Channel.ToString("G")));

            XmlElement nameNode = (XmlElement)contentXml.SelectSingleNode("Channel/Name");
            content.Name = nameNode.InnerText;

            XmlElement channelIdNode = (XmlElement)contentXml.SelectSingleNode("Channel/ChannelId");
            String channelId = channelIdNode.InnerText;
            if (!channelId.StartsWith("Channel:", StringComparison.OrdinalIgnoreCase))
            {
                channelId = "Channel:" + channelId;
            }
            content.Properties.Add(new Property(VODnLiveContentProperties.CubiChannelId, channelId));

            XmlElement radioChannelNode = (XmlElement)contentXml.SelectSingleNode("Channel/RadioChannel");
            content.Properties.Add(new Property(VODnLiveContentProperties.RadioChannel, radioChannelNode.InnerText));

            XmlElement isAdultNode = (XmlElement)contentXml.SelectSingleNode("Channel/IsAdult");
            if (isAdultNode != null)
                content.Properties.Add(new Property(VODnLiveContentProperties.IsAdult, isAdultNode.InnerText));

            // load default settings.
            ChannelConfiguration defaultChannelconfig = null;
            XmlNode defaultConfigurationNode = contentXml.SelectSingleNode("Channel/DefaultConfiguration");
            if (defaultConfigurationNode != null) {
                defaultChannelconfig = loadServiceConfig(defaultConfigurationNode);
            }

            // load servcie settings
            List<ContentAgreement> agreements = mppWrapper.GetAllServicesForContent(content);
           
            List<string> serviceObjectIds = new List<string>();
            List<ChannelConfiguration> serviceChannelConfigs = new List<ChannelConfiguration>();
            foreach (MultipleContentService service in agreements[0].IncludedServices) {
                serviceObjectIds.Add(service.ObjectID.Value.ToString());
                XmlNode serviceNode = contentXml.SelectSingleNode("Channel/ConfigurationForServices/Service[@serviceObjectId='" + service.ObjectID + "']");
                ChannelConfiguration serviceChannelConfig = new ChannelConfiguration();
                if (serviceNode != null) {
                    // load sservice settings.
                    serviceChannelConfig = loadServiceConfig(serviceNode);
                }

                List<ServiceViewMatchRule> matchrules = mppWrapper.GetServiceViewMatchRules(service);
                serviceChannelConfig.ServiceObjectId = service.ObjectID.Value;
                serviceChannelConfig.ServiceViewLanugageISO = matchrules[0].ServiceViewLanugageISO;

                // copy from default settigns if service settings is missing.
                if (String.IsNullOrWhiteSpace(serviceChannelConfig.Lcn))
                    serviceChannelConfig.Lcn = defaultChannelconfig.Lcn;

                if (String.IsNullOrWhiteSpace(serviceChannelConfig.Uuid))
                    serviceChannelConfig.Uuid = defaultChannelconfig.Uuid;

                if (String.IsNullOrWhiteSpace(serviceChannelConfig.Title))
                    serviceChannelConfig.Title = defaultChannelconfig.Title;

                if (String.IsNullOrWhiteSpace(serviceChannelConfig.BoxCover))
                    serviceChannelConfig.BoxCover = defaultChannelconfig.BoxCover;

                if (serviceChannelConfig.Catchup == null) {
                    serviceChannelConfig.Catchup = defaultChannelconfig.Catchup;
                    if (serviceChannelConfig.Catchup == null)
                        serviceChannelConfig.Catchup = false;
                }

                if (serviceChannelConfig.NPVR == null) {
                    serviceChannelConfig.NPVR = defaultChannelconfig.NPVR;
                    if (serviceChannelConfig.NPVR == null)
                        serviceChannelConfig.NPVR = false;
                }

                // load dvb stream first from default settings.
                var dvbSource = defaultChannelconfig.Assets.Where(a => a.Type == StreamType.DVB);
                List<AssetSource> dvbStreams = new List<AssetSource>();
                dvbStreams.AddRange(dvbSource);
                LoadDefaultAssets(serviceChannelConfig, dvbStreams);

                // load the rest streams from the default settings.
                var otherSource = defaultChannelconfig.Assets.Where(a => a.Type != StreamType.DVB);
                List<AssetSource> otherStreams = new List<AssetSource>();
                otherStreams.AddRange(otherSource);
                LoadDefaultAssets(serviceChannelConfig, otherStreams);

                serviceChannelConfigs.Add(serviceChannelConfig);
            }
            foreach (XmlNode node in contentXml.SelectNodes("Channel/ConfigurationForServices/Service"))
            {
                XmlAttribute attribute = node.Attributes["serviceObjectId"];
                if (attribute != null && !String.IsNullOrEmpty(attribute.Value))
                {
                    if (!serviceObjectIds.Contains(attribute.Value))
                    {
                       throw new Exception("Configuration for services have a serviceObjectId that doesn't exist, objectId = " + attribute.Value);
                    }
                }
                else
                {
                    throw new Exception("Attribute serviceObjectId is missing or has empty value in ConfigurationForServices");
                }
            }

            // Build Content from servcie settings.
            foreach(ChannelConfiguration serviceChannelConfig in serviceChannelConfigs) {

                content.Properties.Add(new Property(VODnLiveContentProperties.EnableCatchUp + ":" + serviceChannelConfig.ServiceObjectId, serviceChannelConfig.Catchup.Value.ToString()));
                content.Properties.Add(new Property(VODnLiveContentProperties.EnableNPVR + ":" + serviceChannelConfig.ServiceObjectId, serviceChannelConfig.NPVR.Value.ToString()));
                content.Properties.Add(new Property(VODnLiveContentProperties.LCN + ":" + serviceChannelConfig.ServiceObjectId, serviceChannelConfig.Lcn));
                if (!String.IsNullOrEmpty(serviceChannelConfig.Uuid))
                    ConaxIntegrationHelper.AddUuidProperty(content, serviceChannelConfig.Uuid, serviceChannelConfig.ServiceObjectId);
                // build languageinfo
                LanguageInfo langInfo = GetLanguageInfo(content, serviceChannelConfig.ServiceViewLanugageISO);
                langInfo.Title = serviceChannelConfig.Title;
                
                Image img = new Image();
                var property = content.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.IngestXMLFileName));
                if (property != null)
                    img.URI = Path.Combine(Path.GetDirectoryName(property.Value), serviceChannelConfig.BoxCover);
                else
                    img.URI = serviceChannelConfig.BoxCover;                

                img.Classification = ingestConfig.DefaultImageClassification;
                img.ClientGUIName = ingestConfig.DefaultImageClientGUIName;
                langInfo.Images.Add(img);

                // add Assets
                foreach(AssetSource source in serviceChannelConfig.Assets) {
                    
                    Asset asset = new Asset();
                    asset.IsTrailer = false;
                    asset.DeliveryMethod = DeliveryMethod.Stream;
                    ////asset.contentAssetServerName = ingestConfig.CAS;
                    asset.LanguageISO = serviceChannelConfig.ServiceViewLanugageISO;
                    asset.Name = source.Url;
                    asset.Properties.Add(new Property(VODnLiveContentProperties.DeviceType, source.Device.ToString()));
                    asset.Properties.Add(new Property(VODnLiveContentProperties.HighDefinition, source.HighDefinition.ToString()));
                    content.Assets.Add(asset);
                }
            }

            // add default metadata
            //CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.Genre, ingestConfig);
            //CommonUtil.AddDefaultMetaDataValue(content, VODnLiveContentProperties.Category, ingestConfig);

            return content;
        }

        public Dictionary<MultipleContentService, List<MultipleServicePrice>> TranslateXmlToPrices(IngestConfig ingestConfig, List<MultipleContentService> connectedServices, XmlDocument priceXml, String name)
        {
            Dictionary<MultipleContentService, List<MultipleServicePrice>> prices = new Dictionary<MultipleContentService, List<MultipleServicePrice>>();
        
            List<String> categories = new List<String>();
            if (ingestConfig.MetaDataDefaultValues.ContainsKey(VODnLiveContentProperties.Category))
                categories.Add(ingestConfig.MetaDataDefaultValues[VODnLiveContentProperties.Category]);
       

            foreach (MultipleContentService connectedService in connectedServices)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = connectedService.ObjectID;

                List<MultipleServicePrice> servicePrices = new List<MultipleServicePrice>();
                List<MultipleServicePrice> matchedServicePrices = ingestConfig.FindPricesForService(service.ObjectID.Value, categories);
                // duplicate prices
                foreach (MultipleServicePrice servicePrice in matchedServicePrices)
                {
                    MultipleServicePrice newPrice = new MultipleServicePrice();
                    newPrice.ID = servicePrice.ID;
                    newPrice.Price = servicePrice.Price;
                    newPrice.Currency = servicePrice.Currency;
                    newPrice.ContentLicensePeriodLength = servicePrice.ContentLicensePeriodLength;
                    newPrice.ContentLicensePeriodLengthTime = servicePrice.ContentLicensePeriodLengthTime;
                    newPrice.Title = name;
                    servicePrices.Add(newPrice);
                }
                prices.Add(service, servicePrices);
            }

            return prices;
        }

        private LanguageInfo GetLanguageInfo(ContentData content, String languageISO)
        {
            var languageInfo = content.LanguageInfos.FirstOrDefault(l => l.ISO.Equals(languageISO));
            if (languageInfo == null) {
                languageInfo = new LanguageInfo();
                languageInfo.ISO = languageISO;
                content.LanguageInfos.Add(languageInfo);
            }

            return languageInfo;
        }

        private void LoadDefaultAssets(ChannelConfiguration serviceChannelConfig, List<AssetSource> assetsFromDefault)
        {
            foreach (AssetSource asset in assetsFromDefault)
            {
                // one exception of condition to copy
                // if NPVR is false on servcie settings, and stream type is dvb, then we don't need to copy any ip type for that device type.
                if (!serviceChannelConfig.NPVR.Value && asset.Type == StreamType.IP)
                {
                    Int32 typeCount = serviceChannelConfig.Assets.Count(a => a.Device.ToString().Equals(asset.Device.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                                                             a.Type == StreamType.DVB);
                    if (typeCount != 0)
                        continue;   // this we have a DVB stream for this device type, and NPVR is false, we don't need to copy the IP stream for this devcie type.
                }

                // check if same type of asset is defiend in the servcie settings.
                Int32 assetCount = serviceChannelConfig.Assets.Count(a => a.Device.ToString().Equals(asset.Device.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                                                          a.Type == asset.Type);
                if (assetCount == 0)
                {
                    // copy from default asset for this device and stream type.
                    AssetSource copyAsset = new AssetSource();
                    copyAsset.Device = asset.Device;
                    copyAsset.Url = asset.Url;
                    copyAsset.Type = asset.Type;
                    copyAsset.HighDefinition = asset.HighDefinition;
                    serviceChannelConfig.Assets.Add(copyAsset);
                }
            }
        }

        private ChannelConfiguration loadServiceConfig(XmlNode configurationNode)
        {
            ChannelConfiguration channelconfig = null;
            if (configurationNode != null)
            {
                channelconfig = new ChannelConfiguration();

                XmlNode lcnNode = configurationNode.SelectSingleNode("Lcn");
                if (lcnNode != null)
                    channelconfig.Lcn = lcnNode.InnerText;

                XmlNode titleNode = configurationNode.SelectSingleNode("Title");
                if (titleNode != null)
                    channelconfig.Title = titleNode.InnerText;

                XmlNode UUIDNode = configurationNode.SelectSingleNode("UUID");
                if (UUIDNode != null)
                    channelconfig.Uuid = UUIDNode.InnerText; 

                XmlNodeList sourceNodes = configurationNode.SelectNodes("Assets/Asset");
                foreach (XmlNode assetNode in sourceNodes)
                {
                    AssetSource asset = new AssetSource();
                    DeviceType device;
                    if (Enum.TryParse(assetNode.Attributes["device"].Value, true, out device))
                    {
                        asset.Device = device;
                    }
                    else
                    {
                        throw new Exception("Wrong deviceType defined, could not parse value " + assetNode.Attributes["device"].Value);
                    }
                    asset.Url = assetNode.InnerText;
                    asset.Type = CommonUtil.GetStreamType(asset.Url);
                    asset.HighDefinition = Boolean.Parse(assetNode.Attributes["highDefinition"].Value);

                    channelconfig.Assets.Add(asset);
                }


                XmlNode NPVRNode = configurationNode.SelectSingleNode("NPVR");
                if (NPVRNode != null)
                {
                    channelconfig.NPVR = Boolean.Parse(NPVRNode.Attributes["enable"].Value);
                    XmlNodeList assetNodes = NPVRNode.SelectNodes("Asset");
                    foreach (XmlNode assetNode in assetNodes)
                    {
                        AssetSource asset = new AssetSource();
                        DeviceType device;
                        if (Enum.TryParse(assetNode.Attributes["device"].Value, true, out device))
                        {
                            asset.Device = device;
                        }
                        else
                        {
                            throw new Exception("Wrong deviceType defined, could not parse value " + assetNode.Attributes["device"].Value);
                        }
                        asset.Url = assetNode.InnerText;
                        asset.Type = CommonUtil.GetStreamType(asset.Url);
                        asset.HighDefinition = Boolean.Parse(assetNode.Attributes["highDefinition"].Value);

                        channelconfig.Assets.Add(asset);
                    }
                }

                XmlNode CatchupNode = configurationNode.SelectSingleNode("Catchup");
                if (CatchupNode != null)
                    channelconfig.Catchup = Boolean.Parse(CatchupNode.Attributes["enable"].Value);

                XmlNode BoxCoverNode = configurationNode.SelectSingleNode("BoxCover");
                if (BoxCoverNode != null)
                    channelconfig.BoxCover = BoxCoverNode.InnerText;
            }

            return channelconfig;
        }
    }

    class ChannelConfiguration {

        public ChannelConfiguration() {
            Assets = new List<AssetSource>();
        }

        public String ServiceViewLanugageISO { get; set; }
        public UInt64 ServiceObjectId { get; set; }
        public String Lcn {get;set;}
        public String Title {get;set;}
        public String Uuid { get; set; }
        public String BoxCover { get; set; }
        public Boolean? Catchup { get; set; }
        public Boolean? NPVR { get; set; }

        public List<AssetSource> Assets { get; set; }
    }

    class AssetSource {
        public DeviceType Device { get; set; }
        public String Url { get; set; }
        public StreamType Type { get; set; }
        public Boolean HighDefinition { get; set; }
    }
}
