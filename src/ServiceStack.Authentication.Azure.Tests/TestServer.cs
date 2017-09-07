using System;
using System.Threading;
using ServiceStack.Auth;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Authentication.Azure.ServiceModel;
using ServiceStack.Authentication.Azure.ServiceModel.Entities;
using ServiceStack.Authentication.Azure20.Tests;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Authentication.Azure.Tests
{
    public class TestServer
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static readonly ServiceStackHost _host;
        public static ServiceStackHost Current => _host;

        static TestServer()
        {
            if (_host != null) return;

            _semaphore.Wait();
            try
            {
                if (_host != null) return;

                var app = new TestAppHost().Init();

                // var app = new BasicAppHost
                // {
                //     ConfigureAppHost = host =>
                //     {
                //         //if (responseFilter != null)
                //         // host.GlobalResponseFilters.Add(responseFilter);

                //         host.OnEndRequestCallbacks.Add(request =>
                //         {

                //         });
                //         var authFeature = new AuthFeature(() => new AuthUserSession(),
                //                             new IAuthProvider[]
                //                             {
                //                                 new CredentialsAuthProvider(host.AppSettings),
                //                                 new AzureAuthenticationProvider(new TestAzureGraphService())
                //                                 {
                //                                 }
                //                             });
                //         // new AuthFeature(() => new AuthUserSession(),
                //         //     new IAuthProvider[]
                //         //     {
                //         //         //new CredentialsAuthProvider(host.AppSettings),
                //         //     // new AzureAuthenticationProvider(new TestAzureGraphService())
                //         //     // {
                //         //     // }
                //         //     }));

                //         // var container = host.GetContainer();
                //         // container.Register<IDbConnectionFactory>(
                //         //     new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
                //         // container.Register<IApplicationRegistryService>(
                //         //     c => new OrmLiteMultiTenantApplicationRegistryService(c.Resolve<IDbConnectionFactory>()));
                //         // container.Register<IAuthRepository>(
                //         //     c => new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));


                //         // host.AfterInitCallbacks.Add(appHost =>
                //         // {
                //         //     var authRepo = appHost.TryResolve<IAuthRepository>();
                //         //     authRepo.InitSchema();

                //         //     // var regService = host.TryResolve<IApplicationRegistryService>();
                //         //     // regService.InitSchema();
                //         //     // regService.RegisterApplication(new ApplicationRegistration
                //         //     // {
                //         //     //     ClientId = "clientid",
                //         //     //     ClientSecret = "clientsecret",
                //         //     //     DirectoryName = "foodomain.com"
                //         //     // });
                //         // });
                //     }
                // }.Init();

                _host = app;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}