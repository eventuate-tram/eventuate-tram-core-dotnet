/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-producer-jdbc
 * package:	io.eventuate.tram.messaging.producer
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using IO.Eventuate.Tram.Database;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Producer.Database
{
	/// <summary>
	/// Implements the AbstractMessageProducer using a database context as the
	/// outbox for published messages to be sent to the message queue.
	/// Eventuate-tram CDC takes messages from the database and puts them
	/// into the message queue.
	/// </summary>
	public class DatabaseMessageProducer : AbstractMessageProducer, IMessageProducer, IMessageSender
	{
		private readonly IIdGenerator _idGenerator;

		private readonly IEventuateTramDbContextProvider _dbContextProvider;
		private readonly IJsonMapper _jsonMapper;

		/// <summary>
		/// Construct an OutboxMessageProducer
		/// </summary>
		/// <param name="messageInterceptors">Collection of intercepts applied before and
		/// after sending the message to outbox</param>
		/// <param name="idGenerator">Function to use for generating keys</param>
		/// <param name="dbContextProvider">DbContext provider used to provide persistence to the outbox</param>
		/// <param name="jsonMapper">JSON mapper</param>
		/// <param name="logger">Logger for diagnostic messages</param>
		public DatabaseMessageProducer(IEnumerable<IMessageInterceptor> messageInterceptors,
			IIdGenerator idGenerator, IEventuateTramDbContextProvider dbContextProvider,
			IJsonMapper jsonMapper, ILogger<DatabaseMessageProducer> logger)
			: base(messageInterceptors, logger)
		{
			_idGenerator = idGenerator;
			_dbContextProvider = dbContextProvider;
			_jsonMapper = jsonMapper;
		}
		
		/// <summary>
		/// Send a message to a specified destination (aka topic in Kafka speak).
		/// </summary>
		/// <param name="destination">Destination channel (topic) to publish to</param>
		/// <param name="message">Message to publish</param>
		public void Send(string destination, IMessage message)
		{
			var logContext = $"{nameof(Send)} destination='{destination}'";
			Logger.LogDebug($"+{logContext}");
			string id = _idGenerator.GenId().AsString();
			SendMessage(id, destination, message, this);
			Logger.LogDebug($"-{logContext}: sent message id={id}");
		}

		public async Task SendAsync(string destination, IMessage message)
		{
			var logContext = $"{nameof(SendAsync)} destination='{destination}'";
			Logger.LogDebug($"+{logContext}");
			string id = _idGenerator.GenId().AsString();
			await SendMessageAsync(id, destination, message, this);
			Logger.LogDebug($"-{logContext}: sent message id={id}");
		}

		/// <summary>
		/// Send message puts the message in the database to be processed
		/// by the CDC.
		/// </summary>
		/// <param name="message">Message to publish</param>
		void IMessageSender.Send(IMessage message)
		{
			// Relies on database column default value to set creation_time
			var messageEntity = new Message(message, _jsonMapper);
			using (EventuateTramDbContext dbContext = _dbContextProvider.CreateDbContext())
			{
				dbContext.Messages.Add(messageEntity);
				dbContext.SaveChanges();
			}
		}

		public async Task SendAsync(IMessage message)
		{
			// Relies on database column default value to set creation_time
			var messageEntity = new Message(message, _jsonMapper);
			await using (EventuateTramDbContext dbContext = _dbContextProvider.CreateDbContext())
			{
				dbContext.Messages.Add(messageEntity);
				await dbContext.SaveChangesAsync();
			}
		}
	}
}