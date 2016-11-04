using System;
using ServiceStack.Authentication.Azure.Entities;
using ServiceStack.Configuration;

namespace ServiceStack.Authentication.Azure
{
    public class SingleTenantApplicationRegistryService : IApplicationRegistryService
    {
        private readonly ApplicationRegistration _registration;

        public SingleTenantApplicationRegistryService(AzureDirectorySettings settings)
        {
            _registration = new ApplicationRegistration
            {
                ClientId = settings.ClientId,
                DirectoryName = settings.DirectoryName,
                ClientSecret = settings.ClientSecret
            };
        }

        public bool ApplicationIsRegistered(string directoryName)
        {
            return string.Compare(_registration.DirectoryName, directoryName, StringComparison.Ordinal) == 0;
        }

        public ApplicationRegistration GetApplicationByDirectoryName(string domain)
        {
            // Actually disregards the domain parameter. All values are specified
            // statically in the configuration
            return _registration;
        }

        public ApplicationRegistration GetApplicationById(string tenantId)
        {
            // Actually disregards the domain parameter. All values are specified
            // statically in the configuration
            return _registration;
        }

        public ApplicationRegistration RegisterApplication(ApplicationRegistration registration)
        {
            throw new NotImplementedException("Cannot override configured application registration");
        }

        public ApplicationRegistration RegisterApplication(string applicationid, string publicKey, string directoryName,
            long? refId,
            string refIdStr)
        {
            throw new NotImplementedException("Cannot override configured application registration");
        }

        public void InitSchema()
        {
            // Noop
        }

        internal static class ConfigSettings
        {
            public const string ClientId = "oauth.{0}.clientId";
            public const string ClientSecret = "oauth.{0}.clientSecret";
            public const string DirectoryName = "oauth.{0}.directoryName";

            private static string GetConfigKey(string keyFormat)
            {
                return keyFormat.Fmt(MsGraph.ProviderName);
            }

            public static string GetClientIdKey() => GetConfigKey(ClientId);
            public static string GetClientSecretKey() => GetConfigKey(ClientSecret);
            public static string GetDirectoryNameKey() => GetConfigKey(DirectoryName);
        }
    }
}