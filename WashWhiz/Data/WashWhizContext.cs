using Microsoft.EntityFrameworkCore;
using WashWhiz.Models;

namespace WashWhiz.Data
{
    public class WashWhizContext : DbContext
    {
        public WashWhizContext(DbContextOptions<WashWhizContext> options)
            : base(options)
        {
        }

        public DbSet<LaundryOrder> Orders { get; set; }
        public DbSet<UserAccount> Users { get; set; }
    }
}