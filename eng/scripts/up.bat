@echo off
echo Starting CodeReviewTool applications...

set SRC_DIR=C:\projects\CodeReviewTool\src

echo Starting ApiGateway...
start "ApiGateway" cmd /k "cd /d %SRC_DIR%\ApiGateway && dotnet run"

echo Starting GitAnalysis.Api...
start "GitAnalysis.Api" cmd /k "cd /d %SRC_DIR%\GitAnalysis\src\GitAnalysis.Api && dotnet run"

echo Starting RealtimeNotification.Api...
start "RealtimeNotification.Api" cmd /k "cd /d %SRC_DIR%\RealtimeNotification\src\RealtimeNotification.Api && dotnet run"

echo Starting Angular app...
start "Angular" cmd /k "cd /d %SRC_DIR%\CodeReviewTool.Workspace && npm start"

echo All applications started.
