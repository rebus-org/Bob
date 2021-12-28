@echo off

dotnet publish Bob -c Release -r win-x64 --self-contained -p:PublishSingleFile=true;PublishTrimmed=true;PublishReadyToRun=true