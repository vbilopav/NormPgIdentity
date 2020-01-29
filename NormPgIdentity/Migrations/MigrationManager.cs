using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Norm.Extensions;
using Npgsql;
using NpgsqlTypes;

namespace NormPgIdentity.Migrations
{
    public enum MigrationDirection { Up, Down}
    public class MigrationManager
    {
        private const string UpEndsWith = "__up";
        private const string DownEndsWith = "__down";

        public static IEnumerable<(int id, string name)> EnumerateMigrations(MigrationDirection direction, string path = "Migrations")
        {
            var result = Directory
                .EnumerateFiles(path, "*.sql", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(item => direction switch
                {
                    MigrationDirection.Up when item.EndsWith(UpEndsWith) => true,
                    MigrationDirection.Down when item.EndsWith(DownEndsWith) => true,
                    _ => false
                })
                .Select(item => (Convert.ToInt32(item.Split("_").FirstOrDefault()), item));

            return direction == MigrationDirection.Up ? result.OrderBy(item => item) : result.OrderByDescending(item => item);
        }

        public static void ThrowIfNotApplied(string connectionString, string path = "Migrations")
        {
            using var connection = new NpgsqlConnection(connectionString);
            var migrations = EnumerateMigrations(MigrationDirection.Up, path);

            var missing = connection.Read<int>(@"

                select v
                from 
                    unnest(@p) v
                    left outer join schema_version s 
                    on v = s.version
                where
                    s.version is null

                ", 
                new NpgsqlParameter("p", NpgsqlDbType.Array | NpgsqlDbType.Integer) {Value = migrations.Select(item => item.id).ToList()})
                .ToList();
            if (missing.Count != 0)
            {
                throw new ApplicationException(
                    $"Some migrations are not applied. Missing migrations in database:{Environment.NewLine}{string.Join(Environment.NewLine, migrations.Where(m => missing.Contains(m.id)).Select(m => m.name))}{Environment.NewLine}"); 
            }
        }
    }
}
