using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChattingApplicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ChattingApplicationProject.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Photo> Photos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>().HasKey(u => u.Id);
            modelBuilder.Entity<AppUser>().Property(u => u.UserName).IsRequired().HasMaxLength(255);
            modelBuilder.Entity<AppUser>().Property(u => u.Role).IsRequired().HasMaxLength(255);

            modelBuilder.Entity<Photo>().HasKey(p => p.Id);
            modelBuilder.Entity<Photo>().Property(p => p.Url).IsRequired();
            modelBuilder.Entity<Photo>().Property(p => p.IsMain).IsRequired();
            modelBuilder
                .Entity<Photo>()
                .HasOne(p => p.AppUser)
                .WithMany(u => u.Photos)
                .HasForeignKey(p => p.AppUserId);
        }
    }
}
