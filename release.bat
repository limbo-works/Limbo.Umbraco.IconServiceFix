@echo off
dotnet build src/Limbo.Umbraco.IconServiceFix --configuration Release /t:rebuild /t:pack -p:PackageOutputPath=../../releases/nuget