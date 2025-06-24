@echo off
echo Building Server Launcher...

:: Create necessary directories
echo Creating output directories...
if not exist "bin" mkdir bin
if not exist "bin\Release" mkdir bin\Release

:: Copy the icon file to the output directory
echo Copying icon...
copy "C:\Users\stanis\Desktop\ServerStarter\app.ico" "bin\Release\app.ico" /Y

:: Find MSBuild path
set MSBUILD_PATH=
for /D %%i in ("%ProgramFiles(x86)%\MSBuild\*") do (
    if exist "%%i\Bin\MSBuild.exe" (
        set "MSBUILD_PATH=%%i\Bin\MSBuild.exe"
    )
)

if "%MSBUILD_PATH%"=="" (
    for /D %%i in ("%ProgramFiles%\MSBuild\*") do (
        if exist "%%i\Bin\MSBuild.exe" (
            set "MSBUILD_PATH=%%i\Bin\MSBuild.exe"
        )
    )
)

if "%MSBUILD_PATH%"=="" (
    for /D %%i in ("%windir%\Microsoft.NET\Framework\v*") do (
        if exist "%%i\MSBuild.exe" (
            set "MSBUILD_PATH=%%i\MSBuild.exe"
        )
    )
)

if "%MSBUILD_PATH%"=="" (
    echo Could not find MSBuild. Please make sure .NET Framework is installed.
    exit /b 1
)

echo Using MSBuild: %MSBUILD_PATH%

:: Build the project
"%MSBUILD_PATH%" ServerLauncher.sln /p:Configuration=Release /p:Platform="Any CPU"

if %errorlevel% neq 0 (
    echo Build failed with error code %errorlevel%
    exit /b %errorlevel%
)

echo Build completed successfully.
echo The application is located in the bin\Release directory.
echo.
echo Press any key to exit...
pause > nul 