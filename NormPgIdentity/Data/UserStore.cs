using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Norm.Extensions;
using Npgsql;

namespace NormPgIdentity.Data
{
    public sealed class UserStore : 
        IUserStore<IdentityUser<long>>, 
        IUserEmailStore<IdentityUser<long>>, 
        IUserPhoneNumberStore<IdentityUser<long>>,
        IUserTwoFactorStore<IdentityUser<long>>, 
        IUserPasswordStore<IdentityUser<long>>, 
        IUserRoleStore<IdentityUser<long>>
    {
        private readonly DbConnection _connection;

        public UserStore(DbConnection connection)
        {
            _connection = connection;
        }

        public void Dispose() { /*nothing to dispose*/ }

        public async Task<IdentityResult> CreateAsync(IdentityUser<long> user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            user.Id = await _connection.WithCancellationToken(cancellationToken).SingleAsync<long>(@"

                insert into ""user""
                (
                    user_name, normalized_user_name, email, normalized_email, email_confirmed, 
                    password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed, 
                    two_factor_enabled, lockout_end, lockout_enabled, access_failed_count
                )
                values
                (
                    @user_name, @normalized_user_name, @email, @normalized_email, @email_confirmed, 
                    @password_hash, @security_stamp, @concurrency_stamp, @phone_number, @phone_number_confirmed, 
                    @two_factor_enabled, @lockout_end, @lockout_enabled, @access_failed_count
                )
                returning id;

            ", GetUserParameters(user));

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(IdentityUser<long> user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"
            
                delete from ""user"" 
                where
                    id = @id

            ", ("id", user.Id));
            return IdentityResult.Success;
        }

        public async Task<IdentityUser<long>> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            await FindAsync(cancellationToken, userId: Convert.ToInt64(userId));

        public async Task<IdentityUser<long>> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => 
            await FindAsync(cancellationToken, name: normalizedUserName);

        public Task<string> GetNormalizedUserNameAsync(IdentityUser<long> user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        
        public Task<string> GetUserIdAsync(IdentityUser<long> user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        
        public Task<string> GetUserNameAsync(IdentityUser<long> user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        
        public Task SetNormalizedUserNameAsync(IdentityUser<long> user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(IdentityUser<long> user, string userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.FromResult(0);
        }

        public async Task<IdentityResult> UpdateAsync(IdentityUser<long> user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"

                update ""user""
                set
                    user_name = @user_name, 
                    normalized_user_name = @normalized_user_name, 
                    email = @email,
                    normalized_email = @normalized_email, 
                    email_confirmed = @email_confirmed, 
                    password_hash = @password_hash, 
                    security_stamp = @security_stamp, 
                    concurrency_stamp = @concurrency_stamp, 
                    phone_number = @phone_number, 
                    phone_number_confirmed = @phone_number_confirmed, 
                    two_factor_enabled = @two_factor_enabled, 
                    lockout_end = @lockout_end, 
                    lockout_enabled = @lockout_enabled, 
                    access_failed_count = @access_failed_count
                where
                    id = @id

            ", GetUserParameters(user));
            return IdentityResult.Success;
        }

        public async Task<IdentityUser<long>> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
            => await FindAsync(cancellationToken, email: normalizedEmail);

        public Task<string> GetEmailAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.EmailConfirmed);

        public Task<string> GetNormalizedEmailAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedEmail);

        public Task SetEmailAsync(IdentityUser<long> user, string email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(IdentityUser<long> user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public Task SetNormalizedEmailAsync(IdentityUser<long> user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PhoneNumber);

        public Task<bool> GetPhoneNumberConfirmedAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PhoneNumberConfirmed);

        public Task SetPhoneNumberAsync(IdentityUser<long> user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            return Task.FromResult(0);
        }

        public Task SetPhoneNumberConfirmedAsync(IdentityUser<long> user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.TwoFactorEnabled);

        public Task SetTwoFactorEnabledAsync(IdentityUser<long> user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash);

        public Task<bool> HasPasswordAsync(IdentityUser<long> user, CancellationToken cancellationToken) =>
            Task.FromResult(user.PasswordHash != null);
        
        public Task SetPasswordHashAsync(IdentityUser<long> user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.FromResult(0);
        }

        public async Task AddToRoleAsync(IdentityUser<long> user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"

                insert into user_role ( user_id, role_id )
                
                select 
                    @id as user_id, role_id

                from 
                    ""role""

                where
                    normalized_name = @role

            ", ("id", user.Id), ("role", roleName.ToUpper()));
        }

        public async Task<IList<string>> GetRolesAsync(IdentityUser<long> user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _connection.WithCancellationToken(cancellationToken).ReadAsync<string>(@"

                select
                    r.name
                from
                    ""role"" r
                    
                    inner join user_role ur 
                    on r.id = ur.role_id and ur.user_id = @id

            ", ("id", user.Id)).ToListAsync(cancellationToken);
        }

        public async Task<IList<IdentityUser<long>>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) =>
            await _connection.WithCancellationToken(cancellationToken).ReadAsync($@"

                select
                    {GetUserSelectRecord()}

                from
                    ""user"" u
                    
                    inner join user_role ur 
                    on u.id = ur.user_id
                    
                    inner join ""role"" r 
                    on ur.role_id = r.id and r.normalized_name = @role

            ", ("role", roleName.ToUpper()))
                .Select<IdentityUser<long>>()
                .ToListAsync(cancellationToken);

        public async Task<bool> IsInRoleAsync(IdentityUser<long> user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _connection.WithCancellationToken(cancellationToken).ReadAsync<string>(@"

                select
                    1
                from
                    ""role"" r
                    
                    inner join user_role ur 
                    on r.id = ur.role_id and ur.user_id = @id

                where
                    r.normalized_name = @role

            ", ("id", user.Id), ("role", roleName.ToUpper())).AnyAsync(cancellationToken);
        }

        public async Task RemoveFromRoleAsync(IdentityUser<long> user, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _connection.WithCancellationToken(cancellationToken).ExecuteAsync(@"

                delete from user_role ur
                using ""role"" r
                where 
                    ur.role_id = r.id and 
                    r.normalized_name = @role and 
                    ur.user_id = @id

            ", ("id", user.Id), ("role", roleName.ToUpper()));
        }

        private async Task<IdentityUser<long>> FindAsync(CancellationToken cancellationToken, long? userId = null, string name = null, string email = null) =>
            await _connection.WithCancellationToken(cancellationToken).ReadAsync($@"
                
                select
                    {GetUserSelectRecord()}

                from
                    ""user""

                where
                    ( @id is null or id = @id ) and 
                    ( @name is null or normalized_user_name = @name ) and 
                    ( @email is null or normalized_email = @email )
            
            ", ("id", userId, DbType.Int64), ("name", name, DbType.String), ("email", email, DbType.String))
                .Select<IdentityUser<long>>()
                .FirstOrDefaultAsync(cancellationToken);

        private static string GetUserSelectRecord() => @$"
                id as {nameof(IdentityRole.Id)},
                user_name as {nameof(IdentityUser<long>.UserName)},
                normalized_user_name as {nameof(IdentityUser<long>.NormalizedUserName)},
                email as {nameof(IdentityUser<long>.Email)},
                normalized_email as {nameof(IdentityUser<long>.NormalizedEmail)},
                email_confirmed as {nameof(IdentityUser<long>.EmailConfirmed)},
                password_hash as {nameof(IdentityUser<long>.PasswordHash)},
                security_stamp as {nameof(IdentityUser<long>.SecurityStamp)},
                concurrency_stamp as {nameof(IdentityUser<long>.ConcurrencyStamp)},
                phone_number as {nameof(IdentityUser<long>.PhoneNumber)},
                phone_number_confirmed as {nameof(IdentityUser<long>.PhoneNumberConfirmed)},
                two_factor_enabled as {nameof(IdentityUser<long>.TwoFactorEnabled)},
                lockout_end as {nameof(IdentityUser<long>.LockoutEnd)},
                lockout_enabled as {nameof(IdentityUser<long>.LockoutEnabled)},
                access_failed_count as {nameof(IdentityUser<long>.AccessFailedCount)}
         ";

        private static (string, object)[] GetUserParameters(IdentityUser<long> user) =>
            new (string, object)[]
            {
                ("id", user.Id),
                ("user_name", user.UserName),
                ("normalized_user_name", user.NormalizedUserName),
                ("email", user.Email),
                ("normalized_email", user.NormalizedEmail),
                ("email_confirmed", user.EmailConfirmed),
                ("password_hash", user.PasswordHash),
                ("security_stamp", user.SecurityStamp),
                ("concurrency_stamp", user.ConcurrencyStamp),
                ("phone_number", user.PhoneNumber),
                ("phone_number_confirmed", user.PhoneNumberConfirmed),
                ("two_factor_enabled", user.TwoFactorEnabled),
                ("lockout_end", user.LockoutEnd),
                ("lockout_enabled", user.LockoutEnabled),
                ("access_failed_count", user.AccessFailedCount)
            };
    }
}
