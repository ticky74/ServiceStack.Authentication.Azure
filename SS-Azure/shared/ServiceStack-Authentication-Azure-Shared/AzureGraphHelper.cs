using System.Collections.Specialized;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Text;

namespace ServiceStack.Authentication.Azure
{
    public static class GraphHelper
    {
        public static object GetMemberGroups(string bearerToken)
        {
            var groups =
                "https://graph.microsoft.com/v1.0/me/getMemberGroups".PostJsonToUrl("{securityEnabledOnly:false}",
                    requestFilter: req =>
                    {
                        req.AddBearerToken(bearerToken);
                        req.ContentType = "application/json";
                        req.Accept = "application/json";
                    });

            return groups;
        }

        public static Me Me(string accessToken)
        {
            var azureReq = "https://graph.microsoft.com/v1.0/me".GetStringFromUrl(
                requestFilter: req => { req.AddBearerToken(accessToken); });

            var meInfo = JsonObject.Parse(azureReq);
            var meInfoNvc = meInfo.ToNameValueCollection();
            var me = new Me
            {
                Email = meInfoNvc["mail"],
                FirstName = meInfoNvc["givenName"],
                LastName = meInfoNvc["surname"],
                Language = meInfoNvc["preferredLanguage"],
                PhoneNumber = meInfoNvc["mobilePhone"]
            };

            return me;
        }
    }
}