using System;
using LINQPad.Extensibility.DataContext;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;

namespace SwaggerContextDriver
{
    public class DynamicDriver : DynamicDataContextDriver
    {
        /// <summary>User-friendly name for your driver.</summary>
        public override string Name { get { return "Swagger Driver"; } }

        /// <summary>Your name.</summary>
        public override string Author { get { return "seba"; } }

        /// <summary>Returns the text to display in the root Schema Explorer node for a given connection info.</summary>
        public override string GetConnectionDescription(IConnectionInfo cxInfo)
        {
            // The URI of the service best describes the connection:
            return new ConnectionProperties(cxInfo).Uri;
        }

        /// <summary>
        /// Builds an assembly containing a typed data context, and returns data for the Schema Explorer.
        /// </summary>
        /// <param name="cxInfo">Connection information, as entered by the user</param>
        /// <param name="assemblyToBuild">Name and location of the target assembly to build</param>
        /// <param name="nameSpace">The suggested namespace of the typed data context. You must update this
        /// parameter if you don't use the suggested namespace.</param>
        /// <param name="typeName">The suggested type name of the typed data context. You must update this
        /// parameter if you don't use the suggested type name.</param>
        /// <returns>Schema which will be subsequently loaded into the Schema Explorer.</returns>
        public override List<ExplorerItem> GetSchemaAndBuildAssembly(IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
        {
            return SchemaBuilder.GetSchemaAndBuildAssembly(
                GetDriverFolder(),
                new ConnectionProperties(cxInfo),
                assemblyToBuild,
                ref nameSpace,
                ref typeName);
        }

        /// <summary>This virtual method is called after a data context object has been instantiated, in
        /// preparation for a query. You can use this hook to perform additional initialization work.</summary>
        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
        {
            var props = new ConnectionProperties(cxInfo);

            if (props.InjectHttpClient)
            {
                dynamic anonContext = context;
                anonContext.driverConnectionInfo = cxInfo;
            }
        }


        /// <summary>Displays a dialog prompting the user for connection details. The isNewConnection
        /// parameter will be true if the user is creating a new connection rather than editing an
        /// existing connection. This should return true if the user clicked OK. If it returns false,
        /// any changes to the IConnectionInfo object will be rolled back.</summary>
        public override bool ShowConnectionDialog(IConnectionInfo cxInfo, bool isNewConnection)
        {
            // Populate the default URI with a demo value:
            if (isNewConnection) new ConnectionProperties(cxInfo)
            {
                Uri = "http://petstore.swagger.io/v2/swagger.json",
                Persist = true
            };

            bool? result = new ConnectionDialog(cxInfo).ShowDialog();
            return result == true;
        }

        /// <summary>Returns the names & types of the parameter(s) that should be passed into your data
        /// context's constructor. Typically this is a connection string or a DbConnection. The number
        /// of parameters and their types need not be fixed - they may depend on custom flags in the
        /// connection's DriverData. The default is no parameters.</summary>
        public virtual ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
        {
            return new ParameterDescriptor[] { new ParameterDescriptor("httpClient", "System.Net.Http.HttpClient") };
        }

        /// <summary>Returns the argument values to pass into your data context's constructor, based on
        /// a given IConnectionInfo. This must be consistent with GetContextConstructorParameters.</summary>
        public virtual object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
        {
            var httpClientHandler = new HttpClientHandler();
            var props = new ConnectionProperties(cxInfo);

            if (props.AuthOption == AuthenticationType.None)
                httpClientHandler.Credentials = null;
            else if (props.AuthOption == AuthenticationType.Basic)
                httpClientHandler.Credentials = new NetworkCredential(props.UserName, props.Password, props.Domain);
            else if (props.AuthOption == AuthenticationType.CurrentUser)
                httpClientHandler.Credentials = CredentialCache.DefaultNetworkCredentials;
            else
                throw new NotSupportedException("Authentication method specified in the Connection Dialog is not supported.");

            var httpClient = new HttpClient(httpClientHandler);
            return new[] { httpClient };
        }

    }
}
