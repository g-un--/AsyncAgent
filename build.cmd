@echo off
cd /d %~dp0
cls

IF NOT EXIST ".\NuGet.exe" (
    powershell "invoke-webrequest https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -outfile NuGet.exe"
)

IF NOT EXIST ".\packages\FAKE\" (
    NuGet.exe "Install" "FAKE" "-OutputDirectory" "packages" "-ExcludeVersion"  
)

"packages\FAKE\tools\Fake.exe" build.fsx %*
