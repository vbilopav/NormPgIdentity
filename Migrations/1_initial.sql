do $$
declare _version int = 1;
begin

    create table if not exists schema_version (
        version int not null primary key,
        timestamp timestamp with time zone not null default (transaction_timestamp() at time zone 'utc')
    );

    if exists(select version from schema_version where version = _version) then
        raise exception 'migration % is already applied, exiting', _version;
    end if;

    create table role (
        id bigint not null generated always as identity primary key,
        name character varying(256) null,
        normalized_name character varying(256) null,
        concurrency_stamp text null
    );

    create table "user" (
        id bigint not null generated always as identity primary key,
        user_name character varying(256) null,
        normalized_user_name character varying(256) null,
        email character varying(256) null,
        normalized_email character varying(256) null,
        email_confirmed boolean not null,
        password_hash text null,
        security_stamp text null,
        concurrency_stamp text null,
        phone_number text null,
        phone_number_confirmed boolean not null,
        two_factor_enabled boolean not null,
        lockout_end timestamp with time zone null,
        lockout_enabled boolean not null,
        access_failed_count integer not null
    );

    create table role_claim (
        id integer not null generated always as identity primary key,
        role_id bigint not null,
        claim_type text null,
        claim_value text null,
        constraint "FK_role_claim_role_role_id" foreign key (role_id) references role (id) on delete cascade
    );

    create table user_claim (
        id integer not null generated always as identity primary key,
        user_id bigint not null,
        claim_type text null,
        claim_value text null,
        constraint "FK_user_claim_user_user_id" foreign key (user_id) references "user" (id) on delete cascade
    );

    create table user_login (
        login_provider text not null,
        provider_key text not null,
        provider_display_name text null,
        user_id bigint not null,
        constraint "PK_user_login" primary key (login_provider, provider_key),
        constraint "FK_user_login_user_user_id" foreign key (user_id) references "user" (id) on delete cascade
    );

    create table user_role (
        user_id bigint not null,
        role_id bigint not null,
        constraint "PK_user_role" primary key (user_id, role_id),
        constraint "FK_user_role_role_role_id" foreign key (role_id) references role (id) on delete cascade,
        constraint "FK_user_role_user_user_id" foreign key (user_id) references "user" (id) on delete cascade
    );

    create table user_token (
        user_id bigint not null,
        login_provider text not null,
        name text not null,
        value text null,
        constraint "PK_user_token" primary key (user_id, login_provider, name),
        constraint "FK_user_token_user_user_id" foreign key (user_id) references "user" (id) on delete cascade
    );

    create unique index "RoleNameIndex" on role (normalized_name);

    create index "IX_role_claim_role_id" on role_claim (role_id);

    create index "EmailIndex" on "user" (normalized_email);

    create unique index "UserNameIndex" on "user" (normalized_user_name);

    create index "IX_user_claim_user_id" on user_claim (user_id);

    create index "IX_user_login_user_id" on user_login (user_id);

    create index "IX_user_role_role_id" ON user_role (role_id);

    raise info 'applying migration version %', _version;

    insert into schema_version (version) values (_version) on conflict do nothing;

end
$$;