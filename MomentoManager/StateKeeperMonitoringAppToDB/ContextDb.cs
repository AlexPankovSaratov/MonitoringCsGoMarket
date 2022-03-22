using Microsoft.EntityFrameworkCore;

namespace StateKeeperMonitoringApp
{
	public class ContextDb : DbContext
	{
		private string _dbConnection;

		public ContextDb(string dbConnection)
		{
			_dbConnection = dbConnection;
		}

		public DbSet<ShoppingItemBuy> ShoppingItemBuy { get;  set;  }
		public DbSet<ShoppingItemBlock> ShoppingItemBlock { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer(_dbConnection);
		}
	}
}
