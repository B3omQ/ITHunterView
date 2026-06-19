@echo off
mkdir d:\DoAn\ITHunterView\frontend\src\app\(auth)
mkdir d:\DoAn\ITHunterView\frontend\src\app\(candidate)
mkdir d:\DoAn\ITHunterView\frontend\src\app\(recruiter)
mkdir d:\DoAn\ITHunterView\frontend\src\app\(admin)
mkdir d:\DoAn\ITHunterView\frontend\src\app\(staff)
mkdir d:\DoAn\ITHunterView\frontend\src\app\(shared)

move d:\DoAn\ITHunterView\frontend\src\app\login d:\DoAn\ITHunterView\frontend\src\app\(auth)\login
move d:\DoAn\ITHunterView\frontend\src\app\auth\* d:\DoAn\ITHunterView\frontend\src\app\(auth)\
rmdir /s /q d:\DoAn\ITHunterView\frontend\src\app\auth

move d:\DoAn\ITHunterView\frontend\src\app\dashboard\candidate d:\DoAn\ITHunterView\frontend\src\app\(candidate)\dashboard
move d:\DoAn\ITHunterView\frontend\src\app\dashboard\recruiter d:\DoAn\ITHunterView\frontend\src\app\(recruiter)\dashboard
move d:\DoAn\ITHunterView\frontend\src\app\dashboard\admin d:\DoAn\ITHunterView\frontend\src\app\(admin)\dashboard
move d:\DoAn\ITHunterView\frontend\src\app\dashboard\staff d:\DoAn\ITHunterView\frontend\src\app\(staff)\dashboard
move d:\DoAn\ITHunterView\frontend\src\app\dashboard\settings d:\DoAn\ITHunterView\frontend\src\app\(shared)\settings

del d:\DoAn\ITHunterView\frontend\src\app\dashboard\layout.tsx
rmdir /s /q d:\DoAn\ITHunterView\frontend\src\app\dashboard

mkdir d:\DoAn\ITHunterView\frontend\src\components\layout
move d:\DoAn\ITHunterView\frontend\src\components\Sidebar.tsx d:\DoAn\ITHunterView\frontend\src\components\layout\Sidebar.tsx
move d:\DoAn\ITHunterView\frontend\src\components\Logo.tsx d:\DoAn\ITHunterView\frontend\src\components\layout\Logo.tsx

echo Done
