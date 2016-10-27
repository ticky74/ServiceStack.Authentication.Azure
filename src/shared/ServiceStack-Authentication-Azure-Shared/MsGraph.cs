using System;
using System.Collections.Specialized;

namespace ServiceStack.Authentication.Azure
{
    internal class MsGraph
    {
        public const string ProviderName = "ms-graph";

        public const string GraphUrl = "https://graph.microsoft.com";

        public const string AuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public const string TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        public const string Realm = "https://login.microsoftonline.com/";

        public const string MeUrl = "https://graph.microsoft.com/v1.0/me";

        public class ActiveDirectory
        {
            public const string MemberGroupsUrl = "https://graph.microsoft.com/v1.0/me/getMemberGroups";
        }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        public static bool RespondedWithError(NameValueCollection info)
        {
            return !(info["error"] ?? info["error_uri"] ?? info["error_description"]).IsNullOrEmpty();
        }
    }

    public class AzureServiceException : Exception
    {
        public NameValueCollection ErrorData { get; }

        public AzureServiceException(string attemptedUrl, NameValueCollection errorData)
            : base($"Azure graph request failed: {attemptedUrl}")
        {
            ErrorData = errorData;
        }
    }
}