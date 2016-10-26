using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Authentication.Azure.ServiceModel.Entities
{
    public class Me
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Language { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
