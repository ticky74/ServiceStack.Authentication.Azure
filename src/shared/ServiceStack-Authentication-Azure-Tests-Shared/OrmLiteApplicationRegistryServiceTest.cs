namespace ServiceStack.Authentication.Azure.Tests 
{    
    using System;
    using ServiceStack.Data;
    using ServiceStack.OrmLite;
    using ServiceStack.Configuration;
    using ServiceStack.Authentication.Azure;
    using ServiceStack.Authentication.Azure.Entities;
    using ServiceStack.Authentication.Azure.OrmLite;
    using Xunit;

    public class OrmLiteApplicationRegistryServiceTest 
    {
        private IDbConnectionFactory _connectionFactory;

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

        public OrmLiteApplicationRegistryServiceTest()
        {
            _connectionFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        }

        [Fact]
        public void ShouldNotFindDirectory()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }            
            var dir = _service.GetApplicationByDirectoryName(Directory1.DirectoryName);
            Assert.Null(dir);
        }

        [Fact]
        public void ShouldFindDirectory()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }              
            var inserted = _service.RegisterApplication(Directory2);

            var result = _service.GetApplicationByDirectoryName(Directory2.DirectoryName);

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
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }              
            var result = _service.RegisterApplication(Directory1);

            Assert.NotNull(result);
            Assert.NotSame(result, Directory1);
            Assert.True(result.Id>0);
            Assert.Equal(result.ClientSecret, "secret");
            Assert.Equal(result.DirectoryName, Directory1.DirectoryName);
            Assert.Null(result.RefId);
            Assert.Null(result.RefIdStr);            
        }

        [Fact]
        public void ShouldNotCreateDuplicateDirectory()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }
            var result = _service.RegisterApplication(Directory1);
            Action action = () => _service.RegisterApplication(Directory1);
            
            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void ShouldCreateMultipleDirectories()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }               
            _service.RegisterApplication(Directory1);
            _service.RegisterApplication(Directory2);

            var result1 = _service.ApplicationIsRegistered(Directory1.DirectoryName);
            var result2 = _service.ApplicationIsRegistered(Directory2.DirectoryName);

            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public void ShouldExist()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }               
            _service.RegisterApplication(Directory1);

            var isRegistered = _service.ApplicationIsRegistered(Directory1.DirectoryName);

            Assert.True(isRegistered);
        }

        [Fact]
        public void ShouldNotExist()
        {
            var _service = new OrmLiteApplicationRegistryService(_connectionFactory);
            using (var db = _connectionFactory.OpenDbConnection())
            {
                db.DropAndCreateTable<ApplicationRegistration>();
            }            
            _service.RegisterApplication(Directory1);

            var isRegistered = _service.ApplicationIsRegistered(Directory2.DirectoryName);

            Assert.False(isRegistered);
        }        
    }
}