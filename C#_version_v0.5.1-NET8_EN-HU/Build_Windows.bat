@echo off
cd /d "%~dp0"
dotnet publish TDC.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o Windows-NET8-x64
if errorlevel 1 pause
