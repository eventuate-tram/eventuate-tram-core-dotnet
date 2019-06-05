using System;
using System.Data.SqlClient;
using System.Transactions;
using IO.Eventuate.Tram.Consumer.Common;
using IO.Eventuate.Tram.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Consumer.Database
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
				// Relies on database column default value to set creation_time
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

		public void DoWithMessage(SubscriberIdAndMessage subscriberIdAndMessage, Action callback)
		{
			string logContext = $"{nameof(DoWithMessage)} " +
			                    $"for {nameof(SubscriberIdAndMessage.SubscriberId)}='{subscriberIdAndMessage.SubscriberId}', " +
			                    $"MessageId='{subscriberIdAndMessage.Message.Id}'";

			//TODO: Do we need this strategy. Is the callback going to get called again on a retry?
			IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
			strategy.Execute(() =>
			{
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
			});
		}
	}
}