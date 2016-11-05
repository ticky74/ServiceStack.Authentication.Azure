using System.Collections.Specialized;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class TokenResponse
    {
        #region Properties and Indexers

        public string AccessToken { get; set; }
        public NameValueCollection AuthData { get; set; }
        public string RefreshToken { get; set; }

        #endregion
    }
}