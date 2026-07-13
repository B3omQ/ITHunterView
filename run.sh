#!/bin/bash

# Stop all background jobs when the script exits
trap 'kill 0' SIGINT

echo "=========================================="
echo "Starting Backend (.NET Web API)..."
echo "=========================================="
(cd backend/ITHunterview.WebAPI && dotnet run) &

echo ""
echo "=========================================="
echo "Starting Frontend (Next.js)..."
echo "=========================================="
(cd frontend && npm install --force && npm run dev) &

echo ""
echo "=========================================="
echo "Both services are starting up! Press Ctrl+C to stop."
echo "=========================================="

# Wait for background jobs to finish
wait
