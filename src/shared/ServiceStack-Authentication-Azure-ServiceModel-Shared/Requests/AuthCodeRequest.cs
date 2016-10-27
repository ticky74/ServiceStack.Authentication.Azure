using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Authentication.Azure.Entities;

namespace ServiceStack.Authentication.Azure.ServiceModel.Requests
{
    public class AuthCodeRequest
    {
        public string CallbackUrl { get; set; }
        public ApplicationRegistration Registration { get; set; }
        public string[] Scopes { get; set; }
        public string UserName { get; set; }
    }

    public class AuthCodeRequestData
    {
        public string State { get; set; }

        public string AuthCodeRequestUrl { get; set; }
    }
}
