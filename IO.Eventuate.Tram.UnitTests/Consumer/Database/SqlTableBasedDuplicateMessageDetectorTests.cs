using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Consumer.Database;
using IO.Eventuate.Tram.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Consumer.Database
{
	public class SqlTableBasedDuplicateMessageDetectorTests
	{
		[Test]
		public async Task IsDuplicate_ReceiveMessageCollision_ReturnsTrue()
		{
			// Arrange
			var context = Substitute.For<EventuateTramDbContext>();
			context.ReceivedMessages = Substitute.For<DbSet<ReceivedMessage>>();
			var logger = Substitute.For<ILogger<SqlTableBasedDuplicateMessageDetector>>();
			var dbContextProvider = Substitute.For<IEventuateTramDbContextProvider>();
			dbContextProvider.CreateDbContext().Returns(context);
			var detector = new SqlTableBasedDuplicateMessageDetector(dbContextProvider, logger);

			const int duplicateKeyError = 2627;
			var sqlException = CreateSqlException(duplicateKeyError, "Already there");
			context.SaveChangesAsync().Throws(new DbUpdateException("Duplicate",
				sqlException));

			// Act
			bool isDuplicate = await detector.IsDuplicateAsync("consumer", "message");

			// Assert
			Assert.That(isDuplicate, Is.True, "IsDuplicate response");
		}

		/// <summary>
		/// Method to make a SqlException since it is sealed and
		/// has no visible constructor
		/// </summary>
		/// <param name="number">Exception number</param>
		/// <param name="message">Exception message</param>
		/// <returns></returns>
		private static SqlException CreateSqlException(int number, string message)
		{
			var collectionConstructor = typeof(SqlErrorCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
				null, //binder
				new Type[0],
				null);
			var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
			var errorCollection = (SqlErrorCollection)collectionConstructor.Invoke(null);
			var errorConstructor = typeof(SqlError).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
				new[]
				{
					typeof (int), typeof (byte), typeof (byte), typeof (string), typeof(string), typeof (string),
					typeof (int), typeof (uint), typeof (Exception)
				}, null);
			var error = errorConstructor.Invoke(new object[] { number, (byte)0, (byte)0, "server", message, "procedure", 100, (uint)0, null });
			addMethod.Invoke(errorCollection, new[] { error });
			var constructor = typeof(SqlException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, //visibility
				null, //binder
				new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
				null); //param modifiers
			return (SqlException)constructor.Invoke(new object[] { message, errorCollection, new DataException(), Guid.NewGuid() });
		}
	}
}
