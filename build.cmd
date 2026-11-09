@echo off

dotnet restore Bob --interactive
dotnet pack Bob -c Release --no-restore
