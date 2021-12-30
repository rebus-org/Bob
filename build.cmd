@echo off

dotnet restore Bob --interactive
dotnet publish Bob -c Release --no-restore -r win-x64 --self-contained -p:PublishSingleFile=true;PublishTrimmed=true