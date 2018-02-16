using System;
using System.Collections.Specialized;

namespace ServiceStack.Authentication.Azure
{
    internal class MsGraph
    {
        #region Public/Internal

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        public static bool RespondedWithError(NameValueCollection info)
        {
            return !(info["error"] ?? info["error_uri"] ?? info["error_description"]).IsNullOrEmpty();
        }

        #endregion

        #region Inner Types

        #region ActiveDirectory

        public class ActiveDirectory
        {
            #region Constants and Variables

            public const string MemberGroupsUrl = "https://graph.microsoft.com/v1.0/me/getMemberGroups";

            #endregion
        }

        #endregion

        #endregion

        #region Constants and Variables

        public const string ProviderName = "ms-graph";

        public const string GraphUrl = "https://graph.microsoft.com";

        public const string DefaultAuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public const string DefaultTokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        public const string Realm = "https://login.microsoftonline.com/";

        public const string MeUrl = "https://graph.microsoft.com/v1.0/me";

        #endregion
    }

    public class AzureServiceException : Exception
    {
        #region Constructors

        public AzureServiceException(string attemptedUrl, NameValueCollection errorData)
            : base($"Azure graph request failed: {attemptedUrl}")
        {
            ErrorData = errorData;
        }

        #endregion

        #region Properties and Indexers

        public NameValueCollection ErrorData { get; }

        #endregion
    }
}