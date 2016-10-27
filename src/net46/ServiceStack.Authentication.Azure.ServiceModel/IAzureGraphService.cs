using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;

namespace ServiceStack.Authentication.Azure.ServiceModel
{
    public interface IAzureGraphService
    {
        AuthCodeRequestData RequestAuthCode(AuthCodeRequest codeRequest);
        Me Me(string authToken);
        string[] GetMemberGroups(string authToken);
        TokenResponse RequestAuthToken(AuthTokenRequest tokenRequest);
    }
}