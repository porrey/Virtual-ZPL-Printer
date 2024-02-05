@echo off

REM ***
REM *** Set up the enviroment.
REM ***
CALL "%ProgramFiles%\Microsoft Visual Studio\2022\Preview\VC\Auxiliary\Build\vcvarsx86_amd64.bat"

REM ***
REM *** Get the NuGet publish key.
REM ***
SET /P PUBLISH_KEY=Enter the publish key:

REM
REM Set the publish key
REM
nuget setApiKey %PUBLISH_KEY%

REM ***
REM *** Publish all *.nupkg files.
REM ***
forfiles /P "%CD%" /S /M *.nupkg /C "cmd /c %CD%\nuget.exe push @path -Source https://api.nuget.org/v3/index.json -SkipDuplicate"

REM ***
REM *** Publish all *.snupkg files.
REM ***
forfiles /P "%CD%" /S /M *.snupkg /C "cmd /c %CD%\nuget.exe push @path -Source https://api.nuget.org/v3/index.json -SkipDuplicate"


REM ***
REM *** Pause to view errors.
REM ***
pause