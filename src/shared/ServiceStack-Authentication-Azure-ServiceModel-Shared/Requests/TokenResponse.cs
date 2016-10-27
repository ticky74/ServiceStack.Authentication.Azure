using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public NameValueCollection AuthData { get; set; }
    }
}
