#!/bin/bash

#build the Tram test database
/opt/mssql-tools/bin/sqlcmd -S $TRAM_DB_SERVER -U sa -P $TRAM_SA_PASSWORD -b -Q "CREATE DATABASE $TRAM_DB" || exit 1
/opt/mssql-tools/bin/sqlcmd -S $TRAM_DB_SERVER -U sa -P $TRAM_SA_PASSWORD -b -Q "ALTER DATABASE $TRAM_DB SET RECOVERY SIMPLE" || exit 1
export TRAM_SCHEMA=$TRAM_SCHEMA
/opt/mssql-tools/bin/sqlcmd -S $TRAM_DB_SERVER -U sa -P $TRAM_SA_PASSWORD -b -d $TRAM_DB -I -i initialize-database.sql || exit 1
export TRAM_SCHEMA=$TRAM_SCHEMA2
/opt/mssql-tools/bin/sqlcmd -S $TRAM_DB_SERVER -U sa -P $TRAM_SA_PASSWORD -b -d $TRAM_DB -I -i initialize-database.sql || exit 1