using Microsoft.EntityFrameworkCore;

namespace IO.Eventuate.Tram.Database
{
	public class EventuateTramDbContextProvider : IEventuateTramDbContextProvider
	{
		private readonly EventuateSchema _eventuateSchema;
		private readonly DbContextOptions<EventuateTramDbContext> _dbContextOptions;

		public EventuateTramDbContextProvider(DbContextOptions<EventuateTramDbContext> dbContextOptions,
			EventuateSchema eventuateSchema)
		{
			_eventuateSchema = eventuateSchema;
			_dbContextOptions = dbContextOptions;
		}

		public EventuateTramDbContext CreateDbContext()
		{
			return new EventuateTramDbContext(_dbContextOptions, _eventuateSchema);
		}
	}
}