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
    }
}
