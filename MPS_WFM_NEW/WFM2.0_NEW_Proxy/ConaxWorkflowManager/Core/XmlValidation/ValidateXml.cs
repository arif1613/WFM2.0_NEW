using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.XmlValidation
{
    public class ValidateXml
    {
        private static ValidationEventArgs _errorMsg;
        private static string _xsdPath;
        private static FileInfo _fileInfo;

        public ValidateXml(FileInfo fi, string xsdPath)
        {
            _fileInfo = fi;
            _xsdPath = xsdPath;
            _errorMsg = null;
        }
        private void ValidationCallBack(object sender, ValidationEventArgs e)
        {
            if (e != null)
            {
                _errorMsg = e;
            }
        }
        public ValidationEventArgs Validate()
        {
            var schemafile = new XmlDocument();
            schemafile.Load(_xsdPath);
            var xmld = new XmlDocument();
            xmld.Load(_fileInfo.FullName);
            xmld.Schemas.Add(null, schemafile.BaseURI);
            xmld.Validate(ValidationCallBack);
            return _errorMsg;
        }

        public ValidationEventArgs getValidationError()
        {
            return _errorMsg;
        }
}
}
