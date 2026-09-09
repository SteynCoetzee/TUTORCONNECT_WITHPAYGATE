@echo off
echo Starting ngrok tunnel for PayFast...
echo Static domain: https://underpaid-saint-curled.ngrok-free.dev
echo Forwarding to: http://localhost:5149
echo.
echo NOTE: This tunnel only receives real PayFast payment callbacks if it is run
echo from the machine whose ngrok account owns this static domain. Anyone else
echo running this script needs their own ngrok account + domain, and PayFast
echo would still need to be pointed at THAT domain to actually notify them.
echo Everything else in the app works fine without this tunnel running.
echo.
echo Keep this window open while testing PayFast payments.
echo Press Ctrl+C to stop the tunnel.
echo.

set NGROK_EXE=

where ngrok >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set NGROK_EXE=ngrok
) else (
    for /f "delims=" %%F in ('dir /s /b "%LOCALAPPDATA%\Microsoft\WinGet\Packages\ngrok.exe" 2^>nul') do set NGROK_EXE=%%F
)

if "%NGROK_EXE%"=="" (
    echo Could not find ngrok.exe on this PC.
    echo Install it from https://ngrok.com/download, or make sure it's on your PATH,
    echo then run this script again.
    pause
    exit /b 1
)

"%NGROK_EXE%" http --domain=underpaid-saint-curled.ngrok-free.dev 5149 --log=stdout
pause
