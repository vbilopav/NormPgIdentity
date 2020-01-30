using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Norm.Extensions;
using NormPgIdentity.Data;
using Xunit;

namespace NormPgIdentityTests
{
    public class UserStoreUnitTests : PostgreSqlUnitTestFixture
    {
        public UserStoreUnitTests(PostgreSqlFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Test_AddToRoleAsync()
        {
            // prepare
            using var userStore = new UserStore(Connection);
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> {Name = "name"};
            await roleStore.CreateAsync(role, CancellationToken.None);
            // act
            await userStore.AddToRoleAsync(new IdentityUser<long> {Id = 1}, "NAME", CancellationToken.None);
            // assert
            Assert.Equal((role.Id, 1), Connection.Single<long, long>("select role_id, user_id from user_role"));
        }

        [Fact]
        public async Task Test_GetRolesAsync()
        {
            // prepare
            using var userStore = new UserStore(Connection);
            using var roleStore = new RoleStore(Connection);
            var role1 = new IdentityRole<long> { Name = "name1" };
            var role2 = new IdentityRole<long> { Name = "name2" };
            var role3 = new IdentityRole<long> { Name = "name3" };
            await roleStore.CreateAsync(role1, CancellationToken.None);
            await roleStore.CreateAsync(role2, CancellationToken.None);
            await roleStore.CreateAsync(role3, CancellationToken.None);
            var user = new IdentityUser<long> {Id = 1};
            await userStore.AddToRoleAsync(user, "name1", CancellationToken.None);
            await userStore.AddToRoleAsync(user, "name2", CancellationToken.None);
            await userStore.AddToRoleAsync(user, "name3", CancellationToken.None);
            // act
            var roles = await userStore.GetRolesAsync(user, CancellationToken.None);
            // assert
            Assert.Equal(3, roles.Count);
            Assert.Contains("name1", roles);
            Assert.Contains("name2", roles);
            Assert.Contains("name3", roles);
        }

        [Fact]
        public async Task Test_GetUsersInRoleAsync()
        {
            // prepare
            using var userStore = new UserStore(Connection);
            using var roleStore = new RoleStore(Connection);
            var role = new IdentityRole<long> { Name = "name" };
            await roleStore.CreateAsync(role, CancellationToken.None);
            var user1 = new IdentityUser<long> { UserName = "user1"};
            var user2 = new IdentityUser<long> { UserName = "user2" };
            await userStore.CreateAsync(user1, CancellationToken.None);
            await userStore.CreateAsync(user2, CancellationToken.None);
            await userStore.AddToRoleAsync(user1, "name", CancellationToken.None);
            await userStore.AddToRoleAsync(user2, "name", CancellationToken.None);
            // act
            var users = await userStore.GetUsersInRoleAsync("name", CancellationToken.None);
            // assert
            Assert.Equal(2, users.Count);
            Assert.NotEmpty(users.Where(u => u.UserName == "user1"));
            Assert.NotEmpty(users.Where(u => u.UserName == "user2"));
        }

        [Fact]
        public async Task Test_IsInRoleAsync()
        {
            // prepare
            using var userStore = new UserStore(Connection);
            using var roleStore = new RoleStore(Connection);
            var role1 = new IdentityRole<long> { Name = "name1" };
            var role2 = new IdentityRole<long> { Name = "name2" };
            await roleStore.CreateAsync(role1, CancellationToken.None);
            await roleStore.CreateAsync(role2, CancellationToken.None);
            var user = new IdentityUser<long> { UserName = "user" };
            await userStore.CreateAsync(user, CancellationToken.None);
            await userStore.AddToRoleAsync(user, "name1", CancellationToken.None);
            // act
            var result1 = await userStore.IsInRoleAsync(user, "name1", CancellationToken.None);
            var result2 = await userStore.IsInRoleAsync(user, "name2", CancellationToken.None);
            // assert
            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact]
        public async Task Test_RemoveFromRoleAsync()
        {
            // prepare
            using var userStore = new UserStore(Connection);
            using var roleStore = new RoleStore(Connection);
            var role1 = new IdentityRole<long> { Name = "name" };
            await roleStore.CreateAsync(role1, CancellationToken.None);
            var user = new IdentityUser<long> { UserName = "user" };
            await userStore.CreateAsync(user, CancellationToken.None);
            await userStore.AddToRoleAsync(user, "name", CancellationToken.None);
            // act
            await userStore.RemoveFromRoleAsync(user, "name", CancellationToken.None);
            // assert
            Assert.False(await userStore.IsInRoleAsync(user, "name", CancellationToken.None));
        }
    }
}
