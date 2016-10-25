using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServiceStack;
using ServiceStack.Authentication.Azure;
using ServiceStack.Authentication.Azure.Entities;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Authentication.Azure.Requests;
using ServiceStack.OrmLite;
using Xunit;

namespace ServiceStack_Authentication_Azure_Tests_Shared
{
    public class GraphAuthServiceTest
    {
        private readonly IApplicationRegistryService _registrationService;

        public GraphAuthServiceTest()
        {
            var connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
            _registrationService = new OrmLiteApplicationRegistryService(connectionFactory);

            _registrationService.InitSchema();
            _registrationService.RegisterApplication(new ApplicationRegistration
            {
                ClientId = "clientid",
                ClientSecret = "clientsecret",
                DirectoryName = "foodomain.com",
            });
        }

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
            Assert.False(((RegisteredDomainCheckResponse)result).IsRegistered);
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
            Assert.Equal(((HttpError)result).StatusCode, HttpStatusCode.BadRequest);
        }
    }
}
