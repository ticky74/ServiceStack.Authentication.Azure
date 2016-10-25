using System.Runtime.CompilerServices;

#if NET462
[assembly: InternalsVisibleTo("ServiceStack.Authentication.Azure.Tests")]
#else
[assembly: InternalsVisibleTo("ServiceStack-Authentication-Azure-Tests")]
#endif