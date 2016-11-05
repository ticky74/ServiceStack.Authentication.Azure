using ServiceStack.Authentication.Azure.ServiceModel.Entities;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class AuthCodeRequest
    {
        #region Properties and Indexers

        public string CallbackUrl { get; set; }
        public ApplicationRegistration Registration { get; set; }
        public string[] Scopes { get; set; }
        public string UserName { get; set; }

        #endregion
    }

    public class AuthCodeRequestData
    {
        #region Properties and Indexers

        public string AuthCodeRequestUrl { get; set; }
        public string State { get; set; }

        #endregion
    }
}