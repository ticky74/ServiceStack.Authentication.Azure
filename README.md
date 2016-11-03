# <img src="Icon.jpeg" width="51" height="40" /> ServiceStack.Authentication.Azure

[![perimeter9 MyGet Build Status](https://www.myget.org/BuildSource/Badge/perimeter9?identifier=cde150f5-6369-476d-9d57-45ae14af0572)](https://www.myget.org/)

Azure Active Directory via Azure Graph for ServiceStack

### Kudos
First off I would like to really thank [@jfoshee](https://github.com/jfoshee) for his work enabling authentication with 
Azure through the awesome [ServiceStack.Authentication.Aad library](https://github.com/jfoshee/ServiceStack.Authentication.Aad). 
If you haven't checked it out yet, you should. A lot (most) of the details for working with azure remain 
similiar or unchanged. You will notice @jfoshee referenced throughout the source. 

### Getting Started
For the majority of you who aren't my Mom and are uninterested in reading my rambling, just 
fire it up and follow the steps here.

### Azure v2.0 Endpoint
In a nutshell, Microsoft has converged the authentication scenarios of personal Microsoft 
accounts and Azure Active Directory, 
[tl;dr version](https://azure.microsoft.com/en-us/documentation/articles/active-directory-appmodel-v2-overview/). 
My feeling is that this is a good thing, one API to rule them all. This is also why I whipped 
up this project vs forking the original. In now way is one better than the other, they are 
just different in my opinion ergo separate repo.


### Configuring Azure
Before allowing users of your ServiceStack app to authenticate using the Azure Graph API
you must first let Azure know about your app. This process is known as 'Registering your 
App with Azure'. This process has been streamlined. It's easy.

1. Navigate to [https://apps.dev.microsoft.com/Landing](https://apps.dev.microsoft.com/Landing) and 
log in with your credentials. Remember to log in under the account that you wish to grant app 
access to, either your microsoft account, or your directory (AAD/Office 365) account. In order 
to grant access to your directory you will need sufficient permissions, likely god of gods.
![alt text](docs/img/user-login.png "Logo Title Text 1") 

