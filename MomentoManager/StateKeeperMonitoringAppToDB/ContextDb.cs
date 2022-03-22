using Microsoft.EntityFrameworkCore;

namespace StateKeeperMonitoringApp
{
	public class ContextDb : DbContext
	{
		public DbSet<ShoppingItemBuy> ShoppingItemBuy { get;  set;  }
		public DbSet<ShoppingItemBlock> ShoppingItemBlock { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer(@"Server=DESKTOP-S9EJ5LH\SQLEXPRESS;DataBase=MonitoringDb;Integrated Security=SSPI");
		}
	}
}
