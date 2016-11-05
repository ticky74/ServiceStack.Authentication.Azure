using ServiceStack.Authentication.Azure.ServiceModel.Entities;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class AuthTokenRequest
    {
        #region Properties and Indexers

        public string CallbackUrl { get; set; }
        public ApplicationRegistration Registration { get; set; }
        public string RequestCode { get; set; }
        public string[] Scopes { get; set; }

        #endregion
    }
}