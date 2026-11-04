@echo off
setlocal

set "APP=%~dp0src\PromptCore.UI\bin\Debug\net10.0-windows\PromptCore.exe"

if not exist "%APP%" (
    echo PromptCore was not found. Build the solution first:
    echo dotnet build "%~dp0PromptCore.slnx"
    pause
    exit /b 1
)

start "" "%APP%"
exit /b 0
