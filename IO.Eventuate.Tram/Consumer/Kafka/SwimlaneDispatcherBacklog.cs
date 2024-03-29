﻿using System.Collections.Concurrent;
using IO.Eventuate.Tram.Local.Kafka.Consumer;

namespace IO.Eventuate.Tram.Consumer.Kafka;

public class SwimlaneDispatcherBacklog : IMessageConsumerBacklog
{
	private readonly ConcurrentQueue<QueuedMessage> _queue;
	
	internal SwimlaneDispatcherBacklog(ConcurrentQueue<QueuedMessage> queue) {
		_queue = queue;
	}
	
	public int Size()
	{
		return _queue.Count;
	}
}