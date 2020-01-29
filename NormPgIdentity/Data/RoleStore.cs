using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Norm.Extensions;

namespace NormPgIdentity.Data
{
    public sealed class RoleStore : IRoleStore<IdentityRole<long>>
    {
        private readonly DbConnection _connection;

        public RoleStore(DbConnection connection)
        {
            _connection = connection;
        }

        public void Dispose() { /*nothing to dispose*/ }

        public async Task<IdentityResult> CreateAsync(IdentityRole<long> role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            role.Id = await _connection.WithCancellationToken(cancellationToken).SingleAsync<long>(@"

                insert into ""role""
                (
                    name, normalized_name, concurrency_stamp
                )
                values
                (
                    @name, @normalized_name, @concurrency_stamp
                )
                returning id;

            ", ("name", role.Name),
                ("normalized_name", role.NormalizedName),
                ("concurrency_stamp", role.ConcurrencyStamp));

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityRole<long> role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"
            
                delete from ""role"" 
                where
                    id = @id

            ", ("id", role.Id));
            return IdentityResult.Success;
        }

        public async Task<IdentityRole<long>> FindByIdAsync(string roleId, CancellationToken cancellationToken) =>
            await FindAsync(cancellationToken, roleId: Convert.ToInt64(roleId));
            
        public async Task<IdentityRole<long>> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) =>
            await FindAsync(cancellationToken, name: normalizedRoleName);

        public Task<string> GetNormalizedRoleNameAsync(IdentityRole<long> role, CancellationToken cancellationToken) => Task.FromResult(role.NormalizedName);

        public Task<string> GetRoleIdAsync(IdentityRole<long> role, CancellationToken cancellationToken) => Task.FromResult(role.Id.ToString());

        public Task<string> GetRoleNameAsync(IdentityRole<long> role, CancellationToken cancellationToken) => Task.FromResult(role.Name);

        public Task SetNormalizedRoleNameAsync(IdentityRole<long> role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName = normalizedName;
            return Task.FromResult(0);
        }

        public Task SetRoleNameAsync(IdentityRole<long> role, string roleName, CancellationToken cancellationToken)
        {
            role.Name = roleName;
            return Task.FromResult(0);
        }

        public async Task<IdentityResult> UpdateAsync(IdentityRole<long> role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"

                update ""role""
                set
                    name = @name,
                    normalized_name = @normalized_name,
                    concurrency_stamp = @concurrency_stamp
                where
                    id = @id

            ", ("id", role.Id), 
                ("name", role.Name), 
                ("normalized_name", role.NormalizedName), 
                ("concurrency_stamp", role.ConcurrencyStamp));
            return IdentityResult.Success;
        }



        private async Task<IdentityRole<long>> FindAsync(CancellationToken cancellationToken, long? roleId = null, string name = null) =>
            await _connection.WithCancellationToken(cancellationToken).ReadAsync($@"
                
                select
                    id as {nameof(IdentityRole.Id)},
                    name as {nameof(IdentityRole.Name)},
                    normalized_name as {nameof(IdentityRole.NormalizedName)},
                    concurrency_stamp as {nameof(IdentityRole.ConcurrencyStamp)}

                from
                    ""role""

                where
                    ( @id is null or id = @id ) and ( @name is null or normalized_name = @name )

            
            ", ("id", roleId, DbType.Int64), ("name", name, DbType.String))
                .Select<IdentityRole<long>>()
                .FirstOrDefaultAsync(cancellationToken);
    }
}
