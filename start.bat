@echo off
chcp 65001 >nul 2>&1
echo ============================================
echo   AI 图片筛选与评审工作台 - 一键启动
echo ============================================
echo.

:: 检查 dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未检测到 .NET SDK，请先安装 .NET 10 SDK
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

:: 检查 node
where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未检测到 Node.js，请先安装 Node.js 18+
    echo 下载地址: https://nodejs.org/
    pause
    exit /b 1
)

:: 安装前端依赖（如果 node_modules 不存在）
if not exist "frontend\node_modules" (
    echo [1/4] 正在安装前端依赖...
    cd frontend
    call npm install
    cd ..
) else (
    echo [1/4] 前端依赖已就绪
)

:: 还原后端依赖
echo [2/4] 正在还原后端依赖...
cd backend
dotnet restore src\AI.Image.HttpApi.Host\AI.Image.HttpApi.Host.csproj >nul 2>&1
cd ..

:: 应用数据库迁移
echo [3/4] 正在更新数据库...
cd backend
dotnet ef database update --project src\AI.Image.EntityFrameworkCore\AI.Image.EntityFrameworkCore.csproj --startup-project src\AI.Image.HttpApi.Host\AI.Image.HttpApi.Host.csproj >nul 2>&1
cd ..

:: 启动后端（新窗口）
echo [4/4] 正在启动服务...
echo.
start "AI-Image Backend (port 5008)" cmd /k "cd /d %~dp0backend\src\AI.Image.HttpApi.Host && dotnet run --urls http://localhost:5008"

:: 等待后端就绪
echo 等待后端服务启动...
timeout /t 8 /nobreak >nul

:: 启动前端（新窗口）
start "AI-Image Frontend (port 5007)" cmd /k "cd /d %~dp0frontend && npm run dev"

:: 等待前端就绪
timeout /t 4 /nobreak >nul

echo.
echo ============================================
echo   启动完成！
echo   前端地址: http://localhost:5007
echo   后端API:  http://localhost:5008/swagger
echo   按下任意键打开浏览器...
echo ============================================
pause >nul

start http://localhost:5007
