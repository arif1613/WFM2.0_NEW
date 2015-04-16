using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Collections;
using System.Diagnostics;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util
{
    public class CatchupHelper
    {

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static MPPIntegrationServicesWrapper mppWrapper = MPPIntegrationServiceManager.InstanceWithPassiveEvent;
        // locks
        private static System.Object _GetAllEPGChannel = new System.Object();

        private static XElement EPGChannelConfigXMLFile = null;

        private static Hashtable channelFromConfig = new Hashtable();

        public static EPGChannel GetEPGChannel(ContentData content)
        {
            String channelId = content.Properties.FirstOrDefault(p => p.Type.Equals(CatchupContentProperties.ChannelId, StringComparison.OrdinalIgnoreCase)).Value;
            EPGChannel channel = GetEPGChannel(UInt64.Parse(channelId), new MppCallTimes());

            return channel;
        }


        public static EPGChannel GetEPGChannel(UInt64 channelId, MppCallTimes times)
        {

            String key = "CatchupHelper.GetEPGChannel|" + channelId;
            EPGChannel channel = WFMCache.Get<EPGChannel>(key);
            if (channel != null)
                return channel;

            lock (_GetAllEPGChannel)
            {// cehck again after locks
                channel = WFMCache.Get<EPGChannel>(key);
                if (channel == null)
                {
                    channel = new EPGChannel();
                    try
                    {
                        var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
                        String EPGChannelConfigXMLUrl = systemConfig.EPGChannelConfigXML;
                        XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);

                        XElement channelNode = EPGChannelConfigXMLFile.XPathSelectElement("Channels/Channel[@mppContentId='" + channelId + "']");
                        if (channelNode == null)
                        {
                            return null;
                        }
                        channel.MppContentId = channelId;
                        if (channelNode.Attribute("epgChannelId") != null)
                            channel.EPGId = channelNode.Attribute("epgChannelId").Value;

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        ContentData content = mppWrapper.GetContentDataByID(channel.MppContentId);
                        stopwatch.Stop();
                        times.GetContentByIdTime.Add(stopwatch.ElapsedMilliseconds);

                        if (content == null)
                            return null;
                        channel.Name = content.Name;
                        channel.PublishInfos = content.PublishInfos;
                        List<MultipleContentService> allServices = new List<MultipleContentService>();

                        stopwatch.Reset();
                        stopwatch.Start();
                        List<ContentAgreement> contentAgreements = mppWrapper.GetAllServicesForContent(content);
                        stopwatch.Stop();
                        times.GetAllServicesForContentTime.Add(stopwatch.ElapsedMilliseconds);


                        // load servcies
                        foreach (ContentAgreement contentAgreement in contentAgreements)
                            allServices.AddRange(contentAgreement.IncludedServices);
                        // load match rules
                        foreach (MultipleContentService service in allServices)
                        {

                            stopwatch.Reset();
                            stopwatch.Start();
                            List<ServiceViewMatchRule> matchRules = mppWrapper.GetServiceViewMatchRules(service);
                            stopwatch.Stop();
                            times.GetServiceViewMatchRulesTime.Add(stopwatch.ElapsedMilliseconds);
                            
                            service.ServiceViewMatchRules = matchRules;

                            // load assets from MPP
                            var allAssets = content.Assets.Where(a => a.LanguageISO.Equals(service.ServiceViewMatchRules[0].ServiceViewLanugageISO, StringComparison.OrdinalIgnoreCase) &&
                                                                      CommonUtil.GetStreamType(a.Name) == StreamType.IP);

                            

                            ServiceEPGConfig serviceEPGConfig = new ServiceEPGConfig();
                            serviceEPGConfig.EnableCatchup = ConaxIntegrationHelper.GetEnableCatchUp(content, service.ObjectID.Value);
                            serviceEPGConfig.EnableNPVR = ConaxIntegrationHelper.GetEnableNPVR(content, service.ObjectID.Value);
                            serviceEPGConfig.ServiceObjectId = service.ObjectID.Value;
                            serviceEPGConfig.ServiceViewLanugageISO = service.ServiceViewMatchRules[0].ServiceViewLanugageISO;
                            foreach(Asset asset in  allAssets) {
                                SourceConfig sourceConfig = new SourceConfig();
                                var deviceTypeProperty = asset.Properties.FirstOrDefault(p => p.Type.Equals(VODnLiveContentProperties.DeviceType));
                                if (deviceTypeProperty == null)
                                    throw new Exception("DeviceTypeProperty was missing on one asset on channel with ID " + channelId);
                                
                                sourceConfig.Device = (DeviceType)Enum.Parse(typeof(DeviceType), deviceTypeProperty.Value, true);
                                if (asset.Name.IndexOf(".isml", 0, StringComparison.OrdinalIgnoreCase) > -1 ||
                                    asset.Name.IndexOf(".m3u8", 0, StringComparison.OrdinalIgnoreCase) > -1)
                                    sourceConfig.Stream = asset.Name;
                                else
                                    continue; // not handled.

                                serviceEPGConfig.SourceConfigs.Add(sourceConfig);
                            }                            
                            channel.ServiceEPGConfigs.Add(service.ObjectID.Value, serviceEPGConfig);
                        }
                        
                        // override Asset with epg config
                        foreach(KeyValuePair<UInt64, ServiceEPGConfig> kvp in channel.ServiceEPGConfigs) {
                            // override with default
                            ServiceEPGConfig serviceEPGConfig = kvp.Value;
                            foreach(XElement defaultConfNode in channelNode.XPathSelectElements("DefaultConfiguration/Source"))
                                CatchupHelper.LoadSourceConfig(serviceEPGConfig, defaultConfNode);
                            
                            // override with service specific
                            XElement serviceConfNode = channelNode.XPathSelectElement("ConfigurationForServices/Service[@serviceObjectId='" + kvp.Key + "']");
                            if (serviceConfNode != null) {
                                foreach (XElement sourceConfNode in serviceConfNode.XPathSelectElements("Source"))
                                    CatchupHelper.LoadSourceConfig(serviceEPGConfig, sourceConfNode);
                            }
                        }


                        var channelIdProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.CubiChannelId));
                        channel.CubiChannelId = channelIdProperty.Value;

                        var conaxContegoContentIDProperty = content.Properties.First(p => p.Type.Equals(CatchupContentProperties.ConaxContegoContentID));
                        channel.ConaxContegoContentID = UInt64.Parse(conaxContegoContentIDProperty.Value);

                        var serviceExtContentIDProperties = content.Properties.Where(p => p.Type.Equals(CatchupContentProperties.ServiceExtContentID));
                        foreach(Property serviceExtContentIDProperty in serviceExtContentIDProperties) {
                            String[] values = serviceExtContentIDProperty.Value.Split(':');
                            channel.ServiceExtContentIDs.Add(UInt64.Parse(values[0]), UInt64.Parse(values[1]));
                        }
                        
                        channel.ContentRightOwner = content.ContentRightsOwner.Name;
                        channel.ContentAgreement = content.ContentAgreements[0].Name;
                        channel.Properties = content.Properties;

                        


                        WFMCache.Add<EPGChannel>(key, channel);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error when fetching EPG channel " + channelId, ex);
                        throw;
                    }
                }
            }
            return channel;
        }

        private static void LoadSourceConfig(ServiceEPGConfig serviceEPGConfig, XElement sourceConfNode)
        {            
            String encodeInTimezone = "UTC";
            XAttribute encodeInTimezoneAttri = sourceConfNode.Attribute("encodeInTimezone");
            if (encodeInTimezoneAttri != null)
                encodeInTimezone = encodeInTimezoneAttri.Value;

            XElement StreamNode = sourceConfNode.XPathSelectElement("Stream");
            XElement catchUpFSRootNode = sourceConfNode.XPathSelectElement("CatchUpFSRoot");
            XElement catchUpWebRootNode = sourceConfNode.XPathSelectElement("CatchUpWebRoot");
            XElement compositeFSRootNode = sourceConfNode.XPathSelectElement("CompositeFSRoot");
            XElement compositeWebRootNode = sourceConfNode.XPathSelectElement("CompositeWebRoot");
            XElement NPVRFSRootNode = sourceConfNode.XPathSelectElement("NPVRFSRoot");
            XElement NPVRWebRootNode = sourceConfNode.XPathSelectElement("NPVRWebRoot");

            foreach (XElement deviceNode in sourceConfNode.XPathSelectElements("ForDevices/Device"))
            {
                DeviceType deviceType = (DeviceType)Enum.Parse(typeof(DeviceType), deviceNode.Value, true);
                var sourceConfig = serviceEPGConfig.SourceConfigs.FirstOrDefault(s => s.Device == deviceType);
                if (sourceConfig != null)
                {   // update
                    sourceConfig.EncodeInTimezone = encodeInTimezone;
                    if (StreamNode != null && !String.IsNullOrWhiteSpace(StreamNode.Value))
                        sourceConfig.Stream = StreamNode.Value;

                    if (catchUpFSRootNode != null && !String.IsNullOrWhiteSpace(catchUpFSRootNode.Value))
                        sourceConfig.CatchUpFSRoot = catchUpFSRootNode.Value;

                    if (catchUpWebRootNode != null && !String.IsNullOrWhiteSpace(catchUpWebRootNode.Value))
                        sourceConfig.CatchUpWebRoot = catchUpWebRootNode.Value;

                    if (compositeFSRootNode != null && !String.IsNullOrWhiteSpace(compositeFSRootNode.Value))
                        sourceConfig.CompositeFSRoot = compositeFSRootNode.Value;

                    if (compositeWebRootNode != null && !String.IsNullOrWhiteSpace(compositeWebRootNode.Value))
                        sourceConfig.CompositeWebRoot = compositeWebRootNode.Value;

                    if (NPVRFSRootNode != null && !String.IsNullOrWhiteSpace(NPVRFSRootNode.Value))
                        sourceConfig.NPVRFSRoot = NPVRFSRootNode.Value;

                    if (NPVRWebRootNode != null && !String.IsNullOrWhiteSpace(NPVRWebRootNode.Value))
                        sourceConfig.NPVRWebRoot = NPVRWebRootNode.Value;
                }
                else
                {
                    // add new? not sure if this goonna work,
                    // since u need channel stream for inflight node,
                    // is NPVR with no inflight mode an valid use case?
                }
            }
        } 

        public static void ClearChannelFromConfigCache()
        {
            lock (channelFromConfig.SyncRoot)
            {
                channelFromConfig.Clear();
            }
            EPGChannelConfigXMLFile = null;
        }

        public static List<String> GetAllContentRightsOwners()
        {
            List<String> ret = new List<string>();
            List<EPGChannel> channels = GetAllEPGChannels();

            foreach (EPGChannel channel in channels)
            {
                if (!ret.Contains(channel.ContentRightOwner))
                    ret.Add(channel.ContentRightOwner);
            }
            return ret;
        }

        public static List<EPGChannel> GetAllEPGChannels()
        {

            List<EPGChannel> channels = new List<EPGChannel>();
            try
            {
                var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
                String EPGChannelConfigXMLUrl = systemConfig.EPGChannelConfigXML;
                XElement EPGChannelConfigXMLFile = XElement.Load(EPGChannelConfigXMLUrl);
                var mppCallTimes = new MppCallTimes();
                foreach (XElement channelNode in EPGChannelConfigXMLFile.XPathSelectElements("//Channels/Channel"))
                {
                    UInt64 channelId = 0;
                    if (channelNode.Attribute("mppContentId") != null)
                    {
                        UInt64.TryParse(channelNode.Attribute("mppContentId").Value, out channelId);
                    }

                    if (channelId == 0)
                    {
                        log.Warn("Attribute mppContentId is missing or is empty in EPGChannelConfig.");
                        continue;
                    }
                    
                    try
                    {
                        EPGChannel channel = GetEPGChannel(channelId, mppCallTimes);
                        if (channel != null)
                            channels.Add(channel);
                    }
                    catch (Exception exc)
                    {
                        log.Warn("Error fetching channel with id " + channelId, exc);
                    }
                }
                var mppCallsTotalTime = mppCallTimes.GetAllServicesForContentTime.Sum()+
                                        mppCallTimes.GetContentByIdTime.Sum() +
                                        mppCallTimes.GetServiceViewMatchRulesTime.Sum();
                log.Debug("Calls to MPP when getting EPG Channels took " + mppCallsTotalTime + " ms. (GetContentDataByID: " + mppCallTimes.GetContentByIdTime.Sum() + " ms, GetAllServicesForContent: " + mppCallTimes.GetAllServicesForContentTime.Sum() + " ms, GetServiceViewMatchRules: " + mppCallTimes.GetServiceViewMatchRulesTime.Sum() + " ms)");
            }
            catch (Exception ex)
            {
                log.Error("Error when fetching EPG channels", ex);
                throw;
            }


            return channels;
        }
    }

    public class MppCallTimes
    {
        public MppCallTimes()
        {
            GetContentByIdTime = new List<long>();
            GetAllServicesForContentTime = new List<long>();
            GetServiceViewMatchRulesTime = new List<long>();
            GetOngoingEPGsTime = new List<long>();
        }

        public List<long> GetContentByIdTime { get; set; }
        public List<long> GetAllServicesForContentTime { get; set; }
        public List<long> GetServiceViewMatchRulesTime { get; set; }
        public List<long> GetOngoingEPGsTime { get; set; } 
    }
}

