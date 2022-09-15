@echo off
dotnet build src/Limbo.Umbraco.IconServiceFix --configuration Debug /t:rebuild /t:pack -p:PackageOutputPath=c:/nuget