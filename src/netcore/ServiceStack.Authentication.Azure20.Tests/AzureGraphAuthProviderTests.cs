using System;
using System.Collections.Specialized;
using System.IO;
using Moq;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure;
using ServiceStack.Authentication.Azure.Entities;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Data;
using ServiceStack.Host.NetCore;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Web;
using Xunit;

namespace ServiceStack.Authentication.Azure20.Tests
{
    public class AzureGraphAuthProviderTests : IDisposable
    {
        #region Constants and Variables

        private readonly ServiceStackHost _appHost;

        #endregion

        #region Constructors

        // private readonly IApplicationRegistryService _registrationService;

        public AzureGraphAuthProviderTests()
        {
//            var connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
//            _registrationService = new OrmLiteApplicationRegistryService(connectionFactory);
//
//            _registrationService.InitSchema();
//            _registrationService.RegisterApplication(new ApplicationRegistration
//            {
//                ClientId = "clientid",
//                ClientSecret = "clientsecret",
//                DirectoryName = "foodomain.com",
//            });

            _appHost = new BasicAppHost
            {
                ConfigureAppHost = host =>
                {
                    host.Plugins.Add(
                        new AuthFeature(() => new AuthUserSession(),
                            new IAuthProvider[]
                            {
                                new AzureGraphAuthenticationProvider(new TestAzureGraphService())
                            }));

                    var autoFac = host.GetContainer();
                    autoFac.Register<IDbConnectionFactory>(
                        new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
                    autoFac.Register<IApplicationRegistryService>(
                        c => new OrmLiteMultiTenantApplicationRegistryService(c.Resolve<IDbConnectionFactory>()));
                    autoFac.Register<IAuthRepository>(c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));


                    host.AfterInitCallbacks.Add(appHost =>
                    {
                        var authRepo = appHost.TryResolve<IAuthRepository>();
                        authRepo.InitSchema();

                        var regService = host.TryResolve<IApplicationRegistryService>();
                        regService.InitSchema();
                        regService.RegisterApplication(new ApplicationRegistration
                        {
                            ClientId = "clientid",
                            ClientSecret = "clientsecret",
                            DirectoryName = "foodomain.com"
                        });
                    });
                }
            }.Init();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                _appHost.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        #endregion

        #region Public/Internal

        [Fact]
        public void ShouldHaveRegisteredAuthenticateService()
        {
            var subject = _appHost.TryResolve<AuthenticateService>();
            Assert.NotNull(subject);
        }

        [Fact]
        public void ShouldBeAuthProvider()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            Assert.IsAssignableFrom<AuthProvider>(subject);
            Assert.Equal("ms-graph", subject.Provider);
        }

        private AuthenticateService ArrangeAuthService()
        {
            var authService = _appHost.TryResolve<AuthenticateService>();
            authService.Request = new MockHttpRequest();
            return authService;
        }

        [Fact]
        public void ShouldRequestCode()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };


            var authService = ArrangeAuthService();
            var response = subject.Authenticate(authService, new AuthUserSession(), auth);

            var result = (IHttpResult) response;
            Assert.True(result.Headers["Location"].StartsWith(
                "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=clientid&response_type=code&redirect_uri=http%3a%2f%2flocalhost%2f&domain_hint=some.user@foodomain.com&scope=https%3a%2f%2fgraph.microsoft.com%2fUser.Read++https%3a%2f%2fgraph.microsoft.com%2foffline%5faccess++https%3a%2f%2fgraph.microsoft.com%2fopenid++https%3a%2f%2fgraph.microsoft.com%2fprofile"));
            var codeRequest = new Uri(result.Headers["Location"]);
            var query = PclExportClient.Instance.ParseQueryString(codeRequest.Query);
            Assert.Equal(query["response_type"], "code");
            Assert.Equal(query["client_id"], "clientid");
            Assert.Equal(query["redirect_uri"].UrlDecode(), subject.CallbackUrl);
            Assert.Equal("some.user@foodomain.com", query["domain_hint"]);
        }

        [Fact]
        public void ShouldSetCallbackUrlWithoutParameters()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };

            var request = new MockHttpRequest("auth", "GET", "text",
                "/auth/foo/bar?redirect=" + "http://localhost/secure-resource".UrlEncode(), new NameValueCollection
                {
                    {"redirect", "http://localhost/secure-resource"}
                }, Stream.Null, null);
            var mockAuthService = MockAuthService(request);

            var response = subject.Authenticate(mockAuthService.Object, new AuthUserSession(), auth);

            var result = (IHttpResult) response;
            var codeRequest = new Uri(result.Headers["Location"]);
            var query = PclExportClient.Instance.ParseQueryString(codeRequest.Query);
            Assert.Equal(query["response_type"], "code");
            Assert.Equal(subject.CallbackUrl, "http://localhost/auth/foo/bar");
            Assert.Equal(query["redirect_uri"].UrlDecode(), subject.CallbackUrl);
        }

        [Fact]
        public void ShouldRedirectToFailurePathIfErrorIn()
        {
            // See https://tools.ietf.org/html/rfc6749#section-4.1.2.1
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            subject.FailureRedirectPath = "/auth-failure";
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };

            var request = new MockHttpRequest("auth", "GET", "text", "/auth/foo?error=invalid_request",
                new NameValueCollection {{"error", "invalid_request"}}, Stream.Null, null);
            var mockAuthService = MockAuthService(request);

            var response = subject.Authenticate(mockAuthService.Object, new AuthUserSession(), auth);

            var result = (IHttpResult) response;
            //var redirectRequest = new Uri(result.Headers["Location"]);
            Assert.Equal("http://localhost/auth-failure?f=invalid_request&response_type=code",
                result.Headers["Location"]);
            var query = PclExportClient.Instance.ParseQueryString(new Uri(result.Headers["Location"]).Query);
            Assert.Equal("code", query["response_type"]);
        }

        [Fact]
        public void ShouldRequestToken()
        {
            // When an application sends a GET request for an authorization code, Azure AD sends a response to the
            // value of the redirect_uri parameter in the request. The response includes the following parameters:
            //      [admin_consent], code, session_state, state

            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };
            var session = new AuthUserSession {State = "D79E5777-702E-4260-9A62-37F75FF22CCE", UserName = auth.UserName};

            subject.CallbackUrl = "http://localhost/myapp/";
            var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
            {
                {
                    "code",
                    "AwABAAAAvPM1KaPlrEqdFSBzjqfTGBCmLdgfSTLEMPGYuNHSUYBrqqf_ZT_p5uEAEJJ_nZ3UmphWygRNy2C3jJ239gV_DBnZ2syeg95Ki-374WHUP-i3yIhv5i-7KU2CEoPXwURQp6IVYMw-DjAOzn7C3JCu5wpngXmbZKtJdWmiBzHpcO2aICJPu1KvJrDLDP20chJBXzVYJtkfjviLNNW7l7Y3ydcHDsBRKZc3GuMQanmcghXPyoDg41g8XbwPudVh7uCmUponBQpIhbuffFP_tbV8SNzsPoFz9CLpBCZagJVXeqWoYMPe2dSsPiLO9Alf_YIe5zpi-zY4C3aLw5g9at35eZTfNd0gBRpR5ojkMIcZZ6IgAA"
                },
                {"session_state", "7B29111D-C220-4263-99AB-6F6E135D75EF"},
                {"state", "D79E5777-702E-4260-9A62-37F75FF22CCE"}
            }, Stream.Null, null);
            var mockAuthService = MockAuthService(request);
            using (new HttpResultsFilter
            {
                StringResultFn = (tokenRequest, s) =>
                    //StringResultFn = tokenRequest =>
                    {
                        // To redeem an authorization code and get an access token,
                        // send an HTTP POST request to a common or tenant-specific Azure AD Authorization endpoint.
                        Assert.Equal(tokenRequest.RequestUri.ToString(),
                            "https://login.microsoftonline.com/common/oauth2/v2.0/token");
                        Assert.Equal(tokenRequest.Method, "POST");
                        Assert.Equal(tokenRequest.ContentType, "application/x-www-form-urlencoded");
                        // TODO: Test form data. Seems impossible: http://stackoverflow.com/questions/31630526/can-i-test-form-data-using-httpresultsfilter-callback
                        return TokenHelper.GetIdToken();
//                        @"{
//                          ""access_token"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik5HVEZ2ZEstZnl0aEV1THdqcHdBSk9NOW4tQSJ9.eyJhdWQiOiJodHRwczovL3NlcnZpY2UuY29udG9zby5jb20vIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlLyIsImlhdCI6MTM4ODQ0MDg2MywibmJmIjoxMzg4NDQwODYzLCJleHAiOjEzODg0NDQ3NjMsInZlciI6IjEuMCIsInRpZCI6IjdmZTgxNDQ3LWRhNTctNDM4NS1iZWNiLTZkZTU3ZjIxNDc3ZSIsIm9pZCI6IjY4Mzg5YWUyLTYyZmEtNGIxOC05MWZlLTUzZGQxMDlkNzRmNSIsInVwbiI6ImZyYW5rbUBjb250b3NvLmNvbSIsInVuaXF1ZV9uYW1lIjoiZnJhbmttQGNvbnRvc28uY29tIiwic3ViIjoiZGVOcUlqOUlPRTlQV0pXYkhzZnRYdDJFYWJQVmwwQ2o4UUFtZWZSTFY5OCIsImZhbWlseV9uYW1lIjoiTWlsbGVyIiwiZ2l2ZW5fbmFtZSI6IkZyYW5rIiwiYXBwaWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJhcHBpZGFjciI6IjAiLCJzY3AiOiJ1c2VyX2ltcGVyc29uYXRpb24iLCJhY3IiOiIxIn0.JZw8jC0gptZxVC-7l5sFkdnJgP3_tRjeQEPgUn28XctVe3QqmheLZw7QVZDPCyGycDWBaqy7FLpSekET_BftDkewRhyHk9FW_KeEz0ch2c3i08NGNDbr6XYGVayNuSesYk5Aw_p3ICRlUV1bqEwk-Jkzs9EEkQg4hbefqJS6yS1HoV_2EsEhpd_wCQpxK89WPs3hLYZETRJtG5kvCCEOvSHXmDE6eTHGTnEgsIk--UlPe275Dvou4gEAwLofhLDQbMSjnlV5VLsjimNBVcSRFShoxmQwBJR_b2011Y5IuD6St5zPnzruBbZYkGNurQK63TJPWmRd3mbJsGM0mf3CUQ"",
//                          ""token_type"": ""Bearer"",
//                          ""expires_in"": ""3600"",
//                          ""expires_on"": ""1388444763"",
//                          ""resource"": ""https://service.contoso.com/"",
//                          ""refresh_token"": ""AwABAAAAvPM1KaPlrEqdFSBzjqfTGAMxZGUTdM0t4B4rTfgV29ghDOHRc2B-C_hHeJaJICqjZ3mY2b_YNqmf9SoAylD1PycGCB90xzZeEDg6oBzOIPfYsbDWNf621pKo2Q3GGTHYlmNfwoc-OlrxK69hkha2CF12azM_NYhgO668yfcUl4VBbiSHZyd1NVZG5QTIOcbObu3qnLutbpadZGAxqjIbMkQ2bQS09fTrjMBtDE3D6kSMIodpCecoANon9b0LATkpitimVCrl-NyfN3oyG4ZCWu18M9-vEou4Sq-1oMDzExgAf61noxzkNiaTecM-Ve5cq6wHqYQjfV9DOz4lbceuYCAA"",
//                          ""scope"": ""user_impersonation"",
//                          ""id_token"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83ZmU4MTQ0Ny1kYTU3LTQzODUtYmVjYi02ZGU1N2YyMTQ3N2UvIiwiaWF0IjoxMzg4NDQwODYzLCJuYmYiOjEzODg0NDA4NjMsImV4cCI6MTM4ODQ0NDc2MywidmVyIjoiMS4wIiwidGlkIjoiN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlIiwib2lkIjoiNjgzODlhZTItNjJmYS00YjE4LTkxZmUtNTNkZDEwOWQ3NGY1IiwidXBuIjoiZnJhbmttQGNvbnRvc28uY29tIiwidW5pcXVlX25hbWUiOiJmcmFua21AY29udG9zby5jb20iLCJzdWIiOiJKV3ZZZENXUGhobHBTMVpzZjd5WVV4U2hVd3RVbTV5elBtd18talgzZkhZIiwiZmFtaWx5X25hbWUiOiJNaWxsZXIiLCJnaXZlbl9uYW1lIjoiRnJhbmsifQ.""
//                        }";
                    }
            })
            {
                var response = subject.Authenticate(mockAuthService.Object, session, auth);
                Assert.True(session.IsAuthenticated);
                var tokens = session.GetAuthTokens("ms-graph");
                Assert.NotNull(tokens);
                Assert.Equal(tokens.Provider, "ms-graph");
                Assert.Equal(tokens.AccessTokenSecret, TokenHelper.AccessToken);
                Assert.NotNull(tokens.RefreshTokenExpiry);
                Assert.Equal(tokens.RefreshToken, TokenHelper.RefreshToken);

                Assert.Equal(tokens.UserId, "d542096aa0b94e2195856b57e43257e4"); // oid
                Assert.Equal(tokens.UserName, "some.user@foodomain.com");
                Assert.Equal(tokens.LastName, "User");
                Assert.Equal(tokens.FirstName, "Some");
                Assert.Equal(tokens.DisplayName, "Some User");
                Assert.Equal(session.UserName, tokens.UserName);
                Assert.Equal(session.LastName, tokens.LastName);
                Assert.Equal(session.FirstName, tokens.FirstName);
                Assert.Equal(session.DisplayName, tokens.DisplayName);
                var result = (IHttpResult) response;
                Assert.True(result.Headers["Location"].StartsWith("http://localhost#s=1"));
            }
        }

        [Fact]
        public void ShouldSetReferrerFromRedirectParam()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };

            var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
            {
                {"redirect", "http://localhost/myapp/secure-resource"}
            }, Stream.Null, null);
            var mockAuthService = MockAuthService(request);
            var session = new AuthUserSession();

            subject.Authenticate(mockAuthService.Object, session, auth);

            Assert.Equal(session.ReferrerUrl, "http://localhost/myapp/secure-resource");
        }

        [Fact]
        public void ShouldNotAuthenticateIfDirectoryNameNotMatched()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };
            subject.CallbackUrl = "http://localhost/myapp/";
            var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
            {
                {"code", "code123"},
                {"state", "D79E5777-702E-4260-9A62-37F75FF22CCE"}
            }, Stream.Null, null);
            var mockAuthService = MockAuthService(request);
            using (new HttpResultsFilter
            {
                StringResult =
                    @"{
                          ""access_token"": ""token456"",
                          ""id_token"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83ZmU4MTQ0Ny1kYTU3LTQzODUtYmVjYi02ZGU1N2YyMTQ3N2UvIiwiaWF0IjoxMzg4NDQwODYzLCJuYmYiOjEzODg0NDA4NjMsImV4cCI6MTM4ODQ0NDc2MywidmVyIjoiMS4wIiwidGlkIjoiN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlIiwib2lkIjoiNjgzODlhZTItNjJmYS00YjE4LTkxZmUtNTNkZDEwOWQ3NGY1IiwidXBuIjoiZnJhbmttQGNvbnRvc28uY29tIiwidW5pcXVlX25hbWUiOiJmcmFua21AY29udG9zby5jb20iLCJzdWIiOiJKV3ZZZENXUGhobHBTMVpzZjd5WVV4U2hVd3RVbTV5elBtd18talgzZkhZIiwiZmFtaWx5X25hbWUiOiJNaWxsZXIiLCJnaXZlbl9uYW1lIjoiRnJhbmsifQ.""
                        }"
            })
            {
                var session = new AuthUserSession();

                try
                {
                    subject.Authenticate(mockAuthService.Object, session, auth);
                }
                catch (UnauthorizedAccessException)
                {
                }

                Assert.False(session.IsAuthenticated);
            }
        }

        [Fact]
        public void ShouldSaveOAuth2StateValue()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };
            var session = new AuthUserSession();

            var response = subject.Authenticate(MockAuthService().Object, session, auth);

            var result = (IHttpResult) response;
            var codeRequest = new Uri(result.Headers["Location"]);
            var query = PclExportClient.Instance.ParseQueryString(codeRequest.Query);
            var state = query["state"];
            Assert.Equal(session.State, state);
        }


        [Fact]
        public void ShouldAbortIfStateValuesDoNotMatch()
        {
            var subject = new AzureGraphAuthenticationProvider(new TestAzureGraphService());
            var auth = new Authenticate
            {
                UserName = "some.user@foodomain.com"
            };

            subject.CallbackUrl = "http://localhost/myapp/";
            var request = new MockHttpRequest("myapp", "GET", "text", "/myapp", new NameValueCollection
            {
                {"code", "code123"},
                {"session_state", "dontcare"},
                {"state", "state123"}
            }, Stream.Null, null);
            var mockAuthService = MockAuthService(request);
            using (new HttpResultsFilter
            {
                StringResultFn = (tokenRequest, s) => { return @"{
                          ""access_token"": ""fake token"",
                          ""id_token"": ""eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83ZmU4MTQ0Ny1kYTU3LTQzODUtYmVjYi02ZGU1N2YyMTQ3N2UvIiwiaWF0IjoxMzg4NDQwODYzLCJuYmYiOjEzODg0NDA4NjMsImV4cCI6MTM4ODQ0NDc2MywidmVyIjoiMS4wIiwidGlkIjoiN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlIiwib2lkIjoiNjgzODlhZTItNjJmYS00YjE4LTkxZmUtNTNkZDEwOWQ3NGY1IiwidXBuIjoiZnJhbmttQGNvbnRvc28uY29tIiwidW5pcXVlX25hbWUiOiJmcmFua21AY29udG9zby5jb20iLCJzdWIiOiJKV3ZZZENXUGhobHBTMVpzZjd5WVV4U2hVd3RVbTV5elBtd18talgzZkhZIiwiZmFtaWx5X25hbWUiOiJNaWxsZXIiLCJnaXZlbl9uYW1lIjoiRnJhbmsifQ.""
                        }"; }
            })
            {
                var session = new AuthUserSession
                {
                    State = "state133" // Not the same as the state in the request above
                };

                try
                {
                    subject.Authenticate(mockAuthService.Object, session, auth);
                }
                catch (UnauthorizedAccessException)
                {
                }

                Assert.False(session.IsAuthenticated);
            }
        }

        internal Mock<IServiceBase> MockAuthService(MockHttpRequest request = null)
        {
            request = request ?? new MockHttpRequest();
            var response = new MockHttpResponse(request);

            var mockAuthService = new Mock<IServiceBase>();
            mockAuthService.SetupGet(s => s.Request).Returns(request);
            mockAuthService.Setup(s => s.TryResolve<IApplicationRegistryService>()).Returns(
                _appHost.Resolve<IApplicationRegistryService>());
            return mockAuthService;
        }

        #endregion
    }
}