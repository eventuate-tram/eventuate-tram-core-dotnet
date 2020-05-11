using System;
using Microsoft.Data.SqlClient;
using System.Transactions;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Database
{
	public class SqlTableBasedDuplicateMessageDetector : IDuplicateMessageDetector
	{
		private readonly IEventuateTramDbContextProvider _dbContextProvider;
		private readonly ILogger<SqlTableBasedDuplicateMessageDetector> _logger;

		public SqlTableBasedDuplicateMessageDetector(IEventuateTramDbContextProvider dbContextProvider, 
			ILogger<SqlTableBasedDuplicateMessageDetector> logger)
		{
			_dbContextProvider = dbContextProvider;
			_logger = logger;
		}

		public bool IsDuplicate(string consumerId, string messageId)
		{
			string logContext = $"{nameof(IsDuplicate)} " +
			                    $"for {nameof(consumerId)}='{consumerId}', {nameof(messageId)}='{messageId}'";
			try
			{
				_logger.LogDebug($"+{logContext}");
				using (EventuateTramDbContext dbContext = _dbContextProvider.CreateDbContext())
				{
					// Relies on database column default value to set creation_time
					dbContext.ReceivedMessages.Add(new ReceivedMessage
						{ConsumerId = consumerId, MessageId = messageId});
					dbContext.SaveChanges();
				}

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

		public void DoWithMessage(SubscriberIdAndMessage subscriberIdAndMessage, Action callback)
		{
			string logContext = $"{nameof(DoWithMessage)} " +
			                    $"for {nameof(SubscriberIdAndMessage.SubscriberId)}='{subscriberIdAndMessage.SubscriberId}', " +
			                    $"MessageId='{subscriberIdAndMessage.Message.Id}'";

			using (var transactionScope = new TransactionScope())
			{
				try
				{
					if (IsDuplicate(subscriberIdAndMessage.SubscriberId,
						subscriberIdAndMessage.Message.Id))
					{
						_logger.LogDebug($"{logContext}: message is a duplicate");
					}
					else
					{
						_logger.LogDebug($"{logContext}: Invoking handlers");
						callback();
					}

					transactionScope.Complete();
					_logger.LogDebug($"{logContext}: Processed message");
				}
				catch (Exception e)
				{
					_logger.LogError(e, $"{logContext}: Exception processing message {e}");
					throw;
				}
			}
		}
	}
}