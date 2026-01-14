@echo off
echo Stopping CodeReviewTool applications...

echo Closing ApiGateway window...
taskkill /FI "WINDOWTITLE eq ApiGateway*" /F >nul 2>&1

echo Closing GitAnalysis.Api window...
taskkill /FI "WINDOWTITLE eq GitAnalysis.Api*" /F >nul 2>&1

echo Closing RealtimeNotification.Api window...
taskkill /FI "WINDOWTITLE eq RealtimeNotification.Api*" /F >nul 2>&1

echo Closing Angular window...
taskkill /FI "WINDOWTITLE eq Angular*" /F >nul 2>&1

echo Stopping any remaining dotnet processes for these projects...
for /f "tokens=2" %%a in ('tasklist /FI "IMAGENAME eq dotnet.exe" /FO LIST ^| find "PID:"') do (
    wmic process where "ProcessId=%%a" get CommandLine 2>nul | findstr /i "ApiGateway GitAnalysis.Api RealtimeNotification.Api" >nul && taskkill /PID %%a /F >nul 2>&1
)

echo Stopping Angular dev server (node processes)...
for /f "tokens=2" %%a in ('tasklist /FI "IMAGENAME eq node.exe" /FO LIST ^| find "PID:"') do (
    wmic process where "ProcessId=%%a" get CommandLine 2>nul | findstr /i "CodeReviewTool.Workspace" >nul && taskkill /PID %%a /F >nul 2>&1
)

echo All applications stopped.
