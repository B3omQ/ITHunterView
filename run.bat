@echo off
title Running ITHunterView Project

echo ==========================================
echo Starting Backend (.NET Web API)...
echo ==========================================
:: Mở cửa sổ mới, di chuyển vào thư mục backend và chạy dotnet run
start "Backend - .NET Web API" cmd /k "cd backend\ITHunterview.WebAPI && dotnet run"

echo.
echo ==========================================
echo Starting Frontend (Next.js)...
echo ==========================================
:: Mở cửa sổ mới, di chuyển vào thư mục frontend và chạy npm run dev
start "Frontend - Next.js" cmd /k "cd frontend && npm run dev"

echo.
echo ==========================================
echo Both services are starting up!
echo ==========================================
pause