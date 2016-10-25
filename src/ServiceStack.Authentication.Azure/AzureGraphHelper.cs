namespace ServiceStack.Authentication.Azure {

    public static class GraphHelper 
    {
        public static object GetMemberGroups(string bearerToken)
        {
            var groups = "https://graph.microsoft.com/v1.0/me/getMemberGroups".PostJsonToUrl("{securityEnabledOnly:false}",                
                requestFilter: req =>
                {
                    req.AddBearerToken(bearerToken);
                    req.ContentType = "application/json";
                    req.Accept = "application/json";
                });

            return groups;
        }
    }
}