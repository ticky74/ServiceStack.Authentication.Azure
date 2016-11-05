using System;
using ServiceStack.Authentication.Azure;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using Xunit;

namespace ServiceStack.Authentication.Azure20.Tests
{
    public class SingleTenantRegistryServiceTest
    {
        #region Constants and Variables

        private readonly AzureDirectorySettings _directorySettings;
        private const string AppId = "e7319d8d-bf2b-4aca-a794-8a9f949ef80b";
        private const string PublicKey = "8ccdacd6a78a4b3891c3097c16fe29bb";
        private const string DirectoryName = "foo.onmicrosoft.com";

        #endregion

        #region Constructors

        public SingleTenantRegistryServiceTest()
        {
            _directorySettings = new AzureDirectorySettings
            {
                ClientSecret = PublicKey,
                ClientId = AppId,
                DirectoryName = DirectoryName
            };
        }

        #endregion

        #region Public/Internal

        [Fact]
        public void ShouldReturnRegistrationWithMatchingDirectoryName()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var reg = service.GetApplicationByDirectoryName(DirectoryName);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnConfiguredRegistrationWhenDirectoryNameDoesNotMatch()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var reg = service.GetApplicationByDirectoryName("zzz" + DirectoryName);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnRegistrationWithMatchingApplicationId()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var reg = service.GetApplicationById(AppId);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnConfiguredRegistrationWhenApplicationIdDoesNotMatch()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var reg = service.GetApplicationByDirectoryName(Guid.NewGuid().ToString());

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldIdentifyRegisteredApplication()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var isRegistered = service.ApplicationIsRegistered(DirectoryName);
            Assert.True(isRegistered);
        }

        [Fact]
        public void ShouldNotIdentifyUnRegisteredApplication()
        {
            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            var isRegistered = service.ApplicationIsRegistered("zzz" + DirectoryName);
            Assert.False(isRegistered);
        }

        [Fact]
        public void ShouldNotRegisterNewApplicationAtRuntime()
        {
            var registration = new ApplicationRegistration
            {
                ClientId = Guid.NewGuid().ToString(),
                DirectoryName = Guid.NewGuid().ToString("N"),
                ClientSecret = Guid.NewGuid().ToString("N")
            };

            var service = new SingleTenantApplicationRegistryService(_directorySettings);
            Action tryRegister = () => service.RegisterApplication(registration);

            Assert.Throws<NotImplementedException>(tryRegister);
        }

        #endregion
    }
}