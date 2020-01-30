using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                .Select(item => (item, Path.GetFileNameWithoutExtension(item).ToLower()))
                .Where(item =>
                {
                    var (_, name) = item;
                    return direction switch
                    {
                        MigrationDirection.Up when name.EndsWith(UpEndsWith) => true,
                        MigrationDirection.Down when name.EndsWith(DownEndsWith) => true,
                        _ => false
                    };
                })
                .Select(item =>
                {
                    var (full, name) = item;
                    return (Convert.ToInt32(name.Split("_").FirstOrDefault()), full);
                });

            return direction == MigrationDirection.Up ? result.OrderBy(item => item) : result.OrderByDescending(item => item);
        }

        public static void ThrowIfNotApplied(string connectionString, string path = "Migrations")
        {
            using var connection = new NpgsqlConnection(connectionString);
            var migrations = EnumerateMigrations(MigrationDirection.Up, path);

            try
            {
                var missing = connection.Read<int>(@"

                        select v
                        from 
                            unnest(@array) v
                            left outer join schema_version s 
                            on v = s.version
                        where
                            s.version is null

                        ", 
                        new NpgsqlParameter("array", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                        {
                            Value = migrations.Select(item => item.id).ToList()
                        })
                    .ToList();

                if (missing.Count != 0)
                {
                    throw new ApplicationException(
                        $"Some migrations are not applied. Missing migrations in database:" +
                        $"{Environment.NewLine}{string.Join(Environment.NewLine, migrations.Where(m => missing.Contains(m.id)).Select(m => m.name))}{Environment.NewLine}");
                }
            }
            catch (PostgresException exception) when (exception.SqlState == "42P01")   // 42P01=undefined_table, see https://www.postgresql.org/docs/9.3/errcodes-appendix.html
            {
                throw new ApplicationException("No migrations are yet applied. Run all available migrations.");
            }
        }
    }
}
