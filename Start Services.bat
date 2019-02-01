@echo off
echo ### VPService batch loop for Windows
echo ###
echo ### This will auto-restart Services in the event of a
echo ### crash. Use CTRL+C or other kill mechanisms
echo ### to exit Services and this batch file.
echo.

rem ### Pass any arguments to this batch file or define
rem ### defaults in the DefaultArgs variable below
set DefaultArgs=

:Loop
set errorlevel=
VPServices.exe %DefaultArgs% %*

IF %errorlevel% NEQ 0 (
    echo Restarting Services...
    goto Loop
)

pause
