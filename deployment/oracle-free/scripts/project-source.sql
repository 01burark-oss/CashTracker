\set ON_ERROR_STOP on
\pset tuples_only on
\pset format unaligned
SELECT format('SELECT %L || ''|'' || md5(to_jsonb(projected)::text) FROM (SELECT %s FROM %I.%I) projected;',
 table_name, string_agg(format('%I', column_name), ', ' ORDER BY ordinal_position), table_schema, table_name)
FROM information_schema.columns
WHERE table_schema='public' AND table_name <> '__EFMigrationsHistory'
GROUP BY table_schema,table_name ORDER BY table_name;
