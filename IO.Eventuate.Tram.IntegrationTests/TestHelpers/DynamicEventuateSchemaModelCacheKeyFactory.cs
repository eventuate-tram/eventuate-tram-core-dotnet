using IO.Eventuate.Tram.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IO.Eventuate.Tram.IntegrationTests.TestHelpers
{
	/// <summary>
	/// Used to ensure a new model is created for each different EventuateSchema
	/// Otherwise, the first model created is cached and used even if the EventuateSchema has changed
	/// </summary>
	public class DynamicEventuateSchemaModelCacheKeyFactory : IModelCacheKeyFactory
	{
		public object Create(DbContext context, bool designTime)
		{
			if (context is EventuateTramDbContext eventuateTramDbContext)
			{
				return (context.GetType(), eventuateTramDbContext.EventuateDatabaseSchema);
			}
			return context.GetType();
		}
	}
}
