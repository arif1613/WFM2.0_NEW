using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using System.Xml.Linq;
using System.Xml.XPath;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;
using log4net;
using System.Reflection;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Catchup;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Database;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Communication;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Encoder.Unified;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using System.Diagnostics;
using System.Collections;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Catchup
{
    public class CodeShopHLSCatchupHandler : BaseEncoderCatchupHandler
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //protected List<EPGChannel> channels = null;

        
        
        public override String CreateAssetName(ContentData content, UInt64 serviceObjId, DeviceType deviceType, EPGChannel channel)
        {
            String AssetName = "";
           
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();

            if (channel == null)
                channel = CatchupHelper.GetEPGChannel(content);

            String liveStreamUrl = channel.ServiceEPGConfigs[serviceObjId].SourceConfigs.First(s => s.Device == deviceType).Stream;

            Int32 startTimePendingSec = Int32.Parse(systemConfig.GetConfigParam("EPGStartTimePendingSec"));
            Int32 endTimePendingSec = Int32.Parse(systemConfig.GetConfigParam("EPGEndTimePendingSec"));

            TimeSpan vbegin = UnifiedHelper.GetServerTimeStamp(content.EventPeriodFrom.Value.AddSeconds(-1 * startTimePendingSec));
            TimeSpan vend = UnifiedHelper.GetServerTimeStamp(content.EventPeriodTo.Value.AddSeconds(endTimePendingSec));
            
            AssetName = liveStreamUrl + "?vbegin=" + ((UInt64)vbegin.TotalSeconds).ToString() + "&vend=" + ((UInt64)vend.TotalSeconds).ToString();
            return AssetName;
        }

        public override String GetAssetUrl(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, NPVRRecording recording, EPGChannel epgChannel)
        {
           throw new NotImplementedException();
        }

        #region Generate
        public override void GenerateManifest(List<String> channelsToProces)
        {
            throw new NotImplementedException();
        }

        private void GenerateCatchupManifest(List<String> channelsToProces) 
        {
            throw new NotImplementedException();
        }

        public override void GenerateNPVR(ContentData content, UInt64 serviceObjId, String serviceViewLanugageISO, DeviceType deviceType, DateTime startTime, DateTime endTime)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region LoadToDB
        public override void ProcessArchive(List<String> channelsToProces)
        {
            // do nothing
            // handle by codeshop server


        }
        #endregion

        #region Delete
        public override void DeleteCatchupSegments(EPGChannel epgChannel)
        {
            // This is handled by the CodeShopSmoothCatchupHandler as hls catchup will be served from smooth archive.
        }

        public override void DeleteNPVR(ContentData content, Asset assetToDelete)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
