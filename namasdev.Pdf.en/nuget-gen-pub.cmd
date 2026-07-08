@echo off
setlocal

REM Packs this project and publishes it to the public NuGet.org feed.
REM
REM The API key is read from the NUGET_API_KEY environment variable.
REM NEVER hard-code the key in this file -- this repository is public.
REM Create a key at https://www.nuget.org/account/apikeys (scope it to "namasdev.*").

if "%NUGET_API_KEY%"=="" (
    echo ERROR: NUGET_API_KEY is not set.
    echo   set NUGET_API_KEY=your-api-key
    exit /b 1
)

REM Remove stale packages so the push loop below can only ever pick up
REM the version produced by this run. Published versions cannot be deleted.
if exist bin\Release\*.nupkg del /q bin\Release\*.nupkg

dotnet build --configuration Release
if errorlevel 1 exit /b 1

dotnet pack --configuration Release --no-build
if errorlevel 1 exit /b 1

for %%f in (bin\Release\*.nupkg) do (
    echo Pushing %%~nxf ...
    dotnet nuget push "%%f" --source https://api.nuget.org/v3/index.json --api-key %NUGET_API_KEY% --skip-duplicate
    if errorlevel 1 exit /b 1
)

echo Done.
endlocal
