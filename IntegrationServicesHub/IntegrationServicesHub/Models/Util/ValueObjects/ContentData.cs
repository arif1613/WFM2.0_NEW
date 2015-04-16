using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;

namespace IntegrationServicesHub.Models.Util.ValueObjects
{
    public class ContentData
    {
        public ContentData() {
            PublishInfos = new List<PublishInfo>();
            Properties = new List<Property>();
            LanguageInfos = new List<LanguageInfo>();
            Assets = new List<Asset>();
            ContentAgreements = new List<ContentAgreement>();
        }

        [Required]
        public String Name { get; set; }
        [Required]
        [Key]
        public UInt64 ID { get; set; }
        [Required]
        public UInt64 ObjectID { get; set; }
        [Required]
        public String HostID { get; set; }
        [Required]
        public String ExternalID { get; set; }

        public DateTime? EventPeriodFrom { get; set; }
        public DateTime? EventPeriodTo { get; set; }
        public DateTime? Created { get; set; }
        public List<PublishInfo> PublishInfos { get; set; }
        public UInt32? ProductionYear { get; set; }
        public TimeSpan? RunningTime { get; set; }
        public Boolean? TemporaryUnavailable { get; set; }
        public List<Property> Properties { get; set; }
        public List<LanguageInfo> LanguageInfos { get; set; }
        public List<Asset> Assets { get; set; }
        public ContentRightsOwner ContentRightsOwner { get; set; }
        public List<ContentAgreement> ContentAgreements { get; set; }
    }
}
