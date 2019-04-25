using System;
using System.Data.SqlClient;
using IO.Eventuate.Tram.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Consumer.Kafka
{
	public class SqlTableBasedDuplicateMessageDetector : IDuplicateMessageDetector
	{
		private readonly EventuateTramDbContext _dbContext;
		private readonly ILogger<SqlTableBasedDuplicateMessageDetector> _logger;

		public SqlTableBasedDuplicateMessageDetector(EventuateTramDbContext dbContext,
			ILogger<SqlTableBasedDuplicateMessageDetector> logger)
		{
			_dbContext = dbContext;
			_logger = logger;
		}

		public bool IsDuplicate(string consumerId, string messageId)
		{
			string logContext = $"{nameof(IsDuplicate)} " +
			                    $"for {nameof(consumerId)}='{consumerId}', {nameof(messageId)}='{messageId}'";
			try
			{
				_logger.LogDebug($"+{logContext}");
				_dbContext.ReceivedMessages.Add(new ReceivedMessage {ConsumerId = consumerId, MessageId = messageId});
				_dbContext.SaveChanges();
				_logger.LogDebug($"-{logContext}");

				return false;
			}
			catch (DbUpdateException e)
			{
				const int duplicateKeyError = 2627;

				if (e.InnerException is SqlException sqlException && sqlException.Number == duplicateKeyError)
				{
					_logger.LogInformation($"{logContext}: Detected duplicate.");
					return true;
				}

				_logger.LogError(e, $"{logContext}: Got exception {e}");
				throw;
			}
		}
	}
}