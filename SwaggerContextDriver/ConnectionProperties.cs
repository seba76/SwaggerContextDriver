using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SwaggerContextDriver
{
    class ConnectionProperties
    {
        readonly IConnectionInfo _cxInfo;
        readonly XElement _driverData;

        public ConnectionProperties(IConnectionInfo cxInfo)
        {
            _cxInfo = cxInfo;
            _driverData = cxInfo.DriverData;            
        }

        public bool Persist
        {
            get { return _cxInfo.Persist; }
            set { _cxInfo.Persist = value; }
        }

        public string Uri
        {
            get { return (string)_driverData.Element("Uri") ?? ""; }
            set { _driverData.SetElementValue("Uri", value); }
        }

        public string Domain
        {
            get { return (string)_driverData.Element("Domain") ?? ""; }
            set { _driverData.SetElementValue("Domain", value); }
        }

        public string UserName
        {
            get { return (string)_driverData.Element("UserName") ?? ""; }
            set { _driverData.SetElementValue("UserName", value); }
        }

        public string Password
        {
            get { return _cxInfo.Decrypt((string)_driverData.Element("Password") ?? ""); }
            set { _driverData.SetElementValue("Password", _cxInfo.Encrypt(value)); }
        }

        public ICredentials GetCredentials()
        {
            if (!string.IsNullOrEmpty(Domain))
                return new NetworkCredential(UserName, Password, Domain);

            if (!string.IsNullOrEmpty(UserName))
                return new NetworkCredential(UserName, Password);

            return CredentialCache.DefaultNetworkCredentials;
        }
    }

}
