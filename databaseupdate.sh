#!/bin/bash
echo "=========================================="
echo "Database Update"
echo "=========================================="
cd backend
dotnet ef database update --project ITHunterview.Service --startup-project ITHunterview.WebAPI
