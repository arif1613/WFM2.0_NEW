using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using log4net;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File.Handler;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.File;
using System.IO;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Ingest.XML;
using System.Xml.Schema;
using System.Xml;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.ValueObjects.Ingest;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Enums;
using System.Globalization;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Data;
using MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.WFMConfig.SystemConfiguration;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Task
{
    public class FileWatchTask : BaseTask
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static XmlDocument uploadFolderConfigDoc = new XmlDocument();

        public FileWatchTask()
        {
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String fileIngestUploadDirectoryConfig = systemConfig.FileIngestUploadDirectoryConfig;
            
            uploadFolderConfigDoc.Load(fileIngestUploadDirectoryConfig);
        }

        public override void DoExecute()
        {
            log.Debug("DoExecute Start");
            // find complete CRUD XML
            IList<IngestItem> ingestItems = FindCompleteCRUDXML();

            // CRUD via IS
            if (ingestItems.Count > 0)
            {
                log.Info("found " + ingestItems.Count + " ingestItems to CRUD via IS.");
            }
            foreach (IngestItem ingestItem in ingestItems)
            {

                // move file to work folder before adds to mpp
                if (MoveToWorkFolder(ingestItem))
                {
                    try
                    {
                        MPPIngestFlow.Process(ingestItem);
                    }
                    catch (Exception ex)
                    {
                        // failed to import content. reject import
                        try
                        {
                            MoveToRejectFolder(ingestItem);
                        }
                        catch (Exception moveEx)
                        {
                            log.Warn("Failed to Move files to reject folder", moveEx);
                        }
                        try
                        {
                            CommonUtil.SendFailedVODIngestNotification(Path.GetFileName(ingestItem.OriginalIngestXMLPath), ex);
                        }
                        catch (Exception mailex)
                        {
                            log.Warn("Failed to send Notification.", mailex);
                        }
                    }
                }
            }

            log.Debug("DoExecute End");
        }

        private Boolean MoveToWorkFolder(IngestItem ingestItem)
        {
            FileInfo fi = new FileInfo(ingestItem.OriginalIngestXMLPath);
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String uploadDir = Path.GetDirectoryName(ingestItem.OriginalIngestXMLPath);
            String workDir = Path.Combine(systemConfig.GetConfigParam("FileIngestWorkDirectory"), fi.Directory.Parent.Name, fi.Directory.Name);

            if (!CommonUtil.ContentIsChannel(ingestItem.contentData))
            {
                var channelFileIngestHelper = new CableLabsFileIngestHelper();
                return channelFileIngestHelper.MoveIngestFiles(ingestItem.contentData, uploadDir, workDir);
            }
            else
            {
                var channelFileIngestHelper = new ChannelFileIngestHelper();
                return channelFileIngestHelper.MoveIngestFiles(fi.Name, uploadDir, workDir); 
            }
        }

        private Boolean MoveToRejectFolderFromUpload(IngestConfig ingestConfig, String ingestFile)
        {
            FileInfo fi = new FileInfo(ingestFile);
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String uploadDir = Path.GetDirectoryName(ingestFile);
            String rejectDir = systemConfig.FileIngestRejectDirectory;
            rejectDir = Path.Combine(rejectDir, fi.Directory.Parent.Name, fi.Directory.Name);

            IngestXMLType ingestXmlType = IngestXMLType.CableLabs_1_0;
            try
            {
                ingestXmlType = CommonUtil.GetIngestXMLType(ingestFile);
            }
            catch (Exception exc)
            {
                log.Warn("Can't find ingestXmlType, removing xml", exc);
                CableLabsFileIngestHelper fileIngestHelper = new CableLabsFileIngestHelper();
                return fileIngestHelper.MoveIngestFiles(Path.GetFileName(ingestFile), uploadDir, rejectDir);
            }
            var ingestXMLConfig =
                    Config.GetConfig()
                        .IngestXMLConfigs.SingleOrDefault(
                            i => i.IngestXMLType.Equals(ingestXmlType.ToString("G"), StringComparison.OrdinalIgnoreCase));
                BaseIngestFileIngestHelper FileIngestHelper =
                    Activator.CreateInstance(System.Type.GetType(ingestXMLConfig.FileIngestHelper)) as
                        BaseIngestFileIngestHelper;
                return FileIngestHelper.MoveIngestFiles(Path.GetFileName(ingestFile), uploadDir, rejectDir);
            
        
        }

        private Boolean MoveToRejectFolder(IngestItem ingestItem)
        {
            FileInfo fi = new FileInfo(ingestItem.OriginalIngestXMLPath);
            var systemConfig = Config.GetConfig().SystemConfigs.Where(c => c.SystemName == "ConaxWorkflowManager").SingleOrDefault();
            String workDir = systemConfig.GetConfigParam("FileIngestWorkDirectory");
            workDir = Path.Combine(workDir, ingestItem.contentData.ContentRightsOwner.Name.ToLower(), fi.Directory.Name);
            String rejectDir = systemConfig.GetConfigParam("FileIngestRejectDirectory");
            rejectDir = Path.Combine(rejectDir, ingestItem.contentData.ContentRightsOwner.Name.ToLower(), fi.Directory.Name);

            if (!CommonUtil.ContentIsChannel(ingestItem.contentData))
            {
                var cableLabsFileIngestHelper = new CableLabsFileIngestHelper();
                return cableLabsFileIngestHelper.MoveIngestFiles(ingestItem.contentData, workDir, rejectDir);
            }
            else
            {
                var channelFileIngestHelper = new ChannelFileIngestHelper();
                return channelFileIngestHelper.MoveIngestFiles(fi.Name, workDir, rejectDir);
            }
        }

        private IList<IngestItem> FindCompleteCRUDXML()
        {
            IList<IngestItem> ingestItems = new List<IngestItem>();
            List<String> uploadFolderPaths = GetUploadFolderPaths();

            log.Debug("Find Complete CRUD XML");
            var systemConfig = (ConaxWorkflowManagerConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.ConaxWorkflowManager).SingleOrDefault();
            String folderSettingsFileName = systemConfig.FolderSettingsFileName;
            IFileHandler fileHandler = systemConfig.FileIngestHandlerType;

            foreach (var uploadFolderPath in uploadFolderPaths)
            {
                IngestConfig ingestConfig = new IngestConfig();

                FileInformation[] fileInformations = fileHandler.ListDirectory(uploadFolderPath, false);
                var fileInfo = fileInformations.SingleOrDefault(f => Path.GetFileName(f.Path).Equals(folderSettingsFileName, StringComparison.OrdinalIgnoreCase));
                if (fileInfo == null)
                {
                    log.Error("Upload fodler " + uploadFolderPath + " missing config file " + folderSettingsFileName);
                    continue;
                }

                // find CRUD XML
                var XMLs = fileInformations.Where(f => f.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                List<FileInformation> crudXMLs = new List<FileInformation>();
                crudXMLs.AddRange(XMLs);

                if (crudXMLs.Count > 0)
                { // load folder settigns
                    log.Info("Found " + crudXMLs.Count + " xml files in " + uploadFolderPath +
                             " to process. Start locating complete CRUD XML.");
                    try
                    {
                        ingestConfig = GetIngestConfig(fileInfo);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to load folder settings from " + fileInfo.Path + " skip this folder for now.", ex);
                        continue;
                    }
                }

                foreach (FileInformation fileInformation in crudXMLs)
                {
                    BaseStorageIngestHandler storageIngestHandler = null;
                    try
                    {
                        String guid = Guid.NewGuid().ToString();
                        ThreadContext.Properties["IngestIdentifier"] = guid;
                        log.Debug("Setting " + guid + " as ingestIdentifier on ingest");
                        // identify xml type
                        IngestXMLType ingestXmlType = CommonUtil.GetIngestXMLType(fileInformation.Path);
                        log.Debug("ingest xml " + fileInformation.Path + " is type " + ingestXmlType.ToString("G"));

                        // load ingest xml handler
                        var ingestXMLConfig = Config.GetConfig().IngestXMLConfigs.SingleOrDefault(i => i.IngestXMLType.Equals(ingestXmlType.ToString("G"), StringComparison.OrdinalIgnoreCase));
                        if (ingestXMLConfig == null)
                        {
                            throw new Exception("ingestXmlType " + ingestXmlType.ToString("G") + " is not defiend in the configuraiton xml.");
                        }
                        log.Debug("verify " + fileInformation.Path);
                        //validate XML



                        try
                        {
                            storageIngestHandler = Activator.CreateInstance(System.Type.GetType(ingestXMLConfig.IngestHandler), new object[] { fileHandler }) as BaseStorageIngestHandler;
                            storageIngestHandler.ValidateXML(fileInformation, ingestXMLConfig.XSD);
                        }
                        catch (XmlSchemaValidationException xmlEx)
                        {
                            string errorMessage = "XML " + fileInformation.Path + " is not Valid: " + xmlEx.Message + " check XSD " + ingestXMLConfig.XSD;
                            log.Error(errorMessage);
                            try
                            {
                                MoveToRejectFolderFromUpload(ingestConfig, fileInformation.Path);
                            }
                            catch (Exception moveEx)
                            {
                                log.Warn("Failed to move files to reject folder.", moveEx);
                            }
                            try
                            {
                                CommonUtil.SendFailedVODIngestNotification(Path.GetFileName(fileInformation.Path), errorMessage);
                            }
                            catch (Exception mailEx)
                            {
                                log.Warn("Failed to send Notification.", mailEx);
                            }
                            continue;
                        }
                        catch (XmlException xmlEx)
                        {
                            string errorMessage = "XML " + fileInformation.Path + " is not Valid: " + xmlEx.Message;
                            log.Error(errorMessage, xmlEx);
                            try
                            {
                                MoveToRejectFolderFromUpload(ingestConfig, fileInformation.Path);
                            }
                            catch (Exception moveEx)
                            {
                                log.Warn("Failed to move files to reject folder.", moveEx);
                            }
                            try
                            {
                                CommonUtil.SendFailedVODIngestNotification(Path.GetFileName(fileInformation.Path), errorMessage);
                            }
                            catch (Exception mailEx)
                            {
                                log.Warn("Failed to send Notification.", mailEx);
                            }
                            continue;
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Failed to validate XML " + fileInformation.Path, ex);
                        }

                        // init ingestitem
                        IngestItem ingestItem = storageIngestHandler.InitIngestItem(ingestConfig, fileInformation, guid);

                        // Validate business Domain
                        IngestModelValidationResult imvr = ConaxBusinessDomain.IsValidCableLabsIngestModel(ingestConfig, ingestItem);
                        if (!imvr.IsValid)
                        {
                            log.Warn(imvr.Message);
                            log.Error("XML " + fileInformation.Path + " failed at business domain validation.");
                            try
                            {
                                MoveToRejectFolderFromUpload(ingestConfig, fileInformation.Path);
                            }
                            catch (Exception moveEx)
                            {
                                log.Warn("Failed to move files to reject folder.", moveEx);
                            }
                            try
                            {
                                CommonUtil.SendFailedVODIngestNotification(Path.GetFileName(fileInformation.Path), imvr.Message);
                            }
                            catch (Exception mailEx)
                            {
                                log.Warn("Failed to send Notification.", mailEx);
                            }
                            continue;
                        }

                        // get completeCRUDXML
                        ingestItem = storageIngestHandler.GetCompleteCRUDIngest(ingestConfig, ingestItem);

                        // found complete inget item.
                        if (ingestItem != null)
                            ingestItems.Add(ingestItem);
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Failed to process " + fileInformation.Path, ex);
                        try
                        {
                            MoveToRejectFolderFromUpload(ingestConfig, fileInformation.Path);
                        }
                        catch (Exception moveEx)
                        {
                            log.Warn("Failed to move files to reject folder.", moveEx);
                        }
                        try
                        {
                            CommonUtil.SendFailedVODIngestNotification(Path.GetFileName(fileInformation.Path), ex);
                        }
                        catch (Exception mailEx)
                        {
                            log.Warn("Failed to send Notification.", mailEx);
                        }
                    }
                    ThreadContext.Properties.Remove("IngestIdentifier");
                }
            }

            return ingestItems;
        }

        private List<string> GetUploadFolderPaths()
        {
            var folderPaths = new List<string>();
            var folderPathsToIgnore = new List<string>();

            XmlNodeList croNodes = uploadFolderConfigDoc.SelectNodes("UploadFolderConfig/ContentRightsOwner");

            foreach (XmlNode croNode in croNodes)
            {
                XmlNodeList uploadFolderNodes = croNode.SelectNodes("UploadFolders/UploadFolder");

                if (uploadFolderNodes != null)
                    log.Debug("CRO " + ((XmlElement) croNode).GetAttribute("name") + " have " + uploadFolderNodes.Count +
                              " upload folders.");

                foreach (XmlNode uploadFolderNode in uploadFolderNodes)
                {

                    String folderPath = uploadFolderNode.InnerText;
                    if (folderPaths.Contains(folderPath))
                    {
                        log.Warn("The upload folder path '" + folderPath +
                                 "' occurs more than once in the FileIngestUploadDirectoryConfig. This upload folder will be ignored.");
                        if(!folderPathsToIgnore.Contains(folderPath))
                            folderPathsToIgnore.Add(folderPath);
                    }
                    else
                        folderPaths.Add(folderPath);
                }
            }

            foreach (var folderPathToIgnore in folderPathsToIgnore)
            {
                folderPaths.RemoveAll(x => x == folderPathToIgnore);
            }

            return folderPaths;
        }

        private void CheckForDuplicateUploadFolderPaths(List<String> uploadFolderPaths)
        {
            
        }

        private IngestConfig GetIngestConfig(FileInformation folderSettings)
        {
            var mppConfig = (MPPConfig)Config.GetConfig().SystemConfigs.Where(c => c.SystemName == SystemConfigNames.MPP).SingleOrDefault();
            IngestConfig ingestConfig = new IngestConfig();

            ingestConfig.HostID = mppConfig.HostID;
            //ingestConfig.CAS = mppConfig.DefaultCAS;

            XmlDocument folderConfigDoc = new XmlDocument();
            folderConfigDoc.Load(folderSettings.Path);

            XmlElement ingestSettingsNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/IngestSettings");
            ingestConfig.EnableQA = Boolean.Parse(ingestSettingsNode.GetAttribute("enableQA"));

            XmlElement URIProfileNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/URIProfile");
            ingestConfig.URIProfile = URIProfileNode.InnerText;

            XmlElement PricingRuleNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/PricingRule");
            if (PricingRuleNode != null && !String.IsNullOrWhiteSpace(PricingRuleNode.InnerText))
                ingestConfig.ADIPricingRule = (ADIPricingRuleType)Enum.Parse(typeof(ADIPricingRuleType), PricingRuleNode.InnerText, true);

            XmlNodeList ingestXMLTypeNodes = folderConfigDoc.SelectNodes("FolderConfiguration/IngestXMLTypes/IngestXMLType");
            foreach (XmlNode ingestXMLTypeNode in ingestXMLTypeNodes)
                ingestConfig.IngestXMLTypes.Add(ingestXMLTypeNode.InnerText);

            XmlNode cronode = folderConfigDoc.SelectSingleNode("FolderConfiguration/ContentRightsOwner");
            ingestConfig.ContentRightsOwner = cronode.InnerText;

            XmlNode caNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/ContentAgreement");
            ingestConfig.ContentAgreement = caNode.InnerText;

            XmlNode diNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultImageFileName");
            if (diNode != null)
                ingestConfig.DefaultImageFileName = diNode.InnerText;

            ingestConfig.DefaultImageClientGUIName = mppConfig.DefaultImageClientGUIName;
            ingestConfig.DefaultImageClassification = mppConfig.DefaultImageClassification;

            XmlNodeList eviceNodes = folderConfigDoc.SelectNodes("FolderConfiguration/Devices/Device");
            foreach (XmlNode eviceNode in eviceNodes)
                ingestConfig.Devices.Add(eviceNode.InnerText);

            XmlNode metadataMappingConfigurationFileNameNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataMappingConfigurationFileName");
            ingestConfig.MetadataMappingConfigurationFileName = metadataMappingConfigurationFileNameNode.InnerText;

            XmlNode defaultRatingTypeNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultRatingType");
            ingestConfig.DefaultRatingType = defaultRatingTypeNode != null
                                                 ? defaultRatingTypeNode.InnerText
                                                 : VODnLiveContentProperties.MovieRating;

             //add metadata default values.

            XmlNode movieRatingNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/" + VODnLiveContentProperties.MovieRating);
            if (movieRatingNode != null && !String.IsNullOrEmpty(movieRatingNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.MovieRating, movieRatingNode.InnerText);

            XmlNode tvRatingNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/" + VODnLiveContentProperties.TVRating);
            if (tvRatingNode != null && !String.IsNullOrEmpty(tvRatingNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.TVRating, tvRatingNode.InnerText);

            XmlNode genreNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/Genre");
            if (genreNode != null && !String.IsNullOrEmpty(genreNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.Genre, genreNode.InnerText);

            XmlNode categoryNode = folderConfigDoc.SelectSingleNode("FolderConfiguration/MetadataDefaultValues/Category");
            if (categoryNode != null && !String.IsNullOrEmpty(categoryNode.InnerText))
                ingestConfig.MetaDataDefaultValues.Add(VODnLiveContentProperties.Category, categoryNode.InnerText);

            // add default prices
            XmlElement defaultRentalPriceNode = (XmlElement)folderConfigDoc.SelectSingleNode("FolderConfiguration/DefaultConfigurationForAllServices/Prices/RentalPrice");
            if (defaultRentalPriceNode != null)
            {
                MultipleServicePrice defaultPrice = new MultipleServicePrice();
                defaultPrice.Price = DataParseHelper.ParsePrice((defaultRentalPriceNode.GetAttribute("amount")));
                defaultPrice.Currency = defaultRentalPriceNode.GetAttribute("currency");
                defaultPrice.ContentLicensePeriodLength = Int64.Parse(defaultRentalPriceNode.GetAttribute("periodLengthInhrs"));
                defaultPrice.IsRecurringPurchase = false;
                ingestConfig.DefaultServicePrices.Add("*", defaultPrice);
            }
            XmlNodeList CategoryNodes = folderConfigDoc.SelectNodes("FolderConfiguration/DefaultConfigurationForAllServices/Prices/Categories/Category");
            foreach (XmlElement CategoryNode in CategoryNodes)
            {
                XmlElement rentalPriceNode = (XmlElement)CategoryNode.SelectSingleNode("RentalPrice");
                if (rentalPriceNode != null)
                {
                    MultipleServicePrice defaultPrice = new MultipleServicePrice();
                    defaultPrice.Price = DataParseHelper.ParsePrice(rentalPriceNode.GetAttribute("amount"));
                    defaultPrice.Currency = rentalPriceNode.GetAttribute("currency");
                    defaultPrice.ContentLicensePeriodLength = Int64.Parse(rentalPriceNode.GetAttribute("periodLengthInhrs"));
                    defaultPrice.IsRecurringPurchase = false;
                    ingestConfig.DefaultServicePrices.Add(CategoryNode.GetAttribute("name"), defaultPrice);
                }
            }
            // add service prices
            XmlNodeList serviceNodes = folderConfigDoc.SelectNodes("FolderConfiguration/ConfigurationForServices/Service");
            foreach (XmlElement serviceNode in serviceNodes)
            {
                MultipleContentService service = new MultipleContentService();
                service.ObjectID = UInt64.Parse(serviceNode.GetAttribute("objectId"));
                List<MultipleServicePrice> prices = new List<MultipleServicePrice>();

                XmlNodeList subscriptionPriceNodes = serviceNode.SelectNodes("Prices/SubscriptionPrice");
                foreach (XmlElement subscriptionPriceNode in subscriptionPriceNodes)
                {
                    MultipleServicePrice subPrice = new MultipleServicePrice();
                    subPrice.ID = UInt64.Parse(subscriptionPriceNode.GetAttribute("id"));
                    subPrice.IsRecurringPurchase = true;
                    prices.Add(subPrice);
                }
                XmlElement rentalPriceNode = (XmlElement)serviceNode.SelectSingleNode("Prices/RentalPrice");
                if (rentalPriceNode != null)
                {
                    MultipleServicePrice rentPrice = new MultipleServicePrice();
                    rentPrice.Price = DataParseHelper.ParsePrice(rentalPriceNode.GetAttribute("amount"));
                    rentPrice.Currency = rentalPriceNode.GetAttribute("currency");
                    rentPrice.ContentLicensePeriodLength = Int64.Parse(rentalPriceNode.GetAttribute("periodLengthInhrs"));
                    rentPrice.IsRecurringPurchase = false;
                    prices.Add(rentPrice);
                }
                if (prices.Count > 0) { // if any price to add.
                    PriceMatchParameter pmp = new PriceMatchParameter();
                    pmp.Service = service;
                    pmp.Category = "*";
                    ingestConfig.ServicePrices.Add(pmp, prices);
                }

                // load category prices
                XmlNodeList serviceCategoryNodes = serviceNode.SelectNodes("Prices/Categories/Category");
                foreach(XmlElement serviceCategoryNode in serviceCategoryNodes) {
                    prices = new List<MultipleServicePrice>();
                    XmlNodeList categorySubscriptionPriceNodes = serviceCategoryNode.SelectNodes("SubscriptionPrice");
                    foreach(XmlElement categorySubscriptionPriceNode in categorySubscriptionPriceNodes) {
                        MultipleServicePrice subPrice = new MultipleServicePrice();
                        subPrice.ID = UInt64.Parse(categorySubscriptionPriceNode.GetAttribute("id"));
                        subPrice.IsRecurringPurchase = true;
                        prices.Add(subPrice);
                    }
                    XmlElement categoryRentalPriceNode = (XmlElement)serviceCategoryNode.SelectSingleNode("RentalPrice");
                    if (categoryRentalPriceNode != null) {
                        MultipleServicePrice categoryRentPrice = new MultipleServicePrice();
                        categoryRentPrice.Price = DataParseHelper.ParsePrice(categoryRentalPriceNode.GetAttribute("amount"));
                        categoryRentPrice.Currency = categoryRentalPriceNode.GetAttribute("currency");
                        categoryRentPrice.ContentLicensePeriodLength = Int64.Parse(categoryRentalPriceNode.GetAttribute("periodLengthInhrs"));
                        categoryRentPrice.IsRecurringPurchase = false;
                        prices.Add(categoryRentPrice);
                    }
                    PriceMatchParameter categorypmp = new PriceMatchParameter();
                    categorypmp.Service = service;
                    categorypmp.Category = serviceCategoryNode.GetAttribute("name");
                    ingestConfig.ServicePrices.Add(categorypmp, prices);
                }
            }

            return ingestConfig;
        }
    }
}
