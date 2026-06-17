using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Infrastructure.Persistence
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(ITHunterviewContext context)
        {
            if (!context.Users.Any())
            {
                var user = new User
                {
                    Email = "admin@ithunterview.com",
                    FullName = "Admin ITHunterview",
                    PasswordHash = PasswordHasher.HashPassword("123456")
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();
            }
        }
    }
}
