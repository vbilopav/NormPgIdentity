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