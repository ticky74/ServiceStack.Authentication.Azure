using System;
using ServiceStack.Authentication.Azure.Entities;

namespace ServiceStack.Authentication.Azure
{
    public class CodeApplicationRegistryService : IApplicationRegistryService
    {
        private readonly ApplicationRegistration _registration;

        public CodeApplicationRegistryService(string applicationId, string publicKey, string directoryName)
        {
            _registration = new ApplicationRegistration
            {
                ClientId = applicationId,
                DirectoryName = directoryName,
                ClientSecret = publicKey
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
    }
}