using System.Net;
using ServiceStack.Authentication.Azure;
using ServiceStack.Authentication.Azure.Entities;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Authentication.Azure.Requests;
using ServiceStack.OrmLite;
using Xunit;

namespace ServiceStack.Authentication.Azure20.Tests
{
    public class GraphAuthServiceTest
    {
        #region Constants and Variables

        private readonly IApplicationRegistryService _registrationService;

        #endregion

        #region Constructors

        public GraphAuthServiceTest()
        {
            var connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            _registrationService = new OrmLiteMultiTenantApplicationRegistryService(connectionFactory);

            _registrationService.InitSchema();
            _registrationService.RegisterApplication(new ApplicationRegistration
            {
                ClientId = "clientid",
                ClientSecret = "clientsecret",
                DirectoryName = "foodomain.com"
            });
        }

        #endregion

        #region Public/Internal

        [Fact]
        public void ShouldFindRegisteredDomain()
        {
            var check = new RegisteredDomainCheck
            {
                Username = "someuser@foodomain.com"
            };
            var service = new GraphAuthService(_registrationService);

            var result = service.Post(check);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<RegisteredDomainCheckResponse>(result);
            Assert.True(((RegisteredDomainCheckResponse) result).IsRegistered);
        }

        [Fact]
        public void ShouldNotFindUnRegisteredDomain()
        {
            var check = new RegisteredDomainCheck
            {
                Username = "someuser@foodomain2.com"
            };
            var service = new GraphAuthService(_registrationService);

            var result = service.Post(check);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<RegisteredDomainCheckResponse>(result);
            Assert.False(((RegisteredDomainCheckResponse) result).IsRegistered);
        }

        [Theory]
        [InlineData("")]
        [InlineData("someuser")]
        [InlineData("someuser@")]
        [InlineData("someuser@foodomain")]
        [InlineData("someuserfoodomain.com")]
        public void ShouldNotFindRegisteredDomainForInvalidEmail(string value)
        {
            var check = new RegisteredDomainCheck
            {
                Username = value
            };
            var service = new GraphAuthService(_registrationService);

            var result = Assert.Throws<HttpError>(() => service.Post(check));
            Assert.NotNull(result);
            Assert.IsAssignableFrom<HttpError>(result);
            Assert.Equal(result.StatusCode, HttpStatusCode.BadRequest);
        }

        #endregion
    }
}