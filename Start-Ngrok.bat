@echo off
echo Starting ngrok tunnel for PayFast...
echo Static domain: https://underpaid-saint-curled.ngrok-free.dev
echo Forwarding to: http://localhost:5149
echo.
echo Keep this window open while testing PayFast payments.
echo Press Ctrl+C to stop the tunnel.
echo.
"%LOCALAPPDATA%\Microsoft\WinGet\Packages\Ngrok.Ngrok_Microsoft.Winget.Source_8wekyb3d8bbwe\ngrok.exe" http --domain=underpaid-saint-curled.ngrok-free.dev 5149 --log=stdout
pause
