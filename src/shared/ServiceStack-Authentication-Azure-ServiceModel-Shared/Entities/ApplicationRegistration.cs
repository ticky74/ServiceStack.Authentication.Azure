
namespace ServiceStack.Authentication.Azure.Entities {

    public class ApplicationRegistration : ServiceStack.Model.IHasLongId
    {
        [ServiceStack.DataAnnotations.AutoIncrement]
        public long Id { get; set; }

        [ServiceStack.DataAnnotations.Required]
        [ServiceStack.DataAnnotations.StringLength(38)]
        public string ClientId { get; set; }

        [ServiceStack.DataAnnotations.Required]
        [ServiceStack.DataAnnotations.StringLength(64)]
        public string ClientSecret { get; set; }

        [ServiceStack.DataAnnotations.Required]
        public string DirectoryName { get; set; }
        public ulong RowVersion { get; set; }

        public long? RefId { get; set; }

        [ServiceStack.DataAnnotations.StringLength(128)]
        public string RefIdStr { get; set; }
    }

}