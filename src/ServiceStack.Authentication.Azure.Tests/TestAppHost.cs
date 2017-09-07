using Funq;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure20.Tests;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.Authentication.Azure.Tests
{
    public class TestAppHost : AppHostBase
    {
        public TestAppHost()
                : base("Unit Test Host", typeof(TestAppHost).GetAssembly())
        {

        }

        public override void Configure(Container container)
        {
            var authFeature = new AuthFeature(() => new AuthUserSession(),
                       new IAuthProvider[]
                       {
                            new CredentialsAuthProvider(AppSettings),
                            new AzureAuthenticationProvider(new TestAzureGraphService())
                            {
                            }
                       });
            Plugins.Add(authFeature);

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
            container.Register<IApplicationRegistryService>(
                c => new OrmLiteMultiTenantApplicationRegistryService(c.Resolve<IDbConnectionFactory>()));
            container.Register<IAuthRepository>(
                c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));


            AfterInitCallbacks.Add(appHost =>
            {
                var authRepo = appHost.TryResolve<IAuthRepository>();
                authRepo.InitSchema();

                var regService = appHost.TryResolve<IApplicationRegistryService>();
                regService.InitSchema();
                regService.RegisterApplication(new ApplicationRegistration
                {
                    ClientId = "clientid",
                    ClientSecret = "clientsecret",
                    DirectoryName = "foodomain.com"
                });
            });
        }
    }
}