# .NET identity with custom PostgreSQL store, migrations, unit tests, and Norm.net

In this article, I'll describe implementation of the **custom .NET Core identity user store** using [Norm](https://github.com/vbilopav/NoOrm.Net) data access library with **`PostgreSQL` database.** Meaning that default implementation, with Entity Framework, will be replaced with custom implementation that uses `Norm` and `PostgreSQL` with hand-written SQL.

The entire source code described in this article can be found [here](https://github.com/vbilopav/NormPgIdentity).

## What is [Norm](https://github.com/vbilopav/NoOrm.Net)

[Norm](https://github.com/vbilopav/NoOrm.Net) - is modern data access library built for .NET Core 3.

It falls into the same category as older and much more popular cousin [Dapper](https://github.com/StackExchange/Dapper) called "Micro ORM's". I would even argue that "Micro ORM's" are not ORM's at all, and that is how [Norm](https://github.com/vbilopav/NoOrm.Net) got its name (No ORM - NoOrm - Norm), but that is entirely another subject.

## Focus of this article

> The focus of this article **will not be `Norm` data access** at all. Not even .NET Core custom identity store.

There are numerous examples of custom .NET Core identity implementations with `Dapper` all over the internet, you can find them fairly easy. `Norm` implementation is very similar, they both use raw SQL to communicate with the data server.

If you want to learn more about `Norm` you can check out this article: [Norm Data Access for .NET Core 3](https://dev.to/vbilopav/norm-data-access-for-net-core-3-fal)

Instead, I'll focus on the following:

- **PostgreSQL**

and especially:

- **Schema migration mechanism**
- **Unit testing**

Those last two (schema migration mechanisms and unit testing) - are sort of pain points of almost every database-backed project. While standard schema migration mechanism comes out of the box with every ORM tool, they are absent from so-called Micro ORM'sm including `Norm`. And, of course, unit testing backed by the database is always a challenge.

In fact, the number one reason why architects and developers don't opt-out for the solution with micro ORM's like `Dapper` or `Norm` is - lack of migrations and unit test troubles.

That is a mistake in my opinion.

Hopefully, I'll manage to demonstrate how you can use both efficiently and with ease.

## Quick round-up of custom identity store with .NET Core 3

- Standard stuff: Create a new .NET Core 3 Web Application with Individual User Accounts and with Store user accounts in-app options. Let the scaffolding do the work for you.

- Remove the following NuGet packages from your project, since we're not going to need them anymore:

    - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` - we use Norm instead of EF

    - `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` - we use Norm instead of EF

    - `Microsoft.EntityFrameworkCore.Tools` - we use Norm instead of EF

    - `Microsoft.EntityFrameworkCore.SqlServer` - we use PostgreSQL instead of Sql Server

- The only one that we will leave is `Microsoft.AspNetCore.Identity.UI` - package with default implementation of default identity UI (login screen, register screen, etc).

- Add the following packages to your project:

    - `Npgsql` - the open-source .NET data provider for PostgreSQL.

    - `Norm.net` - the open-source data access for .NET Core 3.

    - `System.Linq.Async` - the open-source `Linq` (Language Integrated Query) implementation for asynchronous interfaces like `IAsyncEnumerable` by .NET Foundation and Contributors.

- On your local PostgreSQL server, create a new database for our application. For this example, I'm using name `norm_pg_identity` and add the following connection string to your settings (with your chosen credentials, of course):

```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=norm_pg_identity;Port=5432;User Id=postgres;Password=postgres;"
  }
```

- In your `Startup` class, replace existing implementation of `ConfigureServices` method with this one:

```csharp
var connectionString = Configuration.GetConnectionString("DefaultConnection");
MigrationManager.ThrowIfNotApplied(connectionString);
services.AddSingleton<DbConnection, NpgsqlConnection>(provider => new NpgsqlConnection(connectionString));

services.AddTransient<IUserStore<IdentityUser<long>>, UserStore>();
services.AddTransient<IRoleStore<IdentityRole<long>>, RoleStore>();
services.AddTransient<IEmailSender, EmailSender>();
services.AddIdentity<IdentityUser<long>, IdentityRole<long>>(options =>
    {
        options.Password.RequiredLength = 6;
        // 
        // ...
        //
    })
    .AddDefaultTokenProviders()
    .AddDefaultUI();

services.AddRazorPages();
```

Let's dissect a bit:

- `MigrationManager.ThrowIfNotApplied(connectionString);` - will use a connection string to check are all available migrations applied, and if not, it will throw an exception with the appropriate message, thus preventing us to run the application with the wrong version of the schema. This is a part of the custom migration mechanism and more on important migration mechanism later.

- `services.AddSingleton<DbConnection, NpgsqlConnection>(provider => new NpgsqlConnection(connectionString));` - adds default PostgreSQL connection as singleton.

It's common practice to add database context and connection as scoped service (within request-response cycle) so that each request can be treated like a single database transaction and that indeed is the default for most of the applications.

However, we will create transactions manually so there is nothing wrong with having a singleton connection. DI mechanism will take care of proper disposal on application shutdown.

Single open PostgreSQL connection consumes around 50MB, so for now, it's good enough and even more efficient to have one connection within application scope. You may revert this to scoped service if you prefer that approach.

- Our custom data store implementations are injected as transient services with these two lines:

```csharp
services.AddTransient<IUserStore<IdentityUser<long>>, UserStore>();
services.AddTransient<IRoleStore<IdentityRole<long>>, RoleStore>();
```

This means that for `UserStore`, which implements data access for users - interface contract is `IUserStore<IdentityUser<long>>` which operates on `IdentityUser<long>` user model. The generic parameter `long` determines the type of primary key for this model which `long` or `bigint` on the database side. The default one is actually `string`, so we had to change that.

Full implementation for `UserStore` can be found in this [source file](https://github.com/vbilopav/NormPgIdentity/blob/master/NormPgIdentity/Data/UserStore.cs).

Similarly, for `RoleStore`, which implements data access for roles - interface contract is `IRoleStore<IdentityRole<long>>`. Full implementation for `UserRoleStoreStore` can be found in this [source file](https://github.com/vbilopav/NormPgIdentity/blob/master/NormPgIdentity/Data/RoleStore.cs).

- And at last, we can configure our identity mechanism:

```csharp
services.AddIdentity<IdentityUser<long>, IdentityRole<long>>(options =>
    {
        options.Password.RequiredLength = 6;
        // 
        // ...
        //
    })
    .AddDefaultTokenProviders()
    .AddDefaultUI();

services.AddRazorPages();
```

This will add identity for our two data models (`IdentityUser<long>` for users and `IdentityRole<long>`) with appropriate configuration options and with default security token provider. Finally, for our configured identity we add the default UI implementation so that we can actually use the application.

It should be noted that this is not the entire implementation of .NET Core identity: to enable two-factor authentication mechanism user authenticator key store should also be implemented in a similar fashion using interface contract `IUserAuthenticatorKeyStore<IdentityUser<long>>`.

You can do that also if you wish in the same way as two previous data stores, this is just a demo project.

Ok, now let's dive into database migrations.

## Database migrations

Database migrations are a very important and yet simple concept required by all database-backed applications and usually implemented by ORM tools. They provide developers with certain versioning features such as:

- Check the current schema version of your database.

- Check the available schema versions in your source code.

- Upgrade database schema up to certain version.

- Downgrade database schema down to a certain version.

To be able to do that, a couple of things are required, such as:

- An extra table in the database to track the current version as well as migration history. EF uses `__EFMigrationHistory` for example.

- Upgrade script and downgrade script for each available version.

This implementation doesn't use EF, and instead relays on [PL/pgSQL](https://www.postgresql.org/docs/12/plpgsql.html) - SQL procedural programming language supported by the PostgreSQL to do all the scripting needed for the upgrade and downgrade scripts.

`PL/pgSQL` as noted is a procedural programming language, and, it is actually a superset of your standard PostgreSQL SQL. 

That means that you can use SQL inside `PL/pgSQL` script - to define your data with Data Definition Language or [DDL](https://en.wikipedia.org/wiki/Data_definition_language) - to successfully implement schema changes. 

It also supports standard things like control structures, exception handling and much more, which is essential for our migration scripts.

Also, this simple implementation of migrations mechanism relays heavily of **file naming conventions.** Of course, this is not ideal, but it will do the work. The convention I've come up with is as follows:

- **`[version number]_[migration name][__up or __down].sql`**

So that means that first migration with name `initial` will have the following script names:

- `1_initial__up.sql` for the upgrade script.

- `1_initial__down.sql` for the downgrade script.

Of course, this crude mechanism that relays on file name convention can always be replaced with something more robust, but, again, it will do the work nicely.

Content of those scripts is anonymous `PL/pgSQL` scripts (not a function or a procedure) that are executed with [`DO`](https://www.postgresql.org/docs/9.0/sql-do.html) statement. For example: `do 'script body text';`.

String quotes are usually replaced with multiline dollar-quoted literals for editor language support, and the  code is wrapped into transactions, so the basic structure will have the following synopsis:

```sql
do
$$
[ declare
    declarations ]
begin
    statements
end
$$ language plpgsql;
```

The [initial script `1_initial__up.sql`](https://github.com/vbilopav/NormPgIdentity/blob/master/NormPgIdentity/Migrations/1_initial__up.sql) looks looks this:

```sql
do $$
declare _version int = 1;
begin

    create table if not exists schema_version (
        version int not null primary key,
        timestamp timestamp with time zone not null default (transaction_timestamp() at time zone 'utc')
    );

    if exists(select version from schema_version where version = _version) then
        raise exception 'migration % is already applied, exiting ...', _version;
    end if;

    create table "role" (
        id bigint not null generated always as identity primary key,
        name character varying null,
        normalized_name character varying null,
        concurrency_stamp text null
    );

    /*
    * ...
    * The rest of the script omitted, see source code for full version at:
    * https://github.com/vbilopav/NormPgIdentity/blob/master/NormPgIdentity/Migrations/1_initial__up.sql
    *
    */

    raise info 'applying migration version %', _version;

    insert into schema_version (version) values (_version) on conflict do nothing;

end
$$ language plpgsql;
```

- First, we declare a variable `declare _version int = 1;` with version number.

- Second, we create a table if it doesn't already exist - `schema_version` so we can track version number efficiently.

- And after that we check does that version already exists in the `schema_version` table. If it does, that means that the schema version is already applied - we exit the script by raising the exception with an appropriate message.

Note that instead of raising expecting like this:

```sql
raise exception 'migration % is already applied, exiting ...', _version;
```

We could have just display informational text and exit the script with a `return` statement:

```sql
raise info 'migration % is already applied, exiting ...', _version;
return;
```

It would have the same effect - script will exit immediately and **no changes would be applied (transaction rollback)**

Notice the most important thing in this script here - `begin` and the `end` statements. 

> **Everything is under a transaction.**

This is hugely important:

> **PostgreSQL is one of the rare database systems that does support transactional Data Definition Language or DDL**.

For example, and as far as I know - Oracle and MySQL variant does not support transactional DDL. Microsoft SQL Server does have support trough save points mechanism and it is a bit clunky as far as I can see...

Someone can correct freely me on this if I'm mistaken.

Why is this so important?

Well, if something goes wrong during the migration - all changes **will be reverted** to the point before migration and **you can be assured that you won't end up with strange artifacts from failed migration in your database.**

Transactional DDL is sometimes, indeed the life-saver.

The rest of this initial migration script - the entire identity DDL code is originally generated by Entity Framework for PostgreSQL (see [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL)), and then pasted into that script with minimal changes and tweaks.

However, here is the thing:

> Entity Framework for PostgreSQL or for any other database for that matter - **will NOT generate transactional DDL.**

That's one of the tweaks and interventions I have made to the EF generated code to take **full advantage of the power of PostgreSQL database.** 

Something that is simply not available with traditional ORM tools like Entity Framework...

Rest of the changes to the original DDL from Entity Framework are minimal and include the following:

- Primary keys are now the `bigint` type, instead of strings, and they generate identity seed. Example:

```sql
id bigint not null generated always as identity primary key,
```

- Foreign keys are marked as `deferrable`. This means that we can tell our transactions (optionally) - to check for integrity at the end of the transaction, just before commit. This is hugely beneficial in **unit test** which we will see later exactly why. Example of the foreign key in the script:

```sql
constraint "FK_role_claim_role_role_id" foreign key (role_id) references role (id) on delete cascade deferrable
```

That is the upgrade script. 

Downgrade script, named by convention discussed earlier `1_initial__down.sql` - looks like this:

```sql
do $$
declare _version int = 1;
begin

    if not exists(select version from schema_version where version = _version) then
        raise exception 'migration % is already removed, exiting ...', _version;
    end if;

    drop table role_claim;
    drop table user_claim;
    drop table user_login;
    drop table user_role;
    drop table user_token;

    drop table "role";
    drop table "user";

    raise info 'removing migration version %', _version;

    delete from schema_version where version = _version;

end
$$ language plpgsql;
```

It just cleans up what upgrade script created, leaves `schema_version` table for future use, but it removes version number the table.

I have also added one more migration using the same convention and the same principle as the initial script and the entire migration system.

It simply adds another table and fills it up with thousand of random strings. I use that table to show random data on the Index page after a user is authenticated and authorized successfully.

But, more importantly, it serves also to test the entire concept of the migration system. 

The entire source code for these migrations can be found on the following [location](https://github.com/vbilopav/NormPgIdentity/tree/master/NormPgIdentity/Migrations).

To run these migration scripts you can simply use some of the PostgreSQL tools, like for example:
`psql` - a command-line tool with `-f` or `--file` switch to run the specific script by name.
`pgAdmin` - GUI tool to, also run a specific script.
any other database tool at your disposal.

The only thing that you have to take in mind that you need to honor the convention and run the scripts in the specific order:

Upgrade script ordered ascending
Downgrade script ordered descending

You can very, very easily build your own command-line tool (separate or part of the main application or any way you prefer) - that will be mindful of that convention.

In fact, there is a class inside a migration folder that can help you with this.

### The `MigrationManager`class

The [`MigrationManager`](https://github.com/vbilopav/NormPgIdentity/blob/master/NormPgIdentity/Migrations/MigrationManager.cs) class is located at the same location as the scripts it manages and it only has two simple methods:

- **`EnumerateMigrations(direction, path)`** - enumerates all available migrations (id number and the file name) in the given direction (up or down) and in the given path by respecting the convention (ascending for up, descending for down).

This method can be used to build a simple migration tool that honors the convention. It could look something like this:

```csharp
public static void ApplyMigrations(NpgsqlConnection connection, MigrationDirection direction)
{
    foreach (var (_, name) in MigrationManager.EnumerateMigrations(direction, Path))
    {
        connection.Execute(File.ReadAllText(name));
    }
}
```

- `ThrowIfNotApplied(connectionString, path)` - will throw an exception with an appropriate message and terminate further execution - **if some of the available migrations are missing from the database.**

Implementation of this functionality will first see what are the available migration by enumerating upgrade scripts and then query the `schema_version` table to see if some of them are missing.

To be able to do this, the following query is used:

```csharp
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
```

Querying the database on each migration to see is it present - is a well-known data access problem or anti-pattern known as N+1 query anti-pattern, very typical for the ORM approach.

That would utilize network literally for each migration item, and network latency and bandwidth **multiplied by the number of migrations** would have to be counted into overall performances.

Obviously, such a problem would further escalate as the number of the multiple items (number of migrations in this case) would grow - so the best approach is to avoid it altogether.

What we do in this particular case is to create a list of missing version numbers by passing an **array parameter** of existing version numbers into the query.

The query itself then uses PostgreSQL `unnest` function which expands an array parameter into a set of rows and then we can just simply select version numbers that are missing from the left outer joined set.

This is many times more efficient than any other approach I'm aware of, and, as far as I know, no ORM tool (that I'm aware of) supports such specific constructs. Certainly not Entity Framework.

This is yet another example of how (by using so-called Micro ORM or no ORM at all approach) - we can **unleash the full power of this fully armed and operational PostgreSQL database** and gain a significant **performance boost.**

This covers the migration issue. 

Now, let's dive into unit testing with PostgreSQL, .NET Core and XUnit. It will be fun I promise :)

## Unit testing

Many developers struggle with testing backed by the database. And indeed can be challenging, because the schema needs to be in particular version suitable for testing, it can't contain any extra artifacts not found on production and test should be performed in isolation.

The solution that Entity Framework offers for testing is an **in-memory database provider**, which is somewhat inadequate in my opinion. For multiple reasons.

The solution that we're going to use will need live PostgreSQL instance, preferably the same version as one used in production, that we will use for testing only.

The execution sequence is the following:

- For each testing session (all tests in the test project), on session initialization:

    1) Create a new test database on the test server and connect.

    2) Execute all **upgrade** migrations.

- For each testing session (all tests in the test project), on session cleanup:

    1) Execute all **downgrade** migrations.

    2) Drop the test database and close the connection to the server.

This will create a fresh database with our target schema for our testing sessions, but more importantly - it will also **test the validity of all our migrations.**

- For each test in the test project, test initialization:

    1) Create a new connection to the test database and make it available to the test.

    2) Start a new transaction and set constraints to deferred mode.

- For each test in the test project, test cleanup:

    1) Rollback the transaction.

    2) Close and dispose of test connection.

This will ensure the absolute isolation (in terms of a database) of our tests. Each test will have its own connection that will be under transaction by default and it will be rollbacked when test finished, also by default. That assures that no test will see any changes from any other test.

Also, those transactions will have all of its constraints in deferred mode by default. As we said earlier, deferred mode simply deferred all constraint checks just before commit (which will not happen anyhow, unless you tell otherwise). 

This is very useful when inserting test data (test fixtures) into the database. With this mode, we can only insert into a single table that interests us in that particular test - **without having to insert all the related data in all the related tables.** 

Usually, there are dozens or more of related tables in the database, which would make out testing somewhat difficult. But, of course, if that scenario is important for your test, you can always disable that behavior.

Obviously, we're gonna need a bit of boilerplate code to pull this off. 

So, let's get started, first by creating an XUnit project and referencing our main project into it. (or simply by browsing to [already made project on my GitHub](https://github.com/vbilopav/NormPgIdentity/tree/master/NormPgIdentityTests))

First, we're going to need a bit of configuration to define test server connection string, name of the test database and path to migration scripts. So add `appsettings.json` with this content:

```json
  "DefaultConnection": "Server=localhost;Database=postgres;Port=5432;User Id=postgres;Password=postgres;",
  "TestDatabase": "norm_pg_identity_test",
  "MigrationsPath": "../../../../NormPgIdentity/Migrations"
}
```

And, we can make those values globally available with a little bit of boilerplate:

```csharp
public class Config
{
    public string DefaultConnection { get; set; }
    public string TestDatabase { get; set; }
    public string MigrationsPath { get; set; }
 
    public static Config Value { get; }
 
    static Config()
    {
        Value = new Config();
 
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, false)
            .Build()
            .Bind(Value);
    }
}
```

Now, we will add code for that will initialize and clean up the entire test session. 

For this, (and test fixtures also) - we will use the [collection fixtures definition feature from the XUnit testing system](https://xunit.net/docs/shared-context). 

Code looks like this:

```csharp
public sealed class PostgreSqlFixture : IDisposable
{
    public NpgsqlConnection Connection { get; }

    public PostgreSqlFixture()
    {
        Connection = new NpgsqlConnection(Config.Value.DefaultConnection);
        CreateTestDatabase(Connection);
        Connection.ChangeDatabase(Config.Value.TestDatabase);
        ApplyMigrations(Connection, MigrationDirection.Up);
    }

    public void Dispose()
    {
        ApplyMigrations(Connection, MigrationDirection.Down);
        Connection.Close();
        Connection.Dispose();
        using var connection = new NpgsqlConnection(Config.Value.DefaultConnection);
        DropTestDatabase(connection);
    }

    private static void CreateTestDatabase(NpgsqlConnection connection)
    {
        void DoCreate() => connection.Execute($"create database {Config.Value.TestDatabase}");
        try
        {
            DoCreate();
        }
        catch (PostgresException e) when (e.SqlState == "42P04")  // 42P04=duplicate_database, see https://www.postgresql.org/docs/9.3/errcodes-appendix.html
        {
            DropTestDatabase(connection);
            DoCreate();
        }
    }

    private static void DropTestDatabase(NpgsqlConnection connection) => connection.Execute($@"

        revoke connect on database {Config.Value.TestDatabase} from public;

        select
            pid, pg_terminate_backend(pid)
        from
            pg_stat_activity
        where
            datname = '{Config.Value.TestDatabase}' and pid <> pg_backend_pid();

        drop database {Config.Value.TestDatabase};

        ");

    private static void ApplyMigrations(NpgsqlConnection connection, MigrationDirection direction)
    {
        foreach (var (_, name) in MigrationManager.EnumerateMigrations(direction, Config.Value.MigrationsPath))
        {
            connection.Execute(File.ReadAllText(name));
        }
    }
}

[CollectionDefinition("PostgreSqlDatabase")]
public class DatabaseFixtureCollection : ICollectionFixture<PostgreSqlFixture> { }
```

This creates a collection fixture with all necessary initialization and cleanup that we need - creates and drops database, and makes the connection to test database available.

We can use this fixture to mark tests with the `Collection` attribute to indicate that they are using that collection for their sessions.

Since every XUnit test, by [XUnit convention](https://xunit.net/docs/shared-context), uses constructor and `Dispose` methods for initialization and cleanup - we will create an abstract class that implements desired test behavior and mark it with test collection attribute:

```csharp
[Collection("PostgreSqlDatabase")]
public abstract class PostgreSqlUnitTestFixture : IDisposable
{
    protected NpgsqlConnection Connection { get; }

    protected PostgreSqlUnitTestFixture(PostgreSqlFixture fixture)
    {
        Connection = fixture.Connection.CloneWith(fixture.Connection.ConnectionString);
        Connection
            .Execute("begin")
            .Execute("set constraints all deferred");
    }

    public void Dispose()
    {
        Connection.Execute("rollback");
        Connection.Close();
        Connection.Dispose();
    }
}
```

Now, all we have to do in our unit test is inherit this `PostgreSqlUnitTestFixture` class:

```csharp
public class MyUnitTests : PostgreSqlUnitTestFixture
{
    public MyUnitTests(PostgreSqlFixture fixture) : base(fixture) { }

    [Fact]
    public void Test1()
    {
        //
        // Connection property is available and ready
        // your test here ...
        //
    }
}
```