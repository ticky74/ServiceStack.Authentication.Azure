using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ServiceStack.Authentication.Azure;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;

namespace ServiceStack_Authentication_Azure_Tests_Shared
{
    public class TestAzureGraphService : IAzureGraphService
    {
        public readonly NameValueCollection AuthInfo = new NameValueCollection
        {
            {"access_token", TokenHelper.AccessToken},
            {"id_token", TokenHelper.IdToken}
        };

        public AuthCodeRequestData RequestAuthCode(AuthCodeRequest codeRequest)
        {
            // RequestAuthCode
            return new AzureGraphService().RequestAuthCode(codeRequest);
        }

        public Me Me(string authToken)
        {
            return new Me
            {
                Email = "some.user@foodomain.com",
                FirstName = "Some",
                Language = "en",
                LastName = "User",
                PhoneNumber = "15555551212"
            };
        }

        public string[] GetMemberGroups(string authToken)
        {
            return new string[] { "2dc78bbb-96a9-4058-b05d-be6e2bcf0ace" };
        }

        public TokenResponse RequestAuthToken(AuthTokenRequest tokenRequest)
        {
            string idToken = TokenHelper.GetIdToken();

            var nvc = new NameValueCollection
            {
                {"access_token", TokenHelper.AccessToken},
                {"id_token", idToken}
            };

            TokenHelper.HydrateAuthInfo(idToken, nvc);
            return new TokenResponse
            {
                AccessToken = TokenHelper.AccessToken,
                RefreshToken = TokenHelper.RefreshToken,
                AuthData = nvc
            };
        }
    }
}
