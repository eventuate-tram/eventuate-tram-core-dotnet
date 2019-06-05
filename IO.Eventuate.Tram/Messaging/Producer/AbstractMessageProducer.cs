/*
 * Ported from:
 * repo:	https://github.com/eventuate-tram/eventuate-tram-core
 * module:	eventuate-tram-messaging
 * package:	io.eventuate.tram.messaging.producer
 */

using System;
using System.Collections.Generic;
using System.Linq;
using IO.Eventuate.Tram.Messaging.Common;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram.Messaging.Producer
{
	public abstract class AbstractMessageProducer
	{
		protected readonly ILogger Logger;
		protected readonly IMessageInterceptor[] MessageInterceptors;
		
		protected AbstractMessageProducer(IEnumerable<IMessageInterceptor> messageInterceptors,
			ILogger logger)
		{
			MessageInterceptors = messageInterceptors.ToArray();
			Logger = logger;
		}
		
		protected void PreSend(IMessage message)
		{
			var logContext = $"{nameof(PreSend)} message.Id={message.Id}";
			Logger.LogDebug($"+{logContext}");
			foreach (IMessageInterceptor messageInterceptor in MessageInterceptors)
			{
				messageInterceptor.PreSend(message);
			}
			Logger.LogDebug($"-{logContext}: sent to {MessageInterceptors.Length} message interceptors");
		}

		protected void PostSend(IMessage message, Exception e)
		{
			var logContext = $"{nameof(PostSend)} message.Id={message.Id}";
			Logger.LogDebug($"+{logContext}");
			foreach (IMessageInterceptor messageInterceptor in MessageInterceptors)
			{
				messageInterceptor.PostSend(message, e);
			}
			Logger.LogDebug($"-{logContext}: sent to {MessageInterceptors.Length} message interceptors");
		}

		protected void SendMessage(string id, string destination, IMessage message, IMessageSender messageSender)
		{
			var logContext = $"{nameof(SendMessage)} id='{id}', destination='{destination}', message.Id='{message.Id}'";
			Logger.LogDebug($"+{logContext}");
			if (id == null)
			{
				if (message.GetHeader(MessageHeaders.Id) == null)
				{
					Logger.LogError($"{logContext}: Message missing Id header");
					throw new ArgumentNullException(nameof(id), "message needs an id");
				}
			}
			else
			{
				message.SetHeader(MessageHeaders.Id, id);
			}

			message.SetHeader(MessageHeaders.Destination, destination);

			message.SetHeader(MessageHeaders.Date, HttpDateHeaderFormatUtil.NowAsHttpDateString());

			PreSend(message);
			try
			{
				messageSender.Send(message);
				PostSend(message, null);
			}
			catch (Exception e)
			{
				Logger.LogError(e, $"{logContext}: Exception sending message");
				PostSend(message, e);
				throw;
			}
		}
	}
}