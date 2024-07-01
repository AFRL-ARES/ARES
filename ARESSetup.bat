cd C:\ARES\AresLib
dotnet nuget add source c:\ARES\AresLib\nuget --name ARESNuget
dotnet nuget update source ARESNuget --source c:\ARES\AresLib\nuget
dotnet build

cd C:\ARES\FC2
dotnet build

dotnet tool install --global dotnet-ef

sqllocaldb create MSSQLLocalDB

cd FC2Service
powershell -file ResetDatabase.ps1
