namespace ServiceStack.Authentication.Azure
{
    internal class MsGraph
    {
        public const string ProviderName = "ms-graph";

        public const string GraphUrl = "https://graph.microsoft.com";

        public const string AuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        public const string TokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        public const string Realm = "https://login.microsoftonline.com/";
    }
}