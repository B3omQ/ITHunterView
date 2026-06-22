using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;

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

            // Seed default company if none exists
            var company = context.Companies.FirstOrDefault();
            if (company == null)
            {
                company = new Companies
                {
                    Id = Guid.NewGuid(),
                    Name = "ITHunterView Corp",
                    TaxCode = "0102030405",
                    HeadquartersAddress = "123 Dev Street, Tech City",
                    Industry = "Information Technology",
                    CompanySize = "100-500",
                    Description = "Leading tech recruitment platform",
                    Website = "https://ithunterview.com",
                    LogoUrl = "https://logo.clearbit.com/ithunterview.com",
                    VerificationMethod = CompanyVerificationMethod.BUSINESS_REGISTRATION,
                    VerificationDocumentUrl = "https://document.com/license.pdf",
                    Status = CompanyStatus.VERIFIED,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Companies.Add(company);
                await context.SaveChangesAsync();
            }

            // Seed profiles for recruiter users if missing
            if (recruiterRole != null)
            {
                var recruiters = context.Users.Where(u => u.RoleId == recruiterRole.Id).ToList();
                foreach (var r in recruiters)
                {
                    if (!context.RecruiterProfiles.Any(rp => rp.UserId == r.Id))
                    {
                        context.RecruiterProfiles.Add(new RecruiterProfiles
                        {
                            Id = Guid.NewGuid(),
                            UserId = r.Id,
                            CompanyId = company.Id,
                            FullName = $"Recruiter {r.Email.Split('@')[0]}",
                            PositionTitle = "HR Manager",
                            Phone = "0987654321",
                            AvatarUrl = "https://avatar.iran.liara.run/public/30"
                        });
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
                            Location = "Ho Chi Minh City",
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

                context.Skills.AddRange(skills);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedMajorsAsync(ITHunterviewContext context)
        {
            if (!context.Majors.Any())
            {
                var majors = new List<Majors>
                {
                    new Majors { Name = "Computer Science", Code = "CS" },
                    new Majors { Name = "Software Engineering", Code = "SE" },
                    new Majors { Name = "Information Systems", Code = "IS" },
                    new Majors { Name = "Business Administration", Code = "BA" },
                    new Majors { Name = "Information Security / Cyber Security", Code = "SEC" },
                    new Majors { Name = "Finance & Banking", Code = "FIN" },
                    new Majors { Name = "Computer Networks and Data Communication", Code = "NET" },
                    new Majors { Name = "Artificial Intelligence", Code = "AI" }
                };
                context.Majors.AddRange(majors);
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
                        Name = "Free", 
                        Price = 0, 
                        DurationDays = 36500, 
                        FeaturesConfig = "{\"max_active_jobs\": 1, \"ai_interview_credits\": 2, \"cv_match_per_month\": 5}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Premium", 
                        Price = 299000, 
                        DurationDays = 30, 
                        FeaturesConfig = "{\"max_active_jobs\": 10, \"ai_interview_credits\": 20, \"cv_match_per_month\": 50}",
                        Status = SubscriptionStatus.ACTIVE
                    },
                    new Subscriptions 
                    { 
                        Name = "Enterprise", 
                        Price = 1499000, 
                        DurationDays = 30, 
                        FeaturesConfig = "{\"max_active_jobs\": -1, \"ai_interview_credits\": -1, \"cv_match_per_month\": -1}",
                        Status = SubscriptionStatus.ACTIVE
                    }
                };
                context.Subscriptions.AddRange(subs);
                await context.SaveChangesAsync();
            }
        }
    }
}
