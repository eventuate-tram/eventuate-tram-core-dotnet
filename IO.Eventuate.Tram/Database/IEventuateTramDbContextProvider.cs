namespace IO.Eventuate.Tram.Database
{
	public interface IEventuateTramDbContextProvider
	{
		EventuateTramDbContext CreateDbContext();
	}
}