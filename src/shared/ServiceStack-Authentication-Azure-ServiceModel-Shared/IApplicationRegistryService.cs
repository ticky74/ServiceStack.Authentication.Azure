using System;
using ServiceStack.Authentication.Azure.Entities;

namespace ServiceStack.Authentication.Azure
{
    public interface IApplicationRegistryService
    {
        bool ApplicationIsRegistered(string directoryName);
        ApplicationRegistration GetApplicationByDirectoryName(string domain);
        ApplicationRegistration GetApplicationById(string tenantId);
        ApplicationRegistration RegisterApplication(ApplicationRegistration registration);

        ApplicationRegistration RegisterApplication(string applicationid, string publicKey, string directoryName,
            long? refId, string refIdStr);

        void InitSchema(); 
    }
}
