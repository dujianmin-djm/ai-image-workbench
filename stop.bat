@echo off
chcp 65001 >nul 2>&1
echo 正在停止 AI 图片工作台服务...

:: 停止后端 dotnet 进程
taskkill /fi "WINDOWTITLE eq AI-Image Backend*" /f >nul 2>&1
:: 停止前端 node 进程
taskkill /fi "WINDOWTITLE eq AI-Image Frontend*" /f >nul 2>&1

echo 服务已停止。
timeout /t 2 /nobreak >nul
