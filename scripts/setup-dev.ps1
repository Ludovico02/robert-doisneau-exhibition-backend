# Usage (repo root): .\scripts\setup-dev.ps1 -DbPassword "change-me"
param([string]$DbPassword = "change-me")

$ErrorActionPreference = "Stop"
$projects = @(
    "RobertDoisneau.WebApi\RobertDoisneau.Login.WebApi\RobertDoisneau.Login.WebApi.csproj",
    "RobertDoisneau.WebApi\RobertDoisneau.Cart.V2.WebApi\RobertDoisneau.Cart.V2.WebApi.csproj",
    "RobertDoisneau.WebApi\RobertDoisneau.WebApi.GalleryAPI\RobertDoisneau.WebApi.GalleryAPI.csproj"
)

$bytes = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
$connectionString = "Host=localhost;Port=5433;Database=DBDoisneau;Username=admin;Password=$DbPassword"

foreach ($project in $projects) {
    dotnet user-secrets init --project $project | Out-Null
    dotnet user-secrets set "Jwt:Key" $jwtKey --project $project | Out-Null
    dotnet user-secrets set "ConnectionStrings:db" $connectionString --project $project | Out-Null
    Write-Host "Secrets configured for $project"
}
