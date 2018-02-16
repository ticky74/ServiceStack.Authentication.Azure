using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;
using ServiceStack.Configuration;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Authentication.Azure
{
    public class AzureAuthenticationProvider : OAuthProvider
    {
        #region Constants and Variables

        private readonly IAzureGraphService _graphService;
        private string _failureRedirectPath;

        #endregion

        #region Constructors

        public AzureAuthenticationProvider(string overrideAuthUrl = null, string overrideTokenUrl = null)
            : this(new AzureGraphService(overrideAuthUrl, overrideTokenUrl))
        {
        }

        public AzureAuthenticationProvider(IAzureGraphService graphService)
            : this(new AppSettings(), graphService)
        {
        }

        public AzureAuthenticationProvider(IAppSettings settings, IAzureGraphService graphService)
            : base(settings, MsGraph.Realm, MsGraph.ProviderName, "ClientId", "ClientSecret")
        {
            Scopes = new[] { "https://graph.microsoft.com/User.Read", "offline_access", "openid", "profile" };
            _graphService = graphService ?? new AzureGraphService();
            AppSettings = settings;
            if (ServiceStackHost.Instance != null)
                RegisterProviderService(ServiceStackHost.Instance);
        }

        #endregion

        #region Properties and Indexers

        public IAppSettings AppSettings { get; private set; }

        public string FailureRedirectPath
        {
            get { return _failureRedirectPath; }
            set
            {
                if (!value.StartsWith("/"))
                    throw new FormatException("FailureRedirectPath should start with '/'");
                _failureRedirectPath = value;
            }
        }

        public TimeSpan RefreshTokenLifespan { get; set; } = TimeSpan.FromDays(13.9);

        // TODO: Handle dynamic scopes
        // http://graph.microsoft.io/en-us/docs/authorization/permission_scopes
        public string[] Scopes { get; set; }

        public Func<IServiceBase, IApplicationRegistryService, IAuthSession, ApplicationRegistration>
            ApplicationDirectoryResolver
        { get; set; } =
            (serviceBase, registryService, authSession) =>
            {
                var directoryName = GetDirectoryNameFromUsername(authSession.UserName);
                return registryService.GetApplicationByDirectoryName(directoryName);
            };

        #region Validate token helpers taken from OAuth2Provider
        // taken from OAuth2Provider class https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Authentication.OAuth2/OAuth2Provider.cs
        // and GoogleOAuth2Provider class  https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Authentication.OAuth2/GoogleOAuth2Provider.cs
        public string VerifyAccessTokenUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={0}";
        public string UserProfileUrl { get; set; }

        private object AuthenticateWithAccessToken(IServiceBase authService, IAuthSession session, IAuthTokens tokens, string accessToken)
        {
            tokens.AccessToken = accessToken;

            var authInfo = this.CreateAuthInfo(accessToken);

            session.IsAuthenticated = true;

            return OnAuthenticated(authService, session, tokens, authInfo);
        }

        public bool OnVerifyAccessToken(string token)
        {
            string stsDiscoveryEndpoint = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

            //This code requires non .netstandard2.0 dependencies (Microsoft.IdentityModel.Protocols.OpenIdConnect)

            //var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint);

            //var config = configManager.GetConfigurationAsync().Result;

            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                IssuerSigningTokens = config.SigningTokens,
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

            SecurityToken jwt;

            var result = tokendHandler.ValidateToken(token, validationParameters, out jwt);

            return jwt as JwtSecurityToken == null;
        }

        protected Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            var url = this.UserProfileUrl.AddQueryParam("access_token", accessToken);
            string json = url.GetJsonFromUrl();
            var obj = JsonObject.Parse(json);
            var authInfo = new Dictionary<string, string>
            {
                { "user_id", obj["id"] },
                { "username", obj["email"] },
                { "email", obj["email"] },
                { "name", obj["name"] },
                { "first_name", obj["given_name"] },
                { "last_name", obj["family_name"] },
                { "gender", obj["gender"] },
                { "birthday", obj["birthday"] },
                { "link", obj["link"] },
                { "picture", obj["picture"] },
                { "locale", obj["locale"] },
                { AuthMetadataProvider.ProfileUrlKey, obj["picture"] },
            };
            return authInfo;
        }
        #endregion
        #endregion

        #region Public/Internal

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var uri = new Uri(authService.Request.AbsoluteUri);
            if (CallbackUrl.IsNullOrEmpty())
            {
                CallbackUrl =
                    $"{uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)}/{uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped)}";
            }
            var tokens = Init(authService, ref session, request);

            // taken from OAuth2Provider class https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Authentication.OAuth2/OAuth2Provider.cs
            {
                //Transfering AccessToken/Secret from Mobile/Desktop App to Server
                if (request?.AccessToken != null)
                {
                    if (!OnVerifyAccessToken(request.AccessToken))
                        return HttpError.Unauthorized($"AccessToken is not for the configured {Provider} App");

                    var failedResult = AuthenticateWithAccessToken(authService, session, tokens, request.AccessToken);
                    var isHtml = authService.Request.IsHtml();
                    if (failedResult != null)
                        return ConvertToClientError(failedResult, isHtml);

                    return isHtml
                        ? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")))
                        : null; //return default AuthenticateResponse
                }
            }


            var query = new NameValueCollection();
            var httpRequest = authService.Request.QueryString;
            foreach (string s in httpRequest.AllKeys)
            {
                query.Add(s, httpRequest[s]);
            }

            if (HasError(query))
            {
                var result = RedirectDueToFailure(authService, session, query);
                return result;
            }

            // TODO: Can State property be added to IAuthSession to avoid this cast
            var userSession = session as AuthUserSession;
            if (userSession == null)
                throw new NotSupportedException("Concrete dependence on AuthUserSession because of State property");

            var code = query["code"];
            if (code.IsNullOrEmpty())
                return RequestCode(authService, request, session, userSession, tokens);

            var state = query["state"];
            if (state != userSession.State)
            {
                session.IsAuthenticated = false;
                throw new UnauthorizedAccessException("Mismatched state in code response.");
            }

            return RequestAccessToken(authService, session, code, tokens, request);
        }

        public override object Logout(IServiceBase service, Authenticate request)
        {
            var redirect = RedirectToMicrosoftLogout(service);
            var baseLogout = base.Logout(service, request);
            return redirect ?? baseLogout;
        }

        public IHttpResult RedirectToMicrosoftLogout(IServiceBase authService)
        {
            var tokens = authService.GetSession()
                .ProviderOAuthAccess.SingleOrDefault(a => a.Provider == MsGraph.ProviderName);

            var clientId = tokens?.Items["ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
                return null;

            // See https://msdn.microsoft.com/en-us/office/office365/howto/authentication-v2-protocols
            var request = MsGraph.DefaultAuthorizationUrl + "/logout?client_id={0}&post_logout_redirect_uri={1}"
                              .Fmt(clientId, this.RedirectUrl);
            return authService.Redirect(LogoutUrlFilter(this, request));
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            var accessToken = authInfo["access_token"];

            var meData = _graphService.Me(accessToken);
            tokens.FirstName = meData.FirstName;
            tokens.LastName = meData.LastName;
            tokens.Email = meData.Email;
            tokens.Language = meData.Language;
            tokens.PhoneNumber = meData.MobileNumber;

            tokens.Items["id"] = meData.ID.ToString();
            tokens.Items["jobtitle"] = meData.JobTitle;
            tokens.Items["userprincipalname"] = meData.UserPrincipalName;
            tokens.Items["officelocation"] = meData.OfficeLocation;
            tokens.Items["businessphones"] = meData.BusinessPhones != null ? string.Join(";", meData.BusinessPhones) : string.Empty;
            try
            {
                var groups = _graphService.GetMemberGroups(accessToken);

                if (groups != null)
                    tokens.Items["security-groups"] = JsonSerializer.SerializeToString(groups);
            }
            catch (WebException ex)
            {
                Log.WarnFormat("Failed to fetch member groups");
                if (!ex.IsForbidden())
                    throw;
            }
            return base.OnAuthenticated(authService, session, tokens, authInfo);
        }

        #endregion

        #region Private

        private static void RegisterProviderService(IAppHost host)
        {
            host.RegisterService(typeof(GraphAuthService));
        }

        private object RequestAccessToken(IServiceBase authService, IAuthSession session, string code,
            IAuthTokens tokens, Authenticate request)
        {
            try
            {
                var appRegistry = authService.TryResolve<IApplicationRegistryService>();
                if (appRegistry == null)
                    throw new InvalidOperationException(
                        $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

                var registration = ApplicationDirectoryResolver(authService, appRegistry, session);
                if (registration == null)
                    throw new UnauthorizedAccessException($"Authorization for directory failed.");

                try
                {
                    var tokenResponse = _graphService.RequestAuthToken(new AuthTokenRequest
                    {
                        CallbackUrl = CallbackUrl,
                        Registration = registration,
                        RequestCode = code,
                        Scopes = Scopes
                    });


                    tokens.AccessTokenSecret = tokenResponse.AccessToken;
                    tokens.RefreshToken = tokenResponse.RefreshToken;

                    var res = OnAuthenticated(authService, session, tokens, tokenResponse.AuthData.ToDictionary());

                    if (res == null)
                        return authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));

                    if (!registration.AllowAllWindowsAuthUsers)
                    {
                        if (!tokens.Items.ContainsKey("security-groups"))
                            throw HttpError.Unauthorized(ErrorMessages.WindowsAuthFailed);

                        var groups = JsonSerializer.DeserializeFromString<string[]>(tokens.Items["security-groups"]);
                        foreach (var requiredRole in registration.LimitAccessToRoles)
                        {
                            if (groups.Contains(requiredRole))
                            {
                                return res;
                            }
                        }

                        throw HttpError.Unauthorized(ErrorMessages.WindowsAuthFailed);
                    }

                    return res;
                }
                catch (AzureServiceException ase)
                {
                    return RedirectDueToFailure(authService, session, ase.ErrorData);
                }
            }
            catch (WebException webException)
            {
                if (webException.Response == null)
                    return RedirectDueToFailure(authService, session, new NameValueCollection
                    {
                        {"error", webException.GetType().ToString()},
                        {"error_description", webException.Message}
                    });
                Log.Error("Auth Failure", webException);
                var response = (HttpWebResponse)webException.Response;
                var responseText = Encoding.UTF8.GetString(
                    response.GetResponseStream().ReadFully());
                var errorInfo = JsonObject.Parse(responseText).ToNameValueCollection();
                return RedirectDueToFailure(authService, session, errorInfo);
            }
        }

        private object RequestCode(IServiceBase authService, Authenticate request, IAuthSession session,
            AuthUserSession userSession, IAuthTokens tokens)
        {
            var appRegistry = authService.TryResolve<IApplicationRegistryService>();
            if (appRegistry == null)
                throw new InvalidOperationException(
                    $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

            session.UserName = request.UserName;
            var registration = ApplicationDirectoryResolver(authService, appRegistry, session);
            if (registration == null)
                throw new UnauthorizedAccessException($"Authorization for directory failed.");

            var codeRequestData = _graphService.RequestAuthCode(new AuthCodeRequest
            {
                CallbackUrl = CallbackUrl,
                Registration = registration,
                Scopes = Scopes,
                UserName = request.UserName
            });

            tokens.Items["ClientId"] = registration.ClientId;
            userSession.State = codeRequestData.State;
            authService.SaveSession(session, SessionExpiry);
            return authService.Redirect(PreAuthUrlFilter(this, codeRequestData.AuthCodeRequestUrl));
        }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        protected override string GetReferrerUrl(IServiceBase authService, IAuthSession session,
            Authenticate request = null)
        {
            return authService.Request.GetParam("redirect") ??
                   base.GetReferrerUrl(authService, session, request);
            // Note that most auth providers redirect to the referrer url upon failure.
            // This implementation throws a monkey-wrench in that because we are here
            // setting the referrer url to the secure (authentication required) resource.
            // Thus redirecting to the referrer url on auth failure causes a redirect loop.
            // Therefore this auth provider redirects to FailureRedirectPath
            // The bottom line is that the user's destination should be different between success and failure
            // and the base implementation does not naturally support that
        }

        private void FailAndLogError(IAuthSession session, NameValueCollection errorInfo)
        {
            session.IsAuthenticated = false;
            if (HasError(errorInfo))
                Log.Error("{0} OAuth2 Error: '{1}' : \"{2}\" <{3}>".Fmt(
                    Provider,
                    errorInfo["error"],
                    errorInfo["error_description"].UrlDecode(),
                    errorInfo["error_uri"].UrlDecode()));
            else
                Log.Error("Unknown {0} OAuth2 Error".Fmt("Provider"));
        }

        protected IHttpResult RedirectDueToFailure(IServiceBase authService, IAuthSession session,
            NameValueCollection errorInfo)
        {
            FailAndLogError(session, errorInfo);
            var baseUrl = new Uri(authService.Request.AbsoluteUri).GetComponents(UriComponents.SchemeAndServer,
                UriFormat.SafeUnescaped); // .Request.GetBaseUrl();
            var destination = !FailureRedirectPath.IsNullOrEmpty()
                ? baseUrl + FailureRedirectPath
                : session.ReferrerUrl ?? baseUrl;
            var fparam = errorInfo["error"] ?? "Unknown";
            return authService.Redirect(FailedRedirectUrlFilter(this, $"{destination}?f={fparam}&response_type=code"));
        }

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        private static bool HasError(NameValueCollection info)
        {
            return !(info["error"] ?? info["error_uri"] ?? info["error_description"]).IsNullOrEmpty();
        }


        private static string GetDirectoryNameFromUsername(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new UnauthorizedAccessException("Directory name not found");

            var idx = userName.LastIndexOf("@", StringComparison.Ordinal);
            if (idx < 0)
                throw new UnauthorizedAccessException("Application directory not found");

            return userName.Substring(idx + 1);
        }

        protected override void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            try
            {
                var jwt = new JwtSecurityToken(authInfo["id_token"]);
                var p = jwt.Payload;
                tokens.UserId = (string)p["oid"];
                tokens.UserName = (string)p["preferred_username"];
                tokens.DisplayName = (string)p.GetValueOrDefault("name");
                tokens.Items.Add("TenantId", (string)p["tid"]);
                tokens.RefreshTokenExpiry = jwt.ValidFrom.Add(RefreshTokenLifespan);


                if (SaveExtendedUserInfo)
                    p.Each(x => authInfo[x.Key] = x.Value.ToString());
            }
            catch (KeyNotFoundException ex)
            {
                Log.Error("Reading user auth info from JWT", ex);
                throw;
            }

            LoadUserOAuthProvider(userSession, tokens);
        }

        #endregion
    }
}