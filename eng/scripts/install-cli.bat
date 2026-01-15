@echo off
REM Copyright (c) Quinntyne Brown. All Rights Reserved.
REM Licensed under the MIT License. See License.txt in the project root for license information.

echo ========================================
echo CodeReviewTool CLI Installation Script
echo ========================================
echo.

REM Get the script directory and navigate to project root
set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%..\.."

REM Set project path
set PROJECT_PATH=src\CodeReviewTool.Cli\CodeReviewTool.Cli.csproj

echo Checking if project exists...
if not exist "%PROJECT_PATH%" (
    echo ERROR: Project file not found at %PROJECT_PATH%
    exit /b 1
)

echo.
echo Step 1: Uninstalling existing tool (if any)...
dotnet tool uninstall --global CodeReviewTool.Cli 2>nul
if %errorlevel% equ 0 (
    echo   - Previous version uninstalled successfully
) else (
    echo   - No previous version found
)

echo.
echo Step 2: Building project...
dotnet build "%PROJECT_PATH%" --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    exit /b 1
)
echo   - Build completed successfully

echo.
echo Step 3: Packing tool...
dotnet pack "%PROJECT_PATH%" --configuration Release --output ".\nupkg"
if %errorlevel% neq 0 (
    echo ERROR: Pack failed!
    exit /b 1
)
echo   - Pack completed successfully

echo.
echo Step 4: Installing tool globally...
dotnet tool install --global CodeReviewTool.Cli --add-source ".\nupkg"
if %errorlevel% neq 0 (
    echo ERROR: Installation failed!
    exit /b 1
)

echo.
echo ========================================
echo Installation completed successfully!
echo ========================================
echo.
echo You can now use the 'crt' command from anywhere.
echo.
echo Usage examples:
echo   crt --help
echo   crt -r C:\path\to\repo
echo   crt -f feature-branch -i main -d
echo.
echo To uninstall:
echo   dotnet tool uninstall --global CodeReviewTool.Cli
echo.

pause
