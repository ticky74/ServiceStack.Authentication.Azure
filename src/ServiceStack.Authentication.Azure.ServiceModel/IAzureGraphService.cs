using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;

namespace ServiceStack.Authentication.Azure.ServiceModel
{
    public interface IAzureGraphService
    {
        #region  Abstract

        string[] GetMemberGroups(string authToken);
        Me Me(string authToken);
        AuthCodeRequestData RequestAuthCode(AuthCodeRequest codeRequest);
        TokenResponse RequestAuthToken(AuthTokenRequest tokenRequest);

        #endregion
    }
}