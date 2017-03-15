using System;
using System.Net;
using System.Text.RegularExpressions;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;

namespace ServiceStack.Authentication.Azure
{
    public class GraphAuthService : Service
    {
        #region Constructors

        public GraphAuthService(IApplicationRegistryService registry)
        {
            _registry = registry;
        }

        #endregion

        #region Public/Internal

        public object Post(RegisteredDomainCheck request)
        {
            if (string.IsNullOrWhiteSpace(request?.Username))
                throw new HttpError(HttpStatusCode.BadRequest);

            if (!EmailRegex.IsMatch(request.Username))
                throw new HttpError(HttpStatusCode.BadRequest);

            var idx = request.Username.IndexOf("@", StringComparison.Ordinal);
            var isRegistered = _registry.ApplicationIsRegistered(request.Username.Substring(idx + 1));

            return new RegisteredDomainCheckResponse
            {
                IsRegistered = isRegistered
            };
        }

        #endregion

        #region Constants and Variables

        private readonly IApplicationRegistryService _registry;

        public const string DomainPattern =
            @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,6}$";

        public const string EmailPattern = @"\s*\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*\s*";

        private static readonly Regex EmailRegex = new Regex(EmailPattern, RegexOptions.Compiled);
        private static readonly Regex DomainRegex = new Regex(DomainPattern, RegexOptions.Compiled);

        #endregion
    }
}