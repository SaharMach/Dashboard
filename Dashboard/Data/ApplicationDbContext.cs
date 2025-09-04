

using Microsoft.EntityFrameworkCore;
using System;
using UsersMgmt.Entities;

namespace UsersMgmt.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
