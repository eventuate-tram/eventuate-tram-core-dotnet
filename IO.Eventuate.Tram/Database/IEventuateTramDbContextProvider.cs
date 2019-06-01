using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace IO.Eventuate.Tram.Database
{
	public interface IEventuateTramDbContextProvider
	{
		EventuateTramDbContext CreateDbContext();
	}
}