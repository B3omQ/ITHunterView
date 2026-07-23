using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ITHunterviewContext context)
        {
            await SeedRolesAndPermissionsAsync(context);
            await SeedUsersAsync(context);
            await SeedJobCategoriesAsync(context);
            await SeedSkillsAsync(context);
            await SeedMajorsAsync(context);
            await SeedSubscriptionsAsync(context);
            await SeedCoinConfigAsync(context);
            await SeedJobPostingsAsync(context);
            await SeedSfiaSkillsAsync(context);
            await SeedRealisticSpecificJDsAsync(context);
        }

        private static async Task SeedRolesAndPermissionsAsync(ITHunterviewContext context)
        {
            // 1. Seed Roles
            if (!context.Roles.Any())
            {
                var roles = new List<Roles>
                {
                    new Roles { Name = "admin" },
                    new Roles { Name = "staff" },
                    new Roles { Name = "recruiter" },
                    new Roles { Name = "candidate" }
                };
                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
            }

            // 2. Seed Permissions (Matrix)
            if (!context.Permissions.Any())
            {
                var actions = new[] { "create", "read", "update", "delete", "approve", "reject" };
                var resources = new[] { "job", "company", "cv", "user", "application", "interview", "payment", "system_config" };
                var permissionsToAdd = new List<Permissions>();

                foreach (var resource in resources)
                {
                    foreach (var action in actions)
                    {
                        permissionsToAdd.Add(new Permissions { Action = action, Resource = resource });
                    }
                }
                context.Permissions.AddRange(permissionsToAdd);
                await context.SaveChangesAsync();
            }

            // 3. Seed RolePermissions
            if (!context.RolePermissions.Any())
            {
                var adminRole = context.Roles.FirstOrDefault(r => r.Name == "admin");
                var recruiterRole = context.Roles.FirstOrDefault(r => r.Name == "recruiter");
                var candidateRole = context.Roles.FirstOrDefault(r => r.Name == "candidate");

                var allPermissions = context.Permissions.ToList();
                var rolePermissionsToAdd = new List<RolePermissions>();

                // ADMIN: approve, reject, update:system_config, read/*, delete/*
                if (adminRole != null)
                {
                    var adminPerms = allPermissions.Where(p => 
                        p.Action == "approve" || 
                        p.Action == "reject" || 
                        (p.Action == "update" && p.Resource == "system_config") ||
                        p.Action == "read" || 
                        p.Action == "delete").ToList();
                    
                    foreach (var p in adminPerms)
                    {
                        rolePermissionsToAdd.Add(new RolePermissions { RoleId = adminRole.Id, PermissionId = p.Id });
                    }
                }

                // RECRUITER: create:job, update:job, delete:job, read:application, update:application, read:cv
                if (recruiterRole != null)
                {
                    var recruiterPerms = allPermissions.Where(p => 
                        (p.Action == "create" && p.Resource == "job") ||
                        (p.Action == "update" && p.Resource == "job") ||
                        (p.Action == "delete" && p.Resource == "job") ||
                        (p.Action == "read" && p.Resource == "application") ||
                        (p.Action == "update" && p.Resource == "application") ||
                        (p.Action == "read" && p.Resource == "cv")).ToList();

                    foreach (var p in recruiterPerms)
                    {
                        rolePermissionsToAdd.Add(new RolePermissions { RoleId = recruiterRole.Id, PermissionId = p.Id });
                    }
                }

                // CANDIDATE: read:job, create:application, create:cv, delete:cv
                if (candidateRole != null)
                {
                    var candidatePerms = allPermissions.Where(p => 
                        (p.Action == "read" && p.Resource == "job") ||
                        (p.Action == "create" && p.Resource == "application") ||
                        (p.Action == "create" && p.Resource == "cv") ||
                        (p.Action == "delete" && p.Resource == "cv")).ToList();

                    foreach (var p in candidatePerms)
                    {
                        rolePermissionsToAdd.Add(new RolePermissions { RoleId = candidateRole.Id, PermissionId = p.Id });
                    }
                }

                context.RolePermissions.AddRange(rolePermissionsToAdd);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUsersAsync(ITHunterviewContext context)
        {
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "admin");
            var staffRole = context.Roles.FirstOrDefault(r => r.Name == "staff");
            var recruiterRole = context.Roles.FirstOrDefault(r => r.Name == "recruiter");
            var candidateRole = context.Roles.FirstOrDefault(r => r.Name == "candidate");

            var usersToAdd = new List<User>();

            // 1 Admin
            if (!context.Users.Any(u => u.Email == "admin@ithunterview.com"))
            {
                usersToAdd.Add(new User
                {
                    Email = "admin@ithunterview.com",
                    PasswordHash = PasswordHasher.HashPassword("123456"),
                    Status = UserStatus.ACTIVE,
                    RoleId = adminRole?.Id
                });
            }

            // 2 Staff
            for (int i = 1; i <= 2; i++)
            {
                string email = $"staff{i}@ithunterview.com";
                if (!context.Users.Any(u => u.Email == email))
                {
                    usersToAdd.Add(new User
                    {
                        Email = email,
                        PasswordHash = PasswordHasher.HashPassword("123456"),
                        Status = UserStatus.ACTIVE,
                        RoleId = staffRole?.Id
                    });
                }
            }

            // 3 Recruiter
            for (int i = 1; i <= 3; i++)
            {
                string email = $"recruiter{i}@ithunterview.com";
                if (!context.Users.Any(u => u.Email == email))
                {
                    usersToAdd.Add(new User
                    {
                        Email = email,
                        PasswordHash = PasswordHasher.HashPassword("123456"),
                        Status = UserStatus.ACTIVE,
                        RoleId = recruiterRole?.Id
                    });
                }
            }

            // 10 Candidate
            for (int i = 1; i <= 10; i++)
            {
                string email = $"candidate{i}@ithunterview.com";
                if (!context.Users.Any(u => u.Email == email))
                {
                    usersToAdd.Add(new User
                    {
                        Email = email,
                        PasswordHash = PasswordHasher.HashPassword("123456"),
                        Status = UserStatus.ACTIVE,
                        RoleId = candidateRole?.Id
                    });
                }
            }

            if (usersToAdd.Any())
            {
                context.Users.AddRange(usersToAdd);
                await context.SaveChangesAsync();
            }

            // Seed companies if none exists
            var companies = context.Companies.ToList();

            // Update old clearbit logo URLs if they exist in database
            var clearbitCompanies = context.Companies.Where(c => c.LogoUrl.Contains("logo.clearbit.com")).ToList();
            if (clearbitCompanies.Any())
            {
                foreach (var c in clearbitCompanies)
                {
                    if (c.Name.Contains("ITHunterView")) c.LogoUrl = "https://picsum.photos/id/1060/200";
                    else if (c.Name.Contains("FPT")) c.LogoUrl = "https://picsum.photos/id/1061/200";
                    else if (c.Name.Contains("VNG")) c.LogoUrl = "https://picsum.photos/id/1062/200";
                    else c.LogoUrl = "https://picsum.photos/id/1060/200";
                }
                context.SaveChanges();
                companies = context.Companies.ToList(); // Refresh the local company list
            }

            if (companies.Count < 3)
            {
                var comp1 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "ITHunterView Corp", TaxCode = "0102030405", HeadquartersAddress = "123 Dev Street, Tech City",
                    Industry = "Software Products and Web Services", CompanySize = "100-500", Description = "Leading tech recruitment platform",
                    Website = "https://ithunterview.com", LogoUrl = "https://picsum.photos/id/1060/200", CompanyType = "IT Product",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, VerificationDocumentUrl = "https://document.com/license1.pdf", Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                var comp2 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "FPT Software", TaxCode = "0102030406", HeadquartersAddress = "F-Town, HCMC",
                    Industry = "Software Development Outsourcing", CompanySize = "1000+", Description = "Global technology and IT services provider",
                    Website = "https://fptsoftware.com", LogoUrl = "https://picsum.photos/id/1061/200", CompanyType = "IT Outsourcing",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, VerificationDocumentUrl = "https://document.com/license2.pdf", Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                var comp3 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "VNG Corporation", TaxCode = "0102030407", HeadquartersAddress = "VNG Campus, HCMC",
                    Industry = "Game", CompanySize = "1000+", Description = "Vietnam's leading tech firm",
                    Website = "https://vng.com.vn", LogoUrl = "https://picsum.photos/id/1062/200", CompanyType = "IT Product",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, VerificationDocumentUrl = "https://document.com/license3.pdf", Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                
                context.Companies.AddRange(comp1, comp2, comp3);
                await context.SaveChangesAsync();
                companies = new List<Companies> { comp1, comp2, comp3 };
            }

            // Seed profiles for recruiter users if missing
            if (recruiterRole != null)
            {
                // ONLY map profiles for the default seeded recruiters. Do NOT touch user-registered recruiters!
                var seededEmails = new List<string> { "recruiter1@ithunterview.com", "recruiter2@ithunterview.com", "recruiter3@ithunterview.com" };
                var recruiters = context.Users
                    .Where(u => u.RoleId == recruiterRole.Id && seededEmails.Contains(u.Email))
                    .OrderBy(u => u.Email)
                    .ToList();

                for (int i = 0; i < recruiters.Count; i++)
                {
                    var r = recruiters[i];
                    var existingProfile = context.RecruiterProfiles.FirstOrDefault(rp => rp.UserId == r.Id);
                    Companies compToAssign;
                    if (i == 0) compToAssign = companies.FirstOrDefault(c => c.Name.Contains("ITHunterView")) ?? companies.First();
                    else if (i == 1) compToAssign = companies.FirstOrDefault(c => c.Name.Contains("FPT")) ?? companies.First();
                    else if (i == 2) compToAssign = companies.FirstOrDefault(c => c.Name.Contains("VNG")) ?? companies.First();
                    else compToAssign = companies.First();

                    if (existingProfile == null)
                    {
                        context.RecruiterProfiles.Add(new RecruiterProfiles
                        {
                            Id = Guid.NewGuid(),
                            UserId = r.Id,
                            CompanyId = compToAssign.Id,
                            FullName = $"Recruiter {r.Email.Split('@')[0]}",
                            PositionTitle = "HR Manager",
                            Phone = "0987654321",
                            AvatarUrl = $"https://avatar.iran.liara.run/public/{30 + i}"
                        });
                    }
                    else
                    {
                        // Force update CompanyId for existing profiles to ensure separation
                        existingProfile.CompanyId = compToAssign.Id;
                        context.RecruiterProfiles.Update(existingProfile);
                    }
                }
                await context.SaveChangesAsync();
            }

            // Seed MB Bank Company & Recruiter Profile if missing
            var mbCompany = context.Companies.FirstOrDefault(c => c.TaxCode == "0100283873" || c.Name.Contains("MB Bank") || c.Name.Contains("Quân đội"));
            if (mbCompany == null)
            {
                mbCompany = new Companies
                {
                    Id = Guid.NewGuid(),
                    Name = "Ngân hàng TMCP Quân đội (MB Bank)",
                    TaxCode = "0100283873",
                    HeadquartersAddress = "Số 18 Lê Văn Lương, Phường Trung Hòa, Quận Cầu Giấy, Hà Nội",
                    Industry = "Banking & Financial Technology (Fintech)",
                    CompanySize = "1000+",
                    Description = "Ngân hàng Thương mại Cổ phần Quân đội (MB) là một doanh nghiệp trực thuộc Bộ Quốc phòng, tiên phong trong chuyển đổi số và cung cấp các dịch vụ tài chính, ngân hàng số hiện đại hàng đầu Việt Nam.",
                    Website = "https://www.mbbank.com.vn",
                    LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/2/25/Logo_MB_new.png",
                    CompanyType = "IT Product / Banking",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION,
                    VerificationDocumentUrl = "https://www.mbbank.com.vn/license.pdf",
                    Status = CompanyStatus.VERIFIED,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Companies.Add(mbCompany);
                await context.SaveChangesAsync();
            }

            if (recruiterRole != null)
            {
                string mbRecruiterEmail = "recruiter.mbbank@ithunterview.com";
                var mbUser = context.Users.FirstOrDefault(u => u.Email == mbRecruiterEmail);
                if (mbUser == null)
                {
                    mbUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = mbRecruiterEmail,
                        PasswordHash = PasswordHasher.HashPassword("123456"),
                        Status = UserStatus.ACTIVE,
                        RoleId = recruiterRole.Id
                    };
                    context.Users.Add(mbUser);
                    await context.SaveChangesAsync();
                }

                var mbProfile = context.RecruiterProfiles.FirstOrDefault(rp => rp.UserId == mbUser.Id);
                if (mbProfile == null)
                {
                    context.RecruiterProfiles.Add(new RecruiterProfiles
                    {
                        Id = Guid.NewGuid(),
                        UserId = mbUser.Id,
                        CompanyId = mbCompany.Id,
                        FullName = "Tuyển dụng MB Bank",
                        PositionTitle = "Head of Talent Acquisition",
                        Phone = "02437674050",
                        AvatarUrl = "https://avatar.iran.liara.run/public/34"
                    });
                    await context.SaveChangesAsync();
                }
                else if (mbProfile.CompanyId != mbCompany.Id)
                {
                    mbProfile.CompanyId = mbCompany.Id;
                    context.RecruiterProfiles.Update(mbProfile);
                    await context.SaveChangesAsync();
                }
            }

            // Seed profiles for candidate users if missing
            if (candidateRole != null)
            {
                var candidates = context.Users.Where(u => u.RoleId == candidateRole.Id).ToList();
                var avatarBase = new[] { "boy", "girl" };
                int avatarIdx = 1;
                foreach (var c in candidates)
                {
                    if (!context.CandidateProfiles.Any(cp => cp.UserId == c.Id))
                    {
                        var firstName = $"Candidate";
                        var lastName = c.Email.Split('@')[0]; // e.g. "candidate1"
                        context.CandidateProfiles.Add(new CandidateProfiles
                        {
                            Id = Guid.NewGuid(),
                            UserId = c.Id,
                            FirstName = firstName,
                            LastName = lastName,
                            Phone = $"09{avatarIdx:D8}",
                            Location = avatarIdx % 2 == 0 ? "Ho Chi Minh City" : "Hanoi",
                            AboutMe = "Passionate software developer looking for opportunities.",
                            AvatarUrl = $"https://avatar.iran.liara.run/public/{avatarIdx % 50 + 1}",
                            IsVisibleToRecruiters = true,
                            IsProfileComplete = true,
                            ProfileCompletedAt = DateTime.UtcNow
                        });
                        avatarIdx++;
                    }
                }
                await context.SaveChangesAsync();
            }
        }


        private static async Task SeedJobCategoriesAsync(ITHunterviewContext context)
        {
            if (!context.JobCategories.Any())
            {
                // Parent Categories
                var parents = new List<JobCategories>
                {
                    new JobCategories { Name = "Software Development", Slug = "software-development" },
                    new JobCategories { Name = "DevOps & Infrastructure", Slug = "devops-infrastructure" },
                    new JobCategories { Name = "Data & AI", Slug = "data-ai" },
                    new JobCategories { Name = "QA/Testing", Slug = "qa-testing" },
                    new JobCategories { Name = "IT Support", Slug = "it-support" }
                };

                context.JobCategories.AddRange(parents);
                await context.SaveChangesAsync();

                var pSoftware = context.JobCategories.First(c => c.Name == "Software Development").Id;
                var pDevOps = context.JobCategories.First(c => c.Name == "DevOps & Infrastructure").Id;
                var pDataAI = context.JobCategories.First(c => c.Name == "Data & AI").Id;
                var pQA = context.JobCategories.First(c => c.Name == "QA/Testing").Id;
                var pSupport = context.JobCategories.First(c => c.Name == "IT Support").Id;

                var children = new List<JobCategories>
                {
                    // Software Development
                    new JobCategories { Name = "Frontend Development", Slug = "frontend-development", ParentId = pSoftware },
                    new JobCategories { Name = "Backend Development", Slug = "backend-development", ParentId = pSoftware },
                    new JobCategories { Name = "Fullstack Development", Slug = "fullstack-development", ParentId = pSoftware },
                    new JobCategories { Name = "Mobile Development", Slug = "mobile-development", ParentId = pSoftware },
                    new JobCategories { Name = "Embedded & IoT Development", Slug = "embedded-iot-development", ParentId = pSoftware },
                    new JobCategories { Name = "Game Development", Slug = "game-development", ParentId = pSoftware },

                    // DevOps & Infrastructure
                    new JobCategories { Name = "DevOps Engineering", Slug = "devops-engineering", ParentId = pDevOps },
                    new JobCategories { Name = "Cloud Engineering", Slug = "cloud-engineering", ParentId = pDevOps },
                    new JobCategories { Name = "System Administration", Slug = "system-administration", ParentId = pDevOps },
                    new JobCategories { Name = "Database Administration (DBA)", Slug = "database-administration", ParentId = pDevOps },
                    new JobCategories { Name = "Cybersecurity & Security Operations (SecOps)", Slug = "cybersecurity-secops", ParentId = pDevOps },

                    // Data & AI
                    new JobCategories { Name = "Data Engineering", Slug = "data-engineering", ParentId = pDataAI },
                    new JobCategories { Name = "Data Analytics & Business Intelligence (BI)", Slug = "data-analytics-bi", ParentId = pDataAI },
                    new JobCategories { Name = "Data Science", Slug = "data-science", ParentId = pDataAI },
                    new JobCategories { Name = "Machine Learning / Deep Learning Engineering", Slug = "machine-learning", ParentId = pDataAI },
                    new JobCategories { Name = "AI Product / Prompt Engineering", Slug = "ai-product-prompt-engineering", ParentId = pDataAI },

                    // QA/Testing
                    new JobCategories { Name = "Manual Testing", Slug = "manual-testing", ParentId = pQA },
                    new JobCategories { Name = "Automation Testing", Slug = "automation-testing", ParentId = pQA },
                    new JobCategories { Name = "Performance / Security Testing", Slug = "performance-security-testing", ParentId = pQA },

                    // IT Support
                    new JobCategories { Name = "Helpdesk / IT Support", Slug = "helpdesk-it-support", ParentId = pSupport },
                    new JobCategories { Name = "Network Engineering", Slug = "network-engineering", ParentId = pSupport },
                    new JobCategories { Name = "Technical Support (Tier 2/3)", Slug = "technical-support", ParentId = pSupport }
                };

                context.JobCategories.AddRange(children);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSkillsAsync(ITHunterviewContext context)
        {
            if (!context.SkillCategories.Any())
            {
                var categories = new List<SkillCategories>
                {
                    new SkillCategories { Name = "Programming Language" },
                    new SkillCategories { Name = "Framework & Library" },
                    new SkillCategories { Name = "Database" },
                    new SkillCategories { Name = "DevOps & Cloud" },
                    new SkillCategories { Name = "Soft Skill" },
                    new SkillCategories { Name = "Language" },
                    new SkillCategories { Name = "Tool & Design" }
                };
                context.SkillCategories.AddRange(categories);
                await context.SaveChangesAsync();
            }
                
            if (!context.Skills.Any())
            {
                var cProg = context.SkillCategories.First(c => c.Name == "Programming Language").Id;
                var cFram = context.SkillCategories.First(c => c.Name == "Framework & Library").Id;
                var cDb = context.SkillCategories.First(c => c.Name == "Database").Id;
                var cDevOps = context.SkillCategories.First(c => c.Name == "DevOps & Cloud").Id;
                var cSoft = context.SkillCategories.First(c => c.Name == "Soft Skill").Id;
                var cLang = context.SkillCategories.First(c => c.Name == "Language").Id;
                var cTool = context.SkillCategories.First(c => c.Name == "Tool & Design").Id;

                var skills = new List<Skills>
                {
                    // Programming Language
                    new Skills { CategoryId = cProg, Name = "JavaScript", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "TypeScript", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "Python", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "Java", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "Go", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "C#", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cProg, Name = "PHP", Status = SkillStatus.ACTIVE },

                    // Framework & Library
                    new Skills { CategoryId = cFram, Name = "React", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = "Node.js", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = "Spring Boot", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = "Django", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = ".NET", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = "Vue.js", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cFram, Name = "NestJS", Status = SkillStatus.ACTIVE },

                    // Database
                    new Skills { CategoryId = cDb, Name = "PostgreSQL", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDb, Name = "MySQL", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDb, Name = "MongoDB", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDb, Name = "Redis", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDb, Name = "Elasticsearch", Status = SkillStatus.ACTIVE },

                    // DevOps & Cloud
                    new Skills { CategoryId = cDevOps, Name = "Docker", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDevOps, Name = "Kubernetes", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDevOps, Name = "AWS", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDevOps, Name = "GCP", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDevOps, Name = "CI/CD", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cDevOps, Name = "Terraform", Status = SkillStatus.ACTIVE },

                    // Soft Skill
                    new Skills { CategoryId = cSoft, Name = "Communication", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cSoft, Name = "Teamwork", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cSoft, Name = "Problem Solving", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cSoft, Name = "Leadership", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cSoft, Name = "Time Management", Status = SkillStatus.ACTIVE },

                    // Language
                    new Skills { CategoryId = cLang, Name = "English", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cLang, Name = "Japanese", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cLang, Name = "Korean", Status = SkillStatus.ACTIVE },

                    // Tool & Design
                    new Skills { CategoryId = cTool, Name = "Figma", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cTool, Name = "Photoshop", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cTool, Name = "Jira", Status = SkillStatus.ACTIVE },
                    new Skills { CategoryId = cTool, Name = "Git", Status = SkillStatus.ACTIVE }
                };

                foreach (var skill in skills)
                {
                    skill.NormalizedName = StringNormalizationHelper.NormalizeITTerm(skill.Name);
                }

                context.Skills.AddRange(skills);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedMajorsAsync(ITHunterviewContext context)
        {
            // Chỉ truncate và re-seed nếu phát hiện dữ liệu cũ hoặc bảng trống
            bool needsReset = !context.Majors.Any() || context.Majors.Any(m => m.Code == "CS");
            if (needsReset)
            {
                await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE majors RESTART IDENTITY CASCADE;");

                // Cấp 1 (Root Nodes)
                var dev = new Majors { Name = "Software Development", Code = "DEV" };
                var ba = new Majors { Name = "Business Analysis & Product", Code = "BA_PM" };
                var test = new Majors { Name = "Software Testing & QA", Code = "TEST" };

                var lvl1 = new List<Majors> { dev, ba, test };
                foreach (var m in lvl1)
                {
                    m.NormalizedName = StringNormalizationHelper.NormalizeITTerm(m.Name);
                }
                context.Majors.AddRange(lvl1);
                await context.SaveChangesAsync();

                // Cấp 2
                var devWeb = new Majors { Name = "Web Development", Code = "DEV_WEB", ParentId = dev.Id };
                var devMob = new Majors { Name = "Mobile Development", Code = "DEV_MOB", ParentId = dev.Id };
                var devSys = new Majors { Name = "Systems & Embedded Software", Code = "DEV_SYS", ParentId = dev.Id };

                var baAnly = new Majors { Name = "Business Analysis", Code = "BA_ANLY", ParentId = ba.Id };
                var baMgmt = new Majors { Name = "Product & Project Management", Code = "BA_MGMT", ParentId = ba.Id };

                var tstTest = new Majors { Name = "Software Testing", Code = "TST_TEST", ParentId = test.Id };
                var tstQa = new Majors { Name = "Quality Assurance & Process", Code = "TST_QA", ParentId = test.Id };

                var lvl2 = new List<Majors> { devWeb, devMob, devSys, baAnly, baMgmt, tstTest, tstQa };
                foreach (var m in lvl2)
                {
                    m.NormalizedName = StringNormalizationHelper.NormalizeITTerm(m.Name);
                }
                context.Majors.AddRange(lvl2);
                await context.SaveChangesAsync();

                // Cấp 3
                var lvl3 = new List<Majors>
                {
                    // DEV - Web Development
                    new Majors { Name = "Front-end Development", Code = "DEV_WEB_FE", ParentId = devWeb.Id },
                    new Majors { Name = "Back-end Development", Code = "DEV_WEB_BE", ParentId = devWeb.Id },
                    new Majors { Name = "Full-stack Development", Code = "DEV_WEB_FS", ParentId = devWeb.Id },

                    // DEV - Mobile Development
                    new Majors { Name = "iOS Development", Code = "DEV_MOB_IOS", ParentId = devMob.Id },
                    new Majors { Name = "Android Development", Code = "DEV_MOB_AND", ParentId = devMob.Id },
                    new Majors { Name = "Cross-Platform Mobile Development", Code = "DEV_MOB_CP", ParentId = devMob.Id },

                    // DEV - Systems & Embedded Software
                    new Majors { Name = "Embedded Systems & IoT", Code = "DEV_SYS_EMB", ParentId = devSys.Id },
                    new Majors { Name = "Desktop Application Development", Code = "DEV_SYS_DSK", ParentId = devSys.Id },
                    new Majors { Name = "Game Development", Code = "DEV_SYS_GAM", ParentId = devSys.Id },

                    // BA - Business Analysis
                    new Majors { Name = "IT Business Analysis", Code = "BA_ANLY_IT", ParentId = baAnly.Id },
                    new Majors { Name = "Agile/Scrum Business Analysis", Code = "BA_ANLY_AG", ParentId = baAnly.Id },
                    new Majors { Name = "System Analysis", Code = "BA_ANLY_SYS", ParentId = baAnly.Id },

                    // BA - Product & Project Management
                    new Majors { Name = "Product Management", Code = "BA_MGMT_PROD", ParentId = baMgmt.Id },
                    new Majors { Name = "Project Management", Code = "BA_MGMT_PROJ", ParentId = baMgmt.Id },

                    // TEST - Software Testing
                    new Majors { Name = "Manual Testing", Code = "TST_TEST_MAN", ParentId = tstTest.Id },
                    new Majors { Name = "Automation Testing", Code = "TST_TEST_AUT", ParentId = tstTest.Id },
                    new Majors { Name = "Performance & Security Testing", Code = "TST_TEST_PFS", ParentId = tstTest.Id },

                    // TEST - Quality Assurance & Process
                    new Majors { Name = "QA/QC Lead & Management", Code = "TST_QA_LEAD", ParentId = tstQa.Id },
                    new Majors { Name = "Software Quality Assurance", Code = "TST_QA_SQA", ParentId = tstQa.Id }
                };

                foreach (var m in lvl3)
                {
                    m.NormalizedName = StringNormalizationHelper.NormalizeITTerm(m.Name);
                }
                context.Majors.AddRange(lvl3);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSubscriptionsAsync(ITHunterviewContext context)
        {
            if (!context.Subscriptions.Any())
            {
                var subs = new List<Subscriptions>
                {
                    new Subscriptions 
                    { 
                        Name = "Candidate Free", 
                        Price = 0, 
                        DurationDays = 36500, 
                        FeaturesConfig = "{\"role\":\"CANDIDATE\",\"cvMatchLimit\":2,\"mockInterviewLimit\":0,\"cvOptimizeLimit\":0}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Candidate Premium", 
                        Price = 99000, 
                        DurationDays = 30, 
                        FeaturesConfig = "{\"role\":\"CANDIDATE\",\"cvMatchLimit\":30,\"mockInterviewLimit\":10,\"cvOptimizeLimit\":10}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Recruiter Free", 
                        Price = 0, 
                        DurationDays = 36500, 
                        FeaturesConfig = "{\"role\":\"RECRUITER\",\"activeJobPostings\":1,\"activeSourcingLimit\":5,\"highlightedJobs\":0,\"analytics\":false}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Recruiter Premium", 
                        Price = 499000, 
                        DurationDays = 30, 
                        FeaturesConfig = "{\"role\":\"RECRUITER\",\"activeJobPostings\":10,\"activeSourcingLimit\":50,\"highlightedJobs\":3,\"analytics\":true}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Recruiter Enterprise", 
                        Price = 1999000, 
                        DurationDays = 30, 
                        FeaturesConfig = "{\"role\":\"RECRUITER\",\"activeJobPostings\":-1,\"activeSourcingLimit\":-1,\"highlightedJobs\":-1,\"analytics\":true}",
                        Status = SubscriptionStatus.ACTIVE
                    }
                };
                context.Subscriptions.AddRange(subs);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedCoinConfigAsync(ITHunterviewContext context)
        {
            // Seed CoinFeatures
            if (!context.CoinFeatures.Any())
            {
                var features = new List<CoinFeatures>
                {
                    new CoinFeatures { FeatureKey = "CvJdMatching", CoinCost = 2, Description = "So khớp CV-JD AI" },
                    new CoinFeatures
                    {
                        FeatureKey = "MockInterview", CoinCost = 10, Description = "Phỏng vấn thử AI Mock Interview"
                    },
                    new CoinFeatures { FeatureKey = "CvOptimize", CoinCost = 3, Description = "Tối ưu hóa CV AI" }
                };
                context.CoinFeatures.AddRange(features);
                await context.SaveChangesAsync();
            }

            // Seed CoinPackages
            if (!context.CoinPackages.Any())
            {
                var packages = new List<CoinPackages>
                {
                    new CoinPackages
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000020"), Name = "Gói nạp 20 Coin", Coins = 20,
                        Price = 39000, IsActive = true
                    },
                    new CoinPackages
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000050"), Name = "Gói nạp 50 Coin", Coins = 50,
                        Price = 89000, IsActive = true
                    },
                    new CoinPackages
                    {
                        Id = Guid.Parse("00000000-0000-0000-0000-000000000120"), Name = "Gói nạp 120 Coin", Coins = 120,
                        Price = 199000, IsActive = true
                    }
                };
                context.CoinPackages.AddRange(packages);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedJobPostingsAsync(ITHunterviewContext context)
        {
            if (!context.JobPostings.Any())
            {
                var recruiterRole = context.Roles.FirstOrDefault(r => r.Name == "recruiter");
                var recruiters = recruiterRole != null ? context.Users.Where(u => u.RoleId == recruiterRole.Id).ToList() : new List<User>();
                var companies = context.Companies.ToList();
                var categories = context.JobCategories.Where(c => c.ParentId != null).ToList();
                var skills = context.Skills.ToList();
                var majors = context.Majors.Where(m => m.ParentId != null).ToList();

                if (recruiters.Any() && companies.Any() && categories.Any() && skills.Any() && majors.Any())
                {
                    var jobs = new List<JobPostings>();
                    var jobSkills = new List<JobSkillRequirements>();
                    var random = new System.Random();

                    string[] locations = { "Hồ Chí Minh", "Hà Nội", "Đà Nẵng", "Remote" };
                    JobStatus[] statuses = { JobStatus.PUBLISHED, JobStatus.PUBLISHED, JobStatus.PUBLISHED, JobStatus.DRAFT, JobStatus.CLOSED };

                    string[] jobTitlesPrefixes = { "Senior", "Junior", "Middle", "Lead", "Principal", "Fresher", "Internship", "Manager" };
                    string[] workingModels = { "At office", "Remote", "Hybrid" };
                    string[] jobDomains = { 
                        "Blockchain & Web3 Services", "E-commerce", "Education and Training", "Banking",
                        "Game", "IT Services and IT Consulting", "Cyber Security", "Healthcare",
                        "Financial Services", "AI Software & Services", "Software Products and Web Services"
                    };
                    
                    for (int i = 1; i <= 60; i++)
                    {
                        var company = companies[random.Next(companies.Count)];
                        var recruiter = recruiters[random.Next(recruiters.Count)];
                        var category = categories[random.Next(categories.Count)];
                        
                        string prefix = jobTitlesPrefixes[random.Next(jobTitlesPrefixes.Length)];
                        string location = locations[random.Next(locations.Length)];
                        JobStatus status = statuses[random.Next(statuses.Length)];
                        string level = prefix;
                        string workingModel = workingModels[random.Next(workingModels.Length)];
                        string jobDomain = jobDomains[random.Next(jobDomains.Length)];
                        string jobExpertise = majors[random.Next(majors.Count)].Name;
                        
                        decimal minSalary = random.Next(5, 20) * 100;
                        decimal maxSalary = minSalary + random.Next(5, 15) * 100;

                        var jobId = System.Guid.NewGuid();
                        var publishedAt = System.DateTime.UtcNow.AddDays(-random.Next(1, 60));

                        string[] descTemplates = {
                            $"Are you ready to take your career to the next level? We are looking for a highly skilled and passionate {prefix} {category.Name} to join our innovative team at {company.Name}. In this role, you will be at the forefront of technology, building robust solutions that impact millions of users. You will collaborate with cross-functional teams in a fast-paced agile environment, driving technical excellence and product innovation.",
                            $"Join {company.Name} as a {prefix} {category.Name} and become part of a dynamic, forward-thinking organization. We are seeking a talented professional who thrives on solving complex problems and delivering high-quality results. You will have the opportunity to work with cutting-edge tech stacks, shape the technical direction of our projects, and contribute to a culture of continuous learning and growth.",
                            $"{company.Name} is urgently hiring a {prefix} {category.Name} to expand our core engineering team. This is a unique opportunity to work on highly scalable systems and enterprise-level architecture. If you are deeply passionate about technology, enjoy mentoring peers, and want to make a significant impact on our business growth, we want to hear from you!"
                        };

                        string[] respTemplates = {
                            "- Architect, design, and develop scalable software solutions from scratch.\n- Collaborate closely with Product Managers, Designers, and other Engineers to define feature specifications.\n- Write clean, maintainable, and highly efficient code following best practices.\n- Conduct thorough code reviews and provide constructive feedback to peers.\n- Troubleshoot, debug, and optimize application performance in production environments.\n- Participate in daily stand-ups, sprint planning, and retrospective meetings.",
                            "- Lead the technical implementation of key product features and infrastructure improvements.\n- Integrate third-party APIs and services to enhance product functionality.\n- Develop and maintain comprehensive technical documentation.\n- Ensure high code coverage through unit, integration, and end-to-end testing.\n- Monitor system health, investigate bottlenecks, and resolve complex technical debt.\n- Actively contribute to technical architectural decisions and team knowledge sharing.",
                            "- Gather and analyze requirements to translate business needs into technical designs.\n- Build robust and secure APIs to support scalable frontend and mobile applications.\n- Continuously research and implement new technologies to improve development efficiency.\n- Work closely with QA to automate testing and ensure zero-defect releases.\n- Manage cloud deployments and optimize CI/CD pipelines.\n- Mentor junior team members and foster a collaborative engineering culture."
                        };

                        string[] reqTemplates = {
                            $"- 3+ years of proven professional experience as a {category.Name} or similar role.\n- Strong expertise in software engineering principles, design patterns, and data structures.\n- Hands-on experience with modern frameworks, databases (SQL/NoSQL), and RESTful API design.\n- Familiarity with version control systems (Git) and CI/CD workflows.\n- Excellent problem-solving skills and a strong attention to detail.\n- Good communication skills in English, both written and verbal.",
                            $"- Solid academic background in Computer Science, IT, or related fields.\n- Deep understanding of system architecture, microservices, and cloud platforms (AWS/Azure/GCP).\n- Proven track record of delivering high-quality products in an Agile/Scrum environment.\n- Ability to work independently and manage multiple priorities effectively.\n- Experience with performance tuning and security best practices.\n- Strong team player with a proactive, \"can-do\" attitude.",
                            $"- Demonstrated experience in full software development lifecycle (SDLC).\n- Proficiency in writing clean, scalable, and testable code.\n- Experience working with containerization tools like Docker and Kubernetes.\n- Familiarity with monitoring and logging tools (Grafana, ELK stack).\n- Strong analytical mindset to identify and solve complex architectural issues.\n- Willingness to learn new technologies and adapt to a fast-changing startup environment."
                        };

                        string[] benTemplates = {
                            "- Highly competitive base salary with 13th-month bonus and annual performance review.\n- Premium PVI health insurance for you and your family members.\n- 15-18 days of paid annual leave plus additional sick leave.\n- Flexible working hours and hybrid work-from-home policy.\n- State-of-the-art equipment (MacBook Pro/Dell XPS) and ergonomic workspace.\n- Free lunch, snacks, coffee, and weekly happy hours in the office.",
                            "- Attractive compensation package including ESOP (stock options) for key members.\n- Generous budget for professional development, certifications, and tech conferences.\n- Comprehensive healthcare package and regular health check-ups at top hospitals.\n- Dynamic, open, and international working culture with English speaking environment.\n- Regular team-building activities, company trips (domestic and international).\n- Dedicated fitness/gym allowance and employee wellness programs.",
                            "- Sign-on bonus up to $2000 for successful candidates.\n- Full 100% salary during the probation period.\n- Unlimited paid time off (PTO) policy focusing on results over hours.\n- Modern office in the city center with a breathtaking view and relaxation zones.\n- Opportunities for internal mobility and fast-track career progression.\n- Mentorship programs led by industry experts and senior leaders."
                        };

                        jobs.Add(new JobPostings
                        {
                            Id = jobId,
                            JobCode = $"JB-{random.Next(10000, 99999)}",
                            RecruiterId = recruiter.Id,
                            CompanyId = company.Id,
                            Title = $"{prefix} {category.Name}",
                            Description = descTemplates[random.Next(descTemplates.Length)],
                            Responsibilities = respTemplates[random.Next(respTemplates.Length)],
                            Requirements = reqTemplates[random.Next(reqTemplates.Length)],
                            Benefits = benTemplates[random.Next(benTemplates.Length)],
                            MinSalary = minSalary,
                            MaxSalary = maxSalary,
                            Currency = "USD",
                            Location = location,
                            Status = status,
                            Level = level,
                            WorkingModel = workingModel,
                            JobExpertise = jobExpertise,
                            JobDomain = new List<string> { jobDomain },
                            ApplicationCount = random.Next(0, 100),
                            ViewCount = random.Next(100, 5000),
                            PublishedAt = status == JobStatus.PUBLISHED ? publishedAt : null,
                            CreatedAt = publishedAt.AddDays(-random.Next(1, 5)),
                            UpdatedAt = publishedAt
                        });

                        // Seed 3-5 random skills for this job
                        int skillCount = random.Next(3, 6);
                        var shuffledSkills = skills.OrderBy(x => random.Next()).Take(skillCount).ToList();
                        
                        foreach(var skill in shuffledSkills)
                        {
                            jobSkills.Add(new JobSkillRequirements
                            {
                                JobId = jobId,
                                SkillId = skill.Id,
                                IsMandatory = random.Next(100) > 30 // 70% chance to be mandatory
                            });
                        }
                    }

                    context.JobPostings.AddRange(jobs);
                    context.JobSkillRequirements.AddRange(jobSkills);
                    await context.SaveChangesAsync();
                }
            }
        }

        private static async Task SeedRealisticSpecificJDsAsync(ITHunterviewContext context)
        {
            var oldJobs = context.JobPostings.Where(j => j.Title.Contains("RealisticSeed"));
            if (oldJobs.Any())
            {
                context.JobPostings.RemoveRange(oldJobs);
                await context.SaveChangesAsync();
            }

            var recruiterRole = context.Roles.FirstOrDefault(r => r.Name == "recruiter");
            var recruiters = recruiterRole != null ? context.Users.Where(u => u.RoleId == recruiterRole.Id).ToList() : new List<User>();
            var company = context.Companies.FirstOrDefault();
            var allSkills = context.Skills.ToList();
            
            if (recruiters.Any() && company != null)
            {
                var recruiter = recruiters.First();
                var jobs = new List<JobPostings>();
                var jobSkills = new List<JobSkillRequirements>();
                
                var jobData = new[]
                {
                    new { 
                        Title = "Junior QA/Tester (RealisticSeedV2)", Level = "Junior", Cat = "QA/Testing", Exp = "Software Engineering", Min = 500m, Max = 800m, 
                        Desc = "Are you a detail-oriented fresher or junior tester looking for a great start? Join our dynamic team to perform manual and basic automated testing on web and mobile applications. You will work closely with developers to ensure the highest quality of our products before release.", 
                        Resp = "- Review and analyze system specifications to understand testing requirements.\n- Execute test cases (manual or automated) and analyze results.\n- Report bugs and errors to development teams using Jira.\n- Help troubleshoot issues and conduct post-release testing.\n- Work with cross-functional teams to ensure quality throughout the software development lifecycle.",
                        Req = "- Basic knowledge of software QA methodologies, tools, and processes.\n- Familiarity with Agile frameworks and regression testing.\n- Hands-on experience with bug tracking tools like Jira.\n- Good attention to detail and strong analytical skills.\n- Eagerness to learn automation testing in the future.",
                        Ben = "- 13th month salary and performance bonus.\n- Comprehensive health insurance package.\n- Training opportunities and career path development.\n- Regular team building activities and company trips.",
                        SkillNames = new[] { "Teamwork", "Communication", "Python", "JavaScript" }
                    },
                    new { 
                        Title = "Middle Automation Tester (RealisticSeedV2)", Level = "Middle", Cat = "QA/Testing", Exp = "Software Engineering", Min = 1000m, Max = 1500m, 
                        Desc = "We are seeking a Middle Automation Tester with proven experience in Selenium/Cypress to build, maintain, and scale our automation frameworks. You will play a crucial role in reducing manual testing efforts and ensuring continuous delivery of our enterprise solutions.", 
                        Resp = "- Design, develop, and execute automated test scripts using Selenium or Cypress.\n- Integrate automated tests into CI/CD pipelines (Jenkins/GitLab).\n- Perform API testing using Postman or RestAssured.\n- Collaborate with developers to identify system requirements and test coverage.\n- Analyze test results and provide detailed test reports.",
                        Req = "- 2-4 years of experience in software testing with a strong focus on automation.\n- Proficiency in test automation tools such as Selenium, Cypress, or Appium.\n- Strong knowledge of Java, Python, or JavaScript for scripting.\n- Experience with CI/CD tools and API testing.\n- Solid understanding of Agile/Scrum methodologies.",
                        Ben = "- Highly competitive salary and sign-on bonus.\n- Premium healthcare for employees and family members.\n- Flexible hybrid working model (3 days at office, 2 days remote).\n- MacBook Pro provided for work.",
                        SkillNames = new[] { "Python", "JavaScript", "Java", "CI/CD" }
                    },
                    new { 
                        Title = "Senior QA Engineer (RealisticSeedV2)", Level = "Senior", Cat = "QA/Testing", Exp = "Software Engineering", Min = 1800m, Max = 2500m, 
                        Desc = "As a Senior QA Engineer, you will define the testing strategy, lead the QA process, and mentor junior members. You will be responsible for building robust automation frameworks from scratch and ensuring the highest standards of software quality.", 
                        Resp = "- Architect and build scalable test automation frameworks from scratch.\n- Define and implement overall QA strategies and testing processes.\n- Lead performance and security testing initiatives.\n- Mentor and guide junior and mid-level QA team members.\n- Work closely with DevOps to optimize the CI/CD pipeline for automated testing.",
                        Req = "- 5+ years of experience in Software Quality Assurance and Test Automation.\n- Extensive experience in building automation frameworks (Selenium, Playwright, Cypress).\n- Strong leadership and team management skills.\n- Deep understanding of performance testing tools (JMeter, Gatling).\n- Excellent problem-solving skills and ability to work under pressure.",
                        Ben = "- Top-tier salary package with stock options.\n- 15 days of annual leave plus extra sick leave days.\n- Premium health insurance (Bao Viet/PVI) covering family.\n- Generous budget for personal development and certifications.",
                        SkillNames = new[] { "Python", "Java", "CI/CD", "Docker", "Kubernetes", "AWS" }
                    },
                    new { 
                        Title = "Junior Fullstack Developer (React/NodeJS) (RealisticSeedV2)", Level = "Junior", Cat = "Software Development", Exp = "Computer Science", Min = 600m, Max = 1000m, 
                        Desc = "Exciting opportunity for a Junior Fullstack Developer to work on cutting-edge enterprise products. You will have the chance to work with modern technologies like React, Node.js, and MongoDB in a highly collaborative environment.", 
                        Resp = "- Develop user-facing features using React.js.\n- Build and maintain RESTful APIs using Node.js and Express.\n- Collaborate with designers to implement UI/UX designs.\n- Write clean, maintainable, and well-documented code.\n- Participate in code reviews and team meetings.",
                        Req = "- 6 months to 1.5 years of practical experience with JavaScript/TypeScript.\n- Solid understanding of React.js and its core principles.\n- Familiarity with Node.js and basic backend development.\n- Basic knowledge of Git version control.\n- Passion for coding and willingness to learn new technologies.",
                        Ben = "- Mentorship from Senior Developers.\n- 13th month salary and project bonuses.\n- Weekly tech talks and free courses.\n- Free lunch and snacks in the office.",
                        SkillNames = new[] { "JavaScript", "TypeScript", "React", "Node.js", "MongoDB" }
                    },
                    new { 
                        Title = "Middle Backend Developer (NodeJS/NestJS) (RealisticSeedV2)", Level = "Middle", Cat = "Software Development", Exp = "Computer Science", Min = 1200m, Max = 1800m, 
                        Desc = "Looking for an experienced Middle Backend Developer. You will design, build, and maintain scalable APIs using NestJS and PostgreSQL to support our fast-growing user base. You will deal with complex system architectures and microservices.", 
                        Resp = "- Architect and develop scalable backend services using Node.js and NestJS.\n- Design and optimize database schemas in PostgreSQL.\n- Integrate third-party services and APIs.\n- Identify and resolve performance bottlenecks.\n- Write unit and integration tests to ensure code quality.",
                        Req = "- 3+ years of experience in backend development.\n- Strong proficiency in JavaScript/TypeScript and Node.js.\n- Hands-on experience with NestJS framework and PostgreSQL.\n- Solid understanding of RESTful APIs and microservices architecture.\n- Familiarity with Docker and basic CI/CD processes.",
                        Ben = "- Competitive salary reviewed twice a year.\n- Hybrid working environment (Work from home 2 days/week).\n- Premium healthcare insurance.\n- Gym/Fitness allowance.",
                        SkillNames = new[] { "JavaScript", "TypeScript", "Node.js", "NestJS", "PostgreSQL", "Docker" }
                    },
                    new { 
                        Title = "Senior Frontend Developer (ReactJS) (RealisticSeedV2)", Level = "Senior", Cat = "Software Development", Exp = "Computer Science", Min = 2000m, Max = 3000m, 
                        Desc = "Join us as a Senior Frontend Developer. You will architect frontend solutions, optimize performance, and collaborate with UX/UI teams to deliver world-class web applications. You will also lead frontend initiatives and mentor other developers.", 
                        Resp = "- Architect and develop complex frontend applications using React.js and Next.js.\n- Optimize application performance for maximum speed and scalability.\n- Define frontend coding standards and best practices.\n- Mentor mid-level and junior developers in the team.\n- Collaborate closely with product managers and backend engineers.",
                        Req = "- 5+ years of experience in frontend development.\n- Expert-level knowledge of React.js, Next.js, and TypeScript.\n- Deep understanding of web performance optimization and browser rendering behavior.\n- Experience with modern state management tools (Redux Toolkit, Zustand).\n- Excellent communication and leadership skills.",
                        Ben = "- Attractive salary package with sign-on bonus up to $2000.\n- Stock options for senior positions.\n- Full 100% salary during probation.\n- Unlimited paid time off policy.",
                        SkillNames = new[] { "JavaScript", "TypeScript", "React", "CI/CD", "Communication", "Teamwork" }
                    },
                    new { 
                        Title = "Junior Business Analyst (RealisticSeedV2)", Level = "Junior", Cat = "Data & AI", Exp = "Information Systems", Min = 500m, Max = 900m, 
                        Desc = "Great opportunity for a Junior BA. You will act as the bridge between stakeholders and the development team. You will gather requirements, write user stories, and ensure the delivered product meets business needs.", 
                        Resp = "- Assist in gathering and analyzing business requirements from clients.\n- Write user stories and acceptance criteria.\n- Create basic wireframes and process flow diagrams.\n- Support the testing team in UAT (User Acceptance Testing).\n- Maintain project documentation on Confluence.",
                        Req = "- Degree in Information Systems, IT, or Business Administration.\n- Basic understanding of software development lifecycle (SDLC) and Agile/Scrum.\n- Excellent written and verbal communication skills (English & Vietnamese).\n- Strong analytical and problem-solving mindset.\n- Familiarity with tools like Jira, Trello, or Figma is a plus.",
                        Ben = "- Comprehensive training program for freshers/juniors.\n- 13th month salary + performance review every 6 months.\n- Friendly, dynamic, and supportive environment.\n- Company trips and team-building events.",
                        SkillNames = new[] { "Communication", "Teamwork" }
                    },
                    new { 
                        Title = "Middle IT Business Analyst (RealisticSeedV2)", Level = "Middle", Cat = "Data & AI", Exp = "Information Systems", Min = 1200m, Max = 1800m, 
                        Desc = "We need a Middle IT BA to work on complex enterprise software solutions. You will be responsible for defining system requirements, managing product backlogs, and ensuring smooth communication between business and IT.", 
                        Resp = "- Elicit, analyze, and document complex business requirements.\n- Translate business needs into technical specifications and user stories.\n- Model business processes using BPMN or UML.\n- Manage and prioritize the product backlog in Jira.\n- Facilitate Scrum ceremonies and stakeholder meetings.",
                        Req = "- 2-4 years of experience as an IT Business Analyst.\n- Solid experience working in Agile/Scrum environments.\n- Proficiency in drawing flowcharts, wireframes, and sequence diagrams.\n- Strong English communication skills (IELTS 6.0+ or equivalent).\n- Experience with API documentation and SQL is a strong plus.",
                        Ben = "- High market-rate salary and project success bonuses.\n- Flexible working hours and hybrid remote options.\n- Premium Bao Viet Health Insurance.\n- Annual health check-up at premium hospitals.",
                        SkillNames = new[] { "Communication", "Teamwork", "PostgreSQL", "MySQL" }
                    },
                    new { 
                        Title = "Senior Business Analyst (RealisticSeedV2)", Level = "Senior", Cat = "Data & AI", Exp = "Information Systems", Min = 2000m, Max = 3000m, 
                        Desc = "As a Senior BA, you will lead requirement analysis for large-scale digital transformation projects, consult enterprise clients, and drive the overall product roadmap. You will act as a key advisor to both clients and internal tech teams.", 
                        Resp = "- Lead the business analysis phase for large-scale, enterprise-level projects.\n- Consult clients on digital transformation strategies and optimal system solutions.\n- Define product vision, roadmap, and MVP scope.\n- Mentor and manage a team of junior and mid-level BAs.\n- Resolve complex functional issues and conflicts between stakeholders.",
                        Req = "- 5+ years of experience as a Business Analyst or Product Owner.\n- Experience working directly with enterprise clients (B2B) or international clients.\n- Deep domain knowledge in Finance, Banking, or E-commerce.\n- Exceptional negotiation, presentation, and leadership skills.\n- Advanced SQL skills and understanding of system architecture.",
                        Ben = "- Executive salary package and performance-based equity.\n- Dedicated budget for global conferences and training.\n- Fully sponsored health insurance for family.\n- 20 days of paid annual leave.",
                        SkillNames = new[] { "Communication", "Teamwork", "PostgreSQL", "AWS" }
                    }
                };

                foreach (var data in jobData)
                {
                    var jobId = System.Guid.NewGuid();
                    jobs.Add(new JobPostings
                    {
                        Id = jobId,
                        JobCode = $"JB-REAL-{System.Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                        RecruiterId = recruiter.Id,
                        CompanyId = company.Id,
                        Title = data.Title,
                        Description = data.Desc,
                        Responsibilities = data.Resp,
                        Requirements = data.Req,
                        Benefits = data.Ben,
                        MinSalary = data.Min,
                        MaxSalary = data.Max,
                        Currency = "USD",
                        Location = "Ho Chi Minh",
                        Status = JobStatus.PUBLISHED,
                        Level = data.Level,
                        WorkingModel = "Hybrid",
                        JobExpertise = data.Exp,
                        JobDomain = new List<string> { "IT Services" },
                        ApplicationCount = 0,
                        ViewCount = 10,
                        PublishedAt = System.DateTime.UtcNow,
                        CreatedAt = System.DateTime.UtcNow,
                        UpdatedAt = System.DateTime.UtcNow
                    });

                    // Match specific skills
                    foreach (var skillName in data.SkillNames)
                    {
                        var matchedSkill = allSkills.FirstOrDefault(s => s.Name.Equals(skillName, System.StringComparison.OrdinalIgnoreCase));
                        if (matchedSkill != null)
                        {
                            jobSkills.Add(new JobSkillRequirements
                            {
                                JobId = jobId,
                                SkillId = matchedSkill.Id,
                                IsMandatory = true
                            });
                        }
                    }
                }
                
                context.JobPostings.AddRange(jobs);
                context.JobSkillRequirements.AddRange(jobSkills);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedSfiaSkillsAsync(ITHunterviewContext context)
        {
            if (!context.SfiaSkills.Any())
            {
                var skills = new List<SfiaSkill>
                {
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "PROG", SkillName = "Programming/Software Development", Category = "Development and Implementation", Subcategory = "Systems development" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "SWDN", SkillName = "Software Design", Category = "Development and Implementation", Subcategory = "Systems development" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "DESN", SkillName = "Systems Design", Category = "Development and Implementation", Subcategory = "Systems development" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "ARCH", SkillName = "Solution Architecture", Category = "Strategy and architecture", Subcategory = "Advice/guidance" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "SINT", SkillName = "Systems Integration and Build", Category = "Development and Implementation", Subcategory = "Systems development" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "TEST", SkillName = "Testing", Category = "Development and Implementation", Subcategory = "Systems development" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "SCTY", SkillName = "Information Security", Category = "Strategy and architecture", Subcategory = "Security/privacy" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "DATS", SkillName = "Data Science", Category = "Development and Implementation", Subcategory = "Data/analytics" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "MLNG", SkillName = "Machine Learning", Category = "Development and Implementation", Subcategory = "Data/analytics" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "DBAD", SkillName = "Database Administration", Category = "Delivery and operation", Subcategory = "Data/records operations" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "RELM", SkillName = "Release Management", Category = "Delivery and operation", Subcategory = "Technology management" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "DEPL", SkillName = "Systems Installation/Decommissioning", Category = "Delivery and operation", Subcategory = "Technology management" },
                    new SfiaSkill { Id = System.Guid.NewGuid(), SkillCode = "HCEV", SkillName = "Human-centred evaluation", Category = "Development and Implementation", Subcategory = "User centred design" }
                };

                context.SfiaSkills.AddRange(skills);
                await context.SaveChangesAsync();
            }

        }
    }
}
