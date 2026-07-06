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
            if (companies.Count < 3)
            {
                var comp1 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "ITHunterView Corp", TaxCode = "0102030405", HeadquartersAddress = "123 Dev Street, Tech City",
                    Industry = "Software Products and Web Services", CompanySize = "100-500", Description = "Leading tech recruitment platform",
                    Website = "https://ithunterview.com", LogoUrl = "https://logo.clearbit.com/ithunterview.com", CompanyType = "IT Product",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, VerificationDocumentUrl = "https://document.com/license1.pdf", Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                var comp2 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "FPT Software", TaxCode = "0102030406", HeadquartersAddress = "F-Town, HCMC",
                    Industry = "Software Development Outsourcing", CompanySize = "1000+", Description = "Global technology and IT services provider",
                    Website = "https://fptsoftware.com", LogoUrl = "https://logo.clearbit.com/fptsoftware.com", CompanyType = "IT Outsourcing",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION, VerificationDocumentUrl = "https://document.com/license2.pdf", Status = CompanyStatus.VERIFIED, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                };
                var comp3 = new Companies
                {
                    Id = Guid.NewGuid(), Name = "VNG Corporation", TaxCode = "0102030407", HeadquartersAddress = "VNG Campus, HCMC",
                    Industry = "Game", CompanySize = "1000+", Description = "Vietnam's leading tech firm",
                    Website = "https://vng.com.vn", LogoUrl = "https://logo.clearbit.com/vng.com.vn", CompanyType = "IT Product",
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
                            IsVisibleToRecruiters = true
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

                        jobs.Add(new JobPostings
                        {
                            Id = jobId,
                            JobCode = $"JB-{random.Next(10000, 99999)}",
                            RecruiterId = recruiter.Id,
                            CompanyId = company.Id,
                            Title = $"{prefix} {category.Name}",
                            Description = $"We are looking for a talented {prefix} {category.Name} to join our dynamic team at {company.Name}. You will be responsible for developing high-quality solutions and working in an agile environment.",
                            Responsibilities = "- Develop high-quality software design and architecture\n- Identify, prioritize and execute tasks in the software development life cycle\n- Review, test and debug code",
                            Requirements = $"- Proven experience as a {category.Name}\n- Experience with software design and development\n- Excellent communication skills",
                            Benefits = "- Competitive salary\n- Health insurance\n- Paid time off\n- Flexible working hours",
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
    }
}
