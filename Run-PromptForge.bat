@echo off
setlocal

set "APP=%~dp0src\PromptForge.UI\bin\Debug\net10.0-windows\PromptForge.exe"

if not exist "%APP%" (
    echo PromptForge was not found. Build the solution first:
    echo dotnet build "%~dp0PromptForge.slnx"
    pause
    exit /b 1
)

start "" "%APP%"
exit /b 0
