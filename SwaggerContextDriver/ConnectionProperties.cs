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

        public GeneratorType GenOption
        {
            get
            {
                var gt = _driverData.Element("GeneratorType");
                var t = (GeneratorType)Enum.Parse(typeof(GeneratorType), gt != null ? gt.Value : GeneratorType.SingleClientFromOperatinoId.ToString());
                return t;
            }
            set { _driverData.SetElementValue("GeneratorType", value.ToString()); }
        }

        public AuthenticationType AuthOption
        {
            get
            {
                var gt = _driverData.Element("AuthenticationType");
                var t = (AuthenticationType)Enum.Parse(typeof(AuthenticationType), gt != null ? gt.Value : AuthenticationType.None.ToString());
                return t;
            }
            set { _driverData.SetElementValue("AuthenticationType", value.ToString()); }
        }        

        public bool InjectHttpClient
        {
            get
            {
                var el = _driverData.Element("InjectHttpClient");
                return bool.Parse( el == null ? "true" : el.Value);
            }
            set { _driverData.SetElementValue("InjectHttpClient", value); }
        }

        public bool DisposeHttpClient
        {
            get
            {
                var el = _driverData.Element("DisposeHttpClient");
                return bool.Parse(el == null ? "false" : el.Value);
            }
            set { _driverData.SetElementValue("DisposeHttpClient", value); }
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
