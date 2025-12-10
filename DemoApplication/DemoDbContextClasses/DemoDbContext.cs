using DemoApplication.Models.UserAccount;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApplication.DemoDbContextClasses
{
    public class DemoDbContext : DbContext
    {
        public DemoDbContext(DbContextOptions<DemoDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<LkpUserGroup> UserGroups { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserGroup)
                .WithMany(g => g.Users)
                .HasForeignKey(u => u.UserGroupId)
                .OnDelete(DeleteBehavior.Restrict);


            // Configure relationships
            modelBuilder.Entity<UserLog>()
                .HasOne(ul => ul.User)
                .WithMany()
                .HasForeignKey(ul => ul.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            /*
            // Seed initial data
            modelBuilder.Entity<LkpUserGroup>().HasData(
                new LkpUserGroup { UserGroupId = 1, UserGroupName = "Admin" },
                new LkpUserGroup { UserGroupId = 2, UserGroupName = "User" }
            );

            // Create an admin user (password: Admin@123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    FullName = "Administrator",
                    Username = "admin",
                    Email = "admin@demo.com",
                    PhoneNo = "01234567890",
                    UserGroupId = 1,
                    PasswordHash = "GYV2F0uz2c2rPLZ8cYbQxq6Yqs92Tk1Lj2S/gduLxIk=",
                    PasswordSalt = "g9Nh6QFmff0j+QsQKj8L1g==",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                }
            );
            */
        }

    }
}
