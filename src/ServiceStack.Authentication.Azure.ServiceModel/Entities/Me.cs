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
        [Obsolete("Use MobileNumber instead. PhoneNumber is depricated and will be removed.")]
        public string PhoneNumber { get; set; }
        public string MobileNumber
        {
#pragma warning disable 618
            get => PhoneNumber;
#pragma warning restore 618
#pragma warning disable 618
            set => PhoneNumber = value;
#pragma warning restore 618
        }
		public string JobTitle { get; set; }

		public string UserPrincipalName { get; set; }
		public string OfficeLocation { get; set; }
		public string[] BusinessPhones { get; set; }

		#endregion
	}
}