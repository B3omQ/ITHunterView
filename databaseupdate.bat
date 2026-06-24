@echo off
title Running Migration

echo ==========================================
echo Database Update
echo ==========================================

:: Mở cửa sổ mới, di chuyển vào thư mục backend và chạy update database
start "Backend - .NET Web API" cmd /k "cd backend && dotnet ef database update --project ITHunterview.Service --startup-project ITHunterview.WebAPI"