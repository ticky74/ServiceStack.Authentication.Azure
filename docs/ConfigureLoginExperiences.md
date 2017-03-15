## Configure Login Experience

By default ServiceStack.Authentication.Azure requires a username to be sent along with the Authenticate
request. The purpose of the username in the default configuration is two fold:

1. In order to determine which azure directory the user wants to authenticate with, ServiceStack.Authentication.Azure
will strip the upn suffix off of the username and use it to look up your azure directory values needed to perform the authentication.
2. It is used to hydrate the domain_hint url parameter that can be sent to Azure Graph. The idea here was that the login flow could
figure out which directory you were interested in and pre-populate some things for you. Honestly, I think I've seen more lunar eclipses
than this work seemlessly, it's not required.


This can actually be a bit of a pain in the ass if you are implementing either a single tenant
configuration or you would like to use an alternate mechanism that 'silently' figures out what directory 
you would like to authenticate with Azure Graph. For example, if you are implementing a web app you may want to map 
custom domains to each tenant and use the url to lookup what tenant the user is attempting to access.

To handle this you can supply the AzureAuthenticationProvider with your own ApplicationDirectoryResolver by simply assigning a Func 
to the provider in your authentication setup. If you do not specify a value for the ApplicationDirectoryResolver, the default
functionality described above will be used during runtime.

### Example of using a custom ApplicationDirectoryResolver functionality

```

 // Inside your AppHost
 ...
Plugins.Add(new AuthFeature(() => new AuthUserSession(),
    new IAuthProvider[]
    {
        new new AzureAuthenticationProvider()
        {
            ApplicationDirectoryResolver = (serviceBase, registryService, session) => 
            {                
                // Look up request dns info
                var requestedTenantDomain = new Uri(serviceBase.Request.AbsoluteUri).DnsSafeHost;

                // Use the dns to return the Azure Graph ApplicationRegistration
                return registryService.GetApplicationByDirectoryName(requestedTenantDomain);
            }
        }
    })
 ...
```
