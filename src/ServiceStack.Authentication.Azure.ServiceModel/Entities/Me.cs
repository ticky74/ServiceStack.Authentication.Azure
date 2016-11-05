namespace ServiceStack.Authentication.Azure.ServiceModel.Entities
{
    public class Me
    {
        #region Properties and Indexers

        public string Email { get; set; }
        public string FirstName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Language { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        #endregion
    }
}