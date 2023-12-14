@echo off

REM ***
REM *** Set up the enviroment.
REM ***
CALL "%ProgramFiles% (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"

REM ***
REM *** Get the NuGet publish key.
REM ***
SET /P PUBLISH_KEY=Enter the publish key:

REM ***
REM *** Publish all *.nupkg files.
REM ***
forfiles /P "%CD%" /S /M *.nupkg /C "cmd /c dotnet nuget push @path -k %PUBLISH_KEY% -s https://api.nuget.org/v3/index.json"

REM ***
REM *** Pause to view errors.
REM ***
pause