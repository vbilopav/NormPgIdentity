do $$
declare _version int = 2;
begin

    if exists(select version from schema_version where version = _version) then
        raise warning 'migration % is already applied, exiting ...', _version;
        return;
    end if;

    create table random_strings (
        id bigint not null generated always as identity primary key,
        string character varying not null,
        timestamp timestamp with time zone not null default (transaction_timestamp() at time zone 'utc')
    );
    
    insert into random_strings (string)
    select 
        array_to_string(
            array(
                select
                    substr('abcdefghijklmnopqrstuvwxyz', trunc(random() * 26 +(i*0))::integer + 1, 1)
                from
                    generate_series(1, 12)
            ), ''
        ) as string
    from 
        generate_series(1, 1000) i;
    
    raise info 'applying migration version %', _version;

    insert into schema_version (version) values (_version) on conflict do nothing;
    
end
$$ language plpgsql;




