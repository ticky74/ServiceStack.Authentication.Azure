namespace ServiceStack.Authentication.Azure.Tests {

    using System;
    using ServiceStack.Configuration;
    using ServiceStack.Authentication.Azure;
    using ServiceStack.Authentication.Azure.Entities;
    using Xunit;

    public class AppSettingsRegistryServiceTest {
        private const string AppId = "e7319d8d-bf2b-4aca-a794-8a9f949ef80b";
        private const string PublicKey = "8ccdacd6a78a4b3891c3097c16fe29bb";
        private const string DirectoryName = "foo.onmicrosoft.com";
        private IAppSettings _appSettings = null;

        public AppSettingsRegistryServiceTest()
        {
            _appSettings = new AppSettings();
            _appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetClientIdKey(), AppId);
            _appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetClientSecretKey(), PublicKey);
            _appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetDirectoryNameKey(), DirectoryName);
        }

        [Fact]
        public void ShouldReturnRegistrationWithMatchingDirectoryName()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var reg = _service.GetApplicationByDirectoryName(DirectoryName);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnConfiguredRegistrationWhenDirectoryNameDoesNotMatch()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var reg = _service.GetApplicationByDirectoryName("zzz" + DirectoryName);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnRegistrationWithMatchingApplicationId()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var reg = _service.GetApplicationById(AppId);

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldReturnConfiguredRegistrationWhenApplicationIdDoesNotMatch()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var reg = _service.GetApplicationByDirectoryName(Guid.NewGuid().ToString());

            Assert.NotNull(reg);
            Assert.Equal(reg.ClientId, AppId);
            Assert.Equal(reg.ClientSecret, PublicKey);
            Assert.Equal(reg.DirectoryName, DirectoryName);
        }

        [Fact]
        public void ShouldIdentifyRegisteredApplication()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var isRegistered = _service.ApplicationIsRegistered(DirectoryName);
            Assert.True(isRegistered);
        }

        [Fact]
        public void ShouldNotIdentifyUnRegisteredApplication()
        {
            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            var isRegistered = _service.ApplicationIsRegistered("zzz" + DirectoryName);
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

            var _service = new AppSettingsApplicationRegistryService(_appSettings);
            Action tryRegister = () => _service.RegisterApplication(registration);

            Assert.Throws<NotImplementedException>(tryRegister);

        }        
    }
}