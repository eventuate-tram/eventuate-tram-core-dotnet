using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
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
		private SqlTableBasedDuplicateMessageDetector _detector;
		private EventuateTramDbContext _dbContext;

		[SetUp]
		public void SetupDetector()
		{
			var dbContextProvider = Substitute.For<IEventuateTramDbContextProvider>();
			_dbContext = Substitute.For<EventuateTramDbContext>();
			dbContextProvider.CreateDbContext().Returns(_dbContext);
			var logger = Substitute.For<ILogger<SqlTableBasedDuplicateMessageDetector>>();
			_detector = new SqlTableBasedDuplicateMessageDetector(dbContextProvider, logger);
		}

		[Test]
		public void IsDuplicate_ReceiveMessageCollision_ReturnsTrue()
		{
			// Arrange
			const int duplicateKeyError = 2627;
			var sqlException = CreateSqlException(duplicateKeyError, "Already there");
			_dbContext.SaveChanges().Throws(new DbUpdateException("Duplicate",
				sqlException));

			// Act
			bool isDuplicate = _detector.IsDuplicate("consumer", "message");

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
