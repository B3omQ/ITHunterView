using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Interface.Persistence;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ITHunterviewContext _context;

        public SubscriptionRepository(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<Subscriptions> CreateAsync(Subscriptions subscription)
        {
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task UpdateAsync(Subscriptions subscription)
        {
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
        }

        public Task<Subscriptions?> GetByIdAsync(int id)
        {
            return _context.Subscriptions.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Subscriptions?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Subscriptions
                .FromSqlRaw("SELECT * FROM subscriptions WHERE id = {0} FOR UPDATE", id)
                .SingleOrDefaultAsync();
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<List<Subscriptions>> GetAllAsync(string? role, SubscriptionStatus? status)
        {
            var query = _context.Subscriptions.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var items = await query.ToListAsync();

            if (!string.IsNullOrEmpty(role))
            {
                items = items.Where(x => 
                {
                    if (string.IsNullOrEmpty(x.FeaturesConfig)) return false;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(x.FeaturesConfig);
                        if (doc.RootElement.TryGetProperty("role", out var roleElement) || 
                            doc.RootElement.TryGetProperty("Role", out roleElement))
                        {
                            return string.Equals(roleElement.GetString(), role, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch { }
                    
                    return x.FeaturesConfig.Contains($"\"role\":\"{role}\"", StringComparison.OrdinalIgnoreCase) || 
                           x.FeaturesConfig.Contains($"\"role\": \"{role}\"", StringComparison.OrdinalIgnoreCase) ||
                           x.FeaturesConfig.Contains($"\"Role\":\"{role}\"", StringComparison.OrdinalIgnoreCase) || 
                           x.FeaturesConfig.Contains($"\"Role\": \"{role}\"", StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            return items.OrderBy(x => x.Price).ToList();
        }

        public Task<bool> IsSubscriptionUsedAsync(int id)
        {
            // Kiểm tra xem đã có người dùng nào mua gói subscription này chưa
            return _context.UserSubscriptions.AnyAsync(us => us.SubId == id);
        }

        public async Task<(List<Subscriptions> Items, int TotalCount)> GetPagedAsync(
            string? role, 
            SubscriptionStatus? status, 
            int page, 
            int pageSize)
        {
            var query = _context.Subscriptions.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var allItems = await query.ToListAsync();

            Console.WriteLine("================= DEBUG FEATURES CONFIG =================");
            foreach (var item in allItems)
            {
                Console.WriteLine($"ID: {item.Id}, FeaturesConfig: {item.FeaturesConfig}");
            }
            Console.WriteLine($"Role to filter: {role}");
            Console.WriteLine("=========================================================");

            if (!string.IsNullOrEmpty(role))
            {
                allItems = allItems.Where(x => 
                {
                    if (string.IsNullOrEmpty(x.FeaturesConfig)) return false;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(x.FeaturesConfig);
                        if (doc.RootElement.TryGetProperty("role", out var roleElement) || 
                            doc.RootElement.TryGetProperty("Role", out roleElement))
                        {
                            return string.Equals(roleElement.GetString(), role, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch { }
                    
                    return x.FeaturesConfig.Contains($"\"role\":\"{role}\"", StringComparison.OrdinalIgnoreCase) || 
                           x.FeaturesConfig.Contains($"\"role\": \"{role}\"", StringComparison.OrdinalIgnoreCase) ||
                           x.FeaturesConfig.Contains($"\"Role\":\"{role}\"", StringComparison.OrdinalIgnoreCase) || 
                           x.FeaturesConfig.Contains($"\"Role\": \"{role}\"", StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            int totalCount = allItems.Count;
            var items = allItems.OrderBy(x => x.Price)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            return (items, totalCount);
        }

        public async Task<int> UpdateUnusedSubscriptionAsync(Subscriptions subscription)
        {
            // Cập nhật Atomic (ExecuteUpdate) chỉ dành cho các field được phép thay đổi
            // Không được dùng với dbContext thông thường vì cần lock Row Level trên CSDL
            // Hoặc đơn giản là chạy ExecuteUpdateAsync với điều kiện sub chưa bị dùng
            return await _context.Subscriptions
                .Where(s => s.Id == subscription.Id)
                // Cần tham chiếu DbSet UserSubscriptions nhưng EF Core có thể thực thi subquery
                .Where(s => !_context.UserSubscriptions.Any(us => us.SubId == s.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Price, subscription.Price)
                    .SetProperty(p => p.DurationDays, subscription.DurationDays)
                    .SetProperty(p => p.FeaturesConfig, subscription.FeaturesConfig));
        }
    }
}
