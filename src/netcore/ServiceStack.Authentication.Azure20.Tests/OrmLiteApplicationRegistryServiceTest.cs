using System;
using ServiceStack.Authentication.Azure.Entities;
using ServiceStack.Authentication.Azure.OrmLite;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using Xunit;

namespace ServiceStack.Authentication.Azure20.Tests
{
    public class OrmLiteApplicationRegistryServiceTest
    {
        #region Constants and Variables

        private readonly IDbConnectionFactory _connectionFactory;

        internal static readonly ApplicationRegistration Directory1 = new ApplicationRegistration
        {
            ClientSecret = "secret",
            ClientId = "ed0dd5aa6f3f4c368a53ede9ea77a140",
            DirectoryName = "@foo1.ms.com"
        };

        internal static readonly ApplicationRegistration Directory2 = new ApplicationRegistration
        {
            ClientSecret = "secret2",
            ClientId = "2b72c902f41f43549f2de8b530d6a803",
            DirectoryName = "@foo2.ms.com",
            RefId = 1,
            RefIdStr = "1"
        };

        #endregion

        #region Constructors

        public OrmLiteApplicationRegistryServiceTest()
        {
            _connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        }

        #endregion

        #region Public/Internal

        [Fact]
        public void ShouldNotFindDirectory()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            var dir = service.GetApplicationByDirectoryName(Directory1.DirectoryName);
            Assert.Null(dir);
        }

        [Fact]
        public void ShouldFindDirectory()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            var inserted = service.RegisterApplication(Directory2);

            var result = service.GetApplicationByDirectoryName(Directory2.DirectoryName);

            Assert.NotNull(result);
            Assert.NotSame(result, inserted);
            Assert.Equal(result.DirectoryName, inserted.DirectoryName);
            Assert.Equal(result.ClientId, inserted.ClientId);
            Assert.Equal(result.ClientSecret, inserted.ClientSecret);
            Assert.Equal(result.Id, inserted.Id);
            Assert.NotNull(result.RefId);
            Assert.NotNull(result.RefIdStr);
            Assert.Equal(result.RefId, inserted.RefId);
            Assert.Equal(result.RefIdStr, inserted.RefIdStr);
        }

        [Fact]
        public void ShouldCreateDirectory()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            var result = service.RegisterApplication(Directory1);

            Assert.NotNull(result);
            Assert.NotSame(result, Directory1);
            Assert.True(result.Id > 0);
            Assert.Equal(result.ClientSecret, "secret");
            Assert.Equal(result.DirectoryName, Directory1.DirectoryName);
            Assert.Null(result.RefId);
            Assert.Null(result.RefIdStr);
        }

        [Fact]
        public void ShouldNotCreateDuplicateDirectory()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }

            service.RegisterApplication(Directory1);
            Action action = () => service.RegisterApplication(Directory1);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ShouldCreateMultipleDirectories()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            service.RegisterApplication(Directory1);
            service.RegisterApplication(Directory2);

            var result1 = service.ApplicationIsRegistered(Directory1.DirectoryName);
            var result2 = service.ApplicationIsRegistered(Directory2.DirectoryName);

            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public void ShouldExist()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            service.RegisterApplication(Directory1);

            var isRegistered = service.ApplicationIsRegistered(Directory1.DirectoryName);

            Assert.True(isRegistered);
        }

        [Fact]
        public void ShouldNotExist()
        {
            var service = new OrmLiteMultiTenantApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            service.RegisterApplication(Directory1);

            var isRegistered = service.ApplicationIsRegistered(Directory2.DirectoryName);

            Assert.False(isRegistered);
        }

        #endregion
    }
}