using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using WFMProxy.Controllers;

namespace WFMProxy
{
    public class validateXml
    {
        private static string cmdMessage;
        private static string _ingestXmlPath;
        private static string _xsdPath;
        private static DirectoryInfo _directoryToWatch;
        public validateXml(string ingestXmlPath, string xsdPath)
        {
            _ingestXmlPath = ingestXmlPath;
            _xsdPath = xsdPath;
            Validate();
        }

        private void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Error)
            {
                cmdMessage = "Xml Validation Failed";
            }
            else
            {
                cmdMessage = "Xml Invalid";
            }

        }

        public string Validate()
        {
            try
            {
                var schemafile = new XmlDocument();
                schemafile.Load(_xsdPath);
                var xmld = new XmlDocument();
                xmld.Load(_ingestXmlPath);
                xmld.Schemas.Add(null, schemafile.BaseURI);
                xmld.Validate(ValidationCallBack);
                var rm = new ReadMediaInfo(_ingestXmlPath, _directoryToWatch);
                bool checkIfFilesExists = rm.WatchIfMediaFilesExists();
                cmdMessage = checkIfFilesExists ? "Valid Ingest Files Found" : "All Media Files Are Not Present";
            }
            catch (Exception e)
            {
                cmdMessage = e.Message;
            }
            return cmdMessage;
        }
    }
}
