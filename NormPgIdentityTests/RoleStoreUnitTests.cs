using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Norm.Extensions;
using NormPgIdentity.Data;
using Xunit;

namespace NormPgIdentityTests
{
    public class RoleStoreUnitTests : PostgreSqlUnitTestFixture
    {
        public RoleStoreUnitTests(PostgreSqlFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Test_CreateAsync()
        {
            // prepare
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> {Name = "name", ConcurrencyStamp = "stamp"};
            // act
            var result = await roleStore.CreateAsync(role, CancellationToken.None);
            // assert
            Assert.Equal(IdentityResult.Success, result);
            Assert.Equal((role.Id, "name", "NAME", "stamp"), Connection.Single<int, string, string, string>(@"
                          select id, name, normalized_name, concurrency_stamp from role"));
        }

        [Fact]
        public async Task Test_DeleteAsync()
        {
            // prepare
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> { Name = "name" };
            await roleStore.CreateAsync(role, CancellationToken.None);
            // act
            var result = await roleStore.DeleteAsync(role, CancellationToken.None);
            // assert
            Assert.Equal(IdentityResult.Success, result);
            Assert.False(Connection.Read("select 1 from role").Any());
        }

        [Fact]
        public async Task Test_FindByIdAsync()
        {
            // prepare
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> { Name = "name1", NormalizedName= "NormalizedName1" };
            await roleStore.CreateAsync(role, CancellationToken.None);
            await roleStore.CreateAsync(new IdentityRole<long> { Name = "name2" }, CancellationToken.None);
            await roleStore.CreateAsync(new IdentityRole<long> { Name = "name3" }, CancellationToken.None);
            // act
            var result = await roleStore.FindByIdAsync(role.Id.ToString(), CancellationToken.None);
            // assert
            Assert.Equal(role.Id, result.Id);
            Assert.Equal(role.Name, result.Name);
            Assert.Equal(role.NormalizedName, result.NormalizedName);
            Assert.Equal(role.ConcurrencyStamp, result.ConcurrencyStamp);
        }

        [Fact]
        public async Task Test_FindByNameAsync()
        {
            // prepare
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> { Name = "name1", NormalizedName = "NormalizedName1" };
            await roleStore.CreateAsync(role, CancellationToken.None);
            await roleStore.CreateAsync(new IdentityRole<long> { Name = "name2" }, CancellationToken.None);
            await roleStore.CreateAsync(new IdentityRole<long> { Name = "name3" }, CancellationToken.None);
            // act
            var result = await roleStore.FindByNameAsync(role.NormalizedName, CancellationToken.None);
            // assert
            Assert.Equal(role.Id, result.Id);
            Assert.Equal(role.Name, result.Name);
            Assert.Equal(role.NormalizedName, result.NormalizedName);
            Assert.Equal(role.ConcurrencyStamp, result.ConcurrencyStamp);
        }
    }
}
