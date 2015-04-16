using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup
{

    [Serializable()]
    public class EPGChannel
    {
        public EPGChannel() {
            ServiceExtContentIDs = new Dictionary<UInt64, UInt64>();
            Properties = new List<Property>();
            ServiceEPGConfigs = new Dictionary<UInt64, ServiceEPGConfig>();
            PublishInfos = new List<PublishInfo>();
        }

        public String Name { get; set; }
        public UInt64 MppContentId { get; set; }                      // MPP content id
        public String EPGId { get; set; }                   // epg feed channel id
        public String CubiChannelId { get; set; }           // cubi channel id
        public Dictionary<UInt64, UInt64> ServiceExtContentIDs { get; set; } // Cubi content object id
        public UInt64 ConaxContegoContentID { get; set; }   // contento content object id
        public String ContentRightOwner { get; set; }
        public String ContentAgreement { get; set; }
        public List<PublishInfo> PublishInfos { get; set; }

        private String _NameInAlphanumeric;
        public String NameInAlphanumeric
        {
            get {
                if (String.IsNullOrWhiteSpace(_NameInAlphanumeric))
                    _NameInAlphanumeric = Regex.Replace(Name, @"[^A-Za-z0-9]+", "");

                return _NameInAlphanumeric;
            }
        }

        public String GetPublishingHash()
        {
            String publishingHash = "";
            var publishToProperties = Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.PublishedToService)).OrderBy(p=>p.Value);
            foreach (Property publishProperty in publishToProperties)
            {
                publishingHash += publishProperty.Value;
            }
            return publishingHash;
        }

        public Boolean EnableCatchUpInAnyService { 
            get {
                return (this.ServiceEPGConfigs.Where(kvp => kvp.Value.EnableCatchup == true).Count() != 0);
            }
        }
        public Boolean EnableNPVRInAnyService
        {
            get
            {
                return (this.ServiceEPGConfigs.Where(kvp => kvp.Value.EnableNPVR == true).Count() != 0);
            }
        }

        public Boolean PublishedInAnyChannel
        {
            get
            {
                return this.PublishInfos.Where(p => p.PublishState == PublishState.Published).Any();
            }
        }

        //public Boolean EnableCatchUp { get; set; }
        //public Boolean EnableNPVR { get; set; }

        public DateTime DeleteTimeSSBuffer { get; set; }        // buffer time to delete to.
        public DateTime DeleteTimeHLSBuffer { get; set; }        // buffer time to delete to.

        public Dictionary<UInt64, ServiceEPGConfig> ServiceEPGConfigs { get; set; } // service configurations

        public String CatchUpDBSource { get; set; }

        public List<Property> Properties { get; set; }
        public List<ulong?> ObjectIdsOfServicesPublishedTo
        {
            get
            {
                var ret = new List<ulong?>();

                foreach (var p in this.Properties.Where(p => p.Type.Equals(VODnLiveContentProperties.PublishedToService)))
                    ret.Add(Convert.ToUInt64(p.Value));

                return ret;
            }
        }
    }

    [Serializable()]
    public class ServiceEPGConfig {

        public ServiceEPGConfig() {
            SourceConfigs = new List<SourceConfig>();
        }

        public Boolean EnableCatchup { get; set; }
        public Boolean EnableNPVR { get; set; }
        public UInt64 ServiceObjectId { get; set; }
        public String ServiceViewLanugageISO { get; set; }
        public List<SourceConfig> SourceConfigs { get; set; }
        
        
    }

    [Serializable()]
    public class SourceConfig {

        public SourceConfig() {
            Device = DeviceType.NotSpecified;
            EncodeInTimezone = "UTC";
        }

        public String EncodeInTimezone { get; set; }
        public DeviceType Device { get; set; }
        public String Stream { get; set; }
        public String CatchUpFSRoot { get; set; }
        public String CatchUpWebRoot { get; set; }
        public String CompositeFSRoot { get; set; }
        public String CompositeWebRoot { get; set; }
        public String NPVRFSRoot { get; set; }
        /// <summary>
        /// Web root to the NNPVR assets.
        /// </summary>
        public String NPVRWebRoot { get; set; }
    }
}
