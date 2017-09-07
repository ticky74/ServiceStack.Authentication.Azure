using System;

namespace ServiceStack.Authentication.Azure.ServiceModel.Entities
{
    public class Me
    {
        #region Properties and Indexers

        public System.Guid ID { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName { get; set; }
        public string Language { get; set; }
        public string LastName { get; set; }
        public string MobileNumber { get; set; }
        public string JobTitle { get; set; }

        public string UserPrincipalName { get; set; }
        public string OfficeLocation { get; set; }
        public string[] BusinessPhones { get; set; }

        #endregion
    }
}