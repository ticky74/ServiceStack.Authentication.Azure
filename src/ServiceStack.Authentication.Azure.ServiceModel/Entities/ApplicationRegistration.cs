namespace ServiceStack.Authentication.Azure.ServiceModel.Entities
{
    public class ApplicationRegistration : Model.IHasLongId
    {
        #region Properties and Indexers

        [DataAnnotations.Required]
        [DataAnnotations.StringLength(38)]
        public string ClientId { get; set; }

        [DataAnnotations.Required]
        [DataAnnotations.StringLength(64)]
        public string ClientSecret { get; set; }

        [DataAnnotations.Required]
        public string DirectoryName { get; set; }

        public long? RefId { get; set; }

        [DataAnnotations.StringLength(128)]
        public string RefIdStr { get; set; }

        public ulong RowVersion { get; set; }

        #endregion

        #region IHasLongId Members

        [DataAnnotations.AutoIncrement]
        public long Id { get; set; }

        #endregion
    }
}