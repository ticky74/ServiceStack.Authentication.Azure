using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Authentication.Azure.Entities;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class AuthTokenRequest
    {
        public ApplicationRegistration Registration { get; set; }
        public string CallbackUrl { get; set; }
        public string RequestCode { get; set; }
        public string[] Scopes { get; set; }
    }
}
