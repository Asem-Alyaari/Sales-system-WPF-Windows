using App2.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace App2.Data
{
    public static class DatabaseSeeder
    {
        public static void SeedDefaultUser(AppDbContext context)
        {
            try
            {
                // Check if admin user exists, if not create it
                var adminExists = context.Users.Any(u => u.Username == "admin");
                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Password = HashPassword("admin123"),
                        FullName = "مدير النظام",
                        Email = "admin@app2.com",
                        Role = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Users.Add(adminUser);
                }

                // Check if user exists, if not create it
                var userExists = context.Users.Any(u => u.Username == "user");
                if (!userExists)
                {
                    var regularUser = new User
                    {
                        Username = "user",
                        Password = HashPassword("123456"),
                        FullName = "مستخدم عادي",
                        Email = "user@app2.com",
                        Role = "User",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Users.Add(regularUser);
                }

                context.SaveChanges();
            }
            catch
            {
                // Ignore errors during seeding
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
