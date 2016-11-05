namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    [Route("/ms-graph/dom-check")]
    public class RegisteredDomainCheck : IReturn<RegisteredDomainCheckResponse>
    {
        #region Properties and Indexers

        public string Username { get; set; }

        #endregion
    }

    public class RegisteredDomainCheckResponse
    {
        #region Properties and Indexers

        public bool IsRegistered { get; set; }

        #endregion
    }
}