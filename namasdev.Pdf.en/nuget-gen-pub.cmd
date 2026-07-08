@echo off
dotnet build --configuration Release
dotnet pack --configuration Release --no-build
for %%f in (bin\Release\*.nupkg) do (
    powershell -NoProfile -Command "& { $pkg = '%%~nf'; if ($pkg -match '^(.+?)\.(\d+\.\d+\.\d+.*)$') { $id = $matches[1].ToLower(); $ver = $matches[2]; $feed = '\\MATUASUS\nuget\' + $id + '\' + $ver; $cache = $env:USERPROFILE + '\.nuget\packages\' + $id + '\' + $ver; if (Test-Path $feed) { Remove-Item -Recurse -Force $feed; Write-Host ('Removed feed: ' + $feed) }; if (Test-Path $cache) { Remove-Item -Recurse -Force $cache; Write-Host ('Removed cache: ' + $cache) } } }"
    nuget add "%%f" -source \\MATUASUS\nuget
)
