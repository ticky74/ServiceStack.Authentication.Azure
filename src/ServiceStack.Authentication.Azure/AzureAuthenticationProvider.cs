using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Requests;
using ServiceStack.Configuration;
using ServiceStack.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using ServiceStack.Web;
using System.Text;

namespace ServiceStack.Authentication.Azure
{
    public class AzureAuthenticationProvider : OAuthProvider
    {
        #region Constants and Variables

        private readonly IAzureGraphService _graphService;
        private string _failureRedirectPath;

        #endregion

        #region Constructors

        public AzureAuthenticationProvider()
            : this(new AzureGraphService())
        {
        }

        public AzureAuthenticationProvider(IAzureGraphService graphService)
            : this(new AppSettings(), graphService)
        {
        }

        public AzureAuthenticationProvider(IAppSettings settings, IAzureGraphService graphService)
            : base(settings, MsGraph.Realm, MsGraph.ProviderName, "ClientId", "ClientSecret")
        {
            // Default Scopes. Not sure if this is a bad idea @ticky74
            Scopes = new[] {"User.Read", "offline_access", "openid", "profile"};
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

        #endregion

        #region Public/Internal

        // Implementation taken from @jfoshee Servicestack.Authentication.Aad
        // https://github.com/jfoshee/ServiceStack.Authentication.Aad/blob/master/ServiceStack.Authentication.Aad/AadAuthProvider.cs
        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            // TODO: WARN: Property 'redirect' does not exist on type 'ServiceStack.Authenticate'
            // TODO: WARN: Property 'code' does not exist on type 'ServiceStack.Authenticate'
            // TODO: WARN: Property 'session_state' does not exist on type 'ServiceStack.Authenticate'
            // TODO: The base Init() should strip the query string from the request URL
            if (CallbackUrl.IsNullOrEmpty())
            {
                var uri = new Uri(authService.Request.AbsoluteUri);
                CallbackUrl =
                    $"{uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped)}/{uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped)}";
            }
            var tokens = Init(authService, ref session, request);
            var httpRequest = authService.Request;
            var query = httpRequest.QueryString.ToNameValueCollection();
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

            return RequestAccessToken(authService, session, code, tokens);
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
            var request = MsGraph.AuthorizationUrl + "/logout?client_id={0}&post_logout_redirect_uri={1}"
                              .Fmt(clientId, "http://www.google.com");
            return authService.Redirect(LogoutUrlFilter(this, request));
        }

        public override IHttpResult OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            Dictionary<string, string> authInfo)
        {
            try
            {
                var accessToken = authInfo["access_token"];

                var meData = _graphService.Me(accessToken);
                tokens.FirstName = meData.FirstName;
                tokens.LastName = meData.LastName;
                tokens.Email = meData.Email;
                tokens.Language = meData.Language;
                tokens.PhoneNumber = meData.PhoneNumber;

                var groups = _graphService.GetMemberGroups(accessToken);
                tokens.Items["security-groups"] = JsonSerializer.SerializeToString(groups);
            }
            catch
            {
                // Ignore
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
            IAuthTokens tokens)
        {
            try
            {
                var appDirectory = GetDirectoryNameFromUsername(session.UserName);

                var appRegistry = authService.TryResolve<IApplicationRegistryService>();
                if (appRegistry == null)
                    throw new InvalidOperationException(
                        $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

                var registration = appRegistry.GetApplicationByDirectoryName(appDirectory);
                if (registration == null)
                    throw new UnauthorizedAccessException($"Authorization for directory @{appDirectory} failed.");

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

                    return OnAuthenticated(authService, session, tokens, tokenResponse.AuthData.ToDictionary())
                           ??
                           authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));
                }
                catch (AzureServiceException ase)
                {
                    return RedirectDueToFailure(authService, session, ase.ErrorData);
                }

//                var postData =
//                    $"grant_type=authorization_code&redirect_uri={CallbackUrl.UrlEncode()}&code={code}&client_id={registration.ClientId}&client_secret={registration.ClientSecret.UrlEncode()}&scope={BuildScopesFragment()}";
//                var result = MsGraph.TokenUrl.PostToUrl(postData);
//
//                var authInfo = JsonObject.Parse(result);
//                var authInfoNvc = authInfo.ToNameValueCollection();
//                if (HasError(authInfoNvc))
//                    return RedirectDueToFailure(authService, session, authInfoNvc);
//                tokens.AccessTokenSecret = authInfo["access_token"];
//                tokens.RefreshToken = authInfo["refresh_token"];
//                return OnAuthenticated(authService, session, tokens, authInfo.ToDictionary())
//                       ?? authService.Redirect(SuccessRedirectUrlFilter(this, session.ReferrerUrl.SetParam("s", "1")));
            }
            catch (WebException webException)
            {
                if (webException.Response == null)
                {
                    return RedirectDueToFailure(authService, session, new NameValueCollection
                    {
                        {"error", webException.GetType().ToString()},
                        {"error_description", webException.Message}
                    });
                }
                Log.Error("Auth Failure", webException);
                var response = ((HttpWebResponse) webException.Response);
                var responseText = Encoding.UTF8.GetString(
                    response.GetResponseStream().ReadFully());
                var errorInfo = JsonObject.Parse(responseText).ToNameValueCollection();
                return RedirectDueToFailure(authService, session, errorInfo);
            }
        }

        private object RequestCode(IServiceBase authService, Authenticate request, IAuthSession session,
            AuthUserSession userSession, IAuthTokens tokens)
        {
            var appDirectory = GetDirectoryNameFromUsername(request.UserName);
            session.UserName = request.UserName;

            var appRegistry = authService.TryResolve<IApplicationRegistryService>();
            if (appRegistry == null)
                throw new InvalidOperationException(
                    $"No {nameof(IApplicationRegistryService)} found registered in AppHost.");

            var registration = appRegistry.GetApplicationByDirectoryName(appDirectory);
            if (registration == null)
                throw new UnauthorizedAccessException($"Authorization for directory @{appDirectory} failed.");

            var codeRequestData = _graphService.RequestAuthCode(new AuthCodeRequest
            {
                CallbackUrl = CallbackUrl,
                Registration = registration,
                Scopes = Scopes,
                UserName = request.UserName
            });
            tokens.Items.Add("ClientId", registration.ClientId);
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
                tokens.UserId = (string) p["oid"];
                tokens.UserName = (string) p["preferred_username"];
                tokens.DisplayName = (string) p.GetValueOrDefault("name");
                tokens.Items.Add("TenantId", (string) p["tid"]);
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