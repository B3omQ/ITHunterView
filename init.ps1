# init.ps1 - Khoi dong va xac minh moi truong ITHunterview
# Agent chay script nay dau moi phien
# Cach chay: .\init.ps1

Write-Host "=== Khoi dong moi truong ITHunterview ===" -ForegroundColor Cyan

# 1. Xac nhan vi tri
Write-Host "Thu muc hien tai: $(Get-Location)"

# 2. Kiem tra runtime versions
Write-Host "Kiem tra runtime..."
try {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "  .NET: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  X .NET khong tim thay!" -ForegroundColor Red
}

try {
    $nodeVersion = node --version 2>&1
    Write-Host "  Node: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "  X Node.js khong tim thay!" -ForegroundColor Red
}

# 3. Kiem tra moi truong frontend
Write-Host "Kiem tra frontend..."
if (Test-Path "frontend/.env.local") {
    Write-Host "  [OK] frontend/.env.local ton tai" -ForegroundColor Green
} else {
    Write-Host "  [WARN] frontend/.env.local KHONG ton tai - can tao tu .env.example" -ForegroundColor Yellow
}

if (Test-Path "frontend/node_modules") {
    Write-Host "  [OK] node_modules da cai" -ForegroundColor Green
} else {
    Write-Host "  [WARN] node_modules chua co - chay: cd frontend && npm install" -ForegroundColor Yellow
}

# 4. In trang thai tien do
Write-Host ("`n" + "="*50) -ForegroundColor Cyan
Write-Host "TRANG THAI TIEN DO (claude-progress.md):" -ForegroundColor Yellow

if (Test-Path "claude-progress.md") {
    $content = Get-Content "claude-progress.md" -Raw
    $startIdx = $content.IndexOf("## Buoc tiep theo")
    if ($startIdx -lt 0) {
        $startIdx = $content.IndexOf("## B") # fallback
    }
    if ($startIdx -ge 0) {
        $endIdx = $content.IndexOf("---", $startIdx + 1)
        $nextSteps = if ($endIdx -gt $startIdx) {
            $content.Substring($startIdx, $endIdx - $startIdx)
        } else {
            $content.Substring($startIdx, [Math]::Min(500, $content.Length - $startIdx))
        }
        Write-Host $nextSteps -ForegroundColor White
    } else {
        Write-Host "  [WARN] Khong tim thay phan Buoc tiep theo trong claude-progress.md" -ForegroundColor Yellow
    }
} else {
    Write-Host "  X claude-progress.md khong ton tai!" -ForegroundColor Red
}

# 5. In tinh nang dang lam / uu tien cao nhat
Write-Host "TINH NANG CAN LAM (feature_list.json):" -ForegroundColor Yellow

if (Test-Path "feature_list.json") {
    try {
        $features = Get-Content "feature_list.json" | ConvertFrom-Json
        $inProgress = $features.features | Where-Object { $_.status -eq "in_progress" }
        $pending = $features.features | Where-Object { $_.status -eq "pending" -and $_.priority -eq "high" }

        if ($inProgress) {
            Write-Host "  [In Progress] Dang lam:" -ForegroundColor Blue
            foreach ($f in $inProgress) {
                Write-Host "     [$($f.id)] $($f.name)" -ForegroundColor White
            }
        }

        if ($pending) {
            Write-Host "  [Pending] Uu tien cao:" -ForegroundColor Gray
            foreach ($f in ($pending | Select-Object -First 3)) {
                Write-Host "     [$($f.id)] $($f.name)" -ForegroundColor White
            }
        }
    } catch {
        Write-Host "  [WARN] Khong doc duoc feature_list.json" -ForegroundColor Yellow
    }
}

Write-Host ("`n" + "="*50) -ForegroundColor Cyan
Write-Host "Goi y lenh xac minh:" -ForegroundColor Yellow
Write-Host "   Backend:  cd backend; dotnet build ITHunterview_V1.slnx"
Write-Host "   Frontend: cd frontend; npm run build"
Write-Host "Files quan trong:" -ForegroundColor Yellow
Write-Host "   Luat tong the:   AGENTS.md"
Write-Host "   Luat Frontend:   frontend/kinh-mantra.md + .agents/Frontend/DESIGN.md"
Write-Host "   Luat Backend:    .agents/Backend/CODING_RULES.md"
Write-Host "   Dependency map:  system_registry.json -> dependency_map"
Write-Host ""
Write-Host "=== Moi truong san sang ===" -ForegroundColor Cyan
