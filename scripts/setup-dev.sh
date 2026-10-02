#!/usr/bin/env sh
set -eu

db_password=${1:-REDACTED}
jwt_key=$(openssl rand -base64 48 | tr -d '\n')
connection_string="Host=localhost;Port=5433;Database=DBDoisneau;Username=admin;Password=${db_password}"

projects="
RobertDoisneau.WebApi/RobertDoisneau.Login.WebApi/RobertDoisneau.Login.WebApi.csproj
RobertDoisneau.WebApi/RobertDoisneau.Cart.V2.WebApi/RobertDoisneau.Cart.V2.WebApi.csproj
RobertDoisneau.WebApi/RobertDoisneau.WebApi.GalleryAPI/RobertDoisneau.WebApi.GalleryAPI.csproj
"

for project in $projects; do
    dotnet user-secrets init --project "$project" >/dev/null
    dotnet user-secrets set "Jwt:Key" "$jwt_key" --project "$project" >/dev/null
    dotnet user-secrets set "ConnectionStrings:db" "$connection_string" --project "$project" >/dev/null
    printf 'Secrets configured for %s\n' "$project"
done
