do $$
declare _version int = 2;
begin

    if not exists(select version from schema_version where version = _version) then
        raise exception 'migration % is already removed, exiting ...', _version;
    end if;

    drop table random_strings;
        
    raise info 'removing migration version %', _version;

    delete from schema_version where version = _version;
    
end
$$ language plpgsql;
