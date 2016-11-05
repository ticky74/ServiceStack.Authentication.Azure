using ServiceStack.Authentication.Azure.ServiceModel.Entities;

namespace ServiceStack.Authentication.Azure.ServiceModel
{
    public interface IApplicationRegistryService
    {
        #region  Abstract

        bool ApplicationIsRegistered(string directoryName);
        ApplicationRegistration GetApplicationByDirectoryName(string domain);
        ApplicationRegistration GetApplicationById(string tenantId);

        void InitSchema();
        ApplicationRegistration RegisterApplication(ApplicationRegistration registration);

        ApplicationRegistration RegisterApplication(string applicationid, string publicKey, string directoryName,
            long? refId, string refIdStr);

        #endregion
    }
}