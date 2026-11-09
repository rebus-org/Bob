@echo off

set version=%1

if "%version%"=="" (
	echo Please specify the version to build, e.g. "build.cmd 1.2.3"
	exit /b 1
)

dotnet restore Bob --interactive
if errorlevel 1 exit /b %errorlevel%

dotnet pack Bob -c Release --no-restore -p:Version=%version%
if errorlevel 1 exit /b %errorlevel%
