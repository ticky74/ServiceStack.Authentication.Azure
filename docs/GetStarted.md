## Get Started

1. Register your app with [Azure Developer Apps Portal](https://apps.dev.microsoft.com/Landing).
2. Register ServiceStack.Authentication.Azure auth provider with the  AuthFeature in your AppHost.cs
```
...
app.Plugins.Add(
    new AuthFeature(() => new AuthUserSession(), 
    new IAuthProvider[]
    {
        new AzureGraphAuthenticationProvider(), 
    }));
...
```
    3. Register an AzureRegistryService with your di container
        a) Single-Tenant
```

...
container.Register<IApplicationRegistryService>(
    c => SingleTenantApplicationRegistryService(
            new AzureDirectorySettings
            {
                ClientId = "<ApplicationId From App Registration Page>",
                ClientSecret = "<Super secret password generated on App Reg. Page>",
                DirectoryName = "<Azure directory name i.e. contoso.com>"
            }));
...

```
        b) Multi-Tenant: Notice the call to InitSchema, this will initialize the necessary 
        tables in your db to support storing authentication info for multiple directories.
```

...
container.Register<IApplicationRegistryService>(
    c => new OrmLiteMultiTenantApplicationRegistryService(c.Resolve<IDbConnectionFactory>()));

...
var registry = container.Resolve<IApplicationRegistryService>();
registry.InitSchema();

```
4. Post authentication requests to the /auth/ms-graph authentication provider and specify the 
email address (which is the account name) in the UserName property.
```

...
var auth = new Authenticate {
    provider = "ms-graph",
    UserName = "user@mydirectory.com"
};
...

```

That's all!