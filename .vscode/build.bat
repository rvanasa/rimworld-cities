echo off

rmdir /S /Q Source\obj
dotnet build Source\RimCities.csproj --configuration Release
