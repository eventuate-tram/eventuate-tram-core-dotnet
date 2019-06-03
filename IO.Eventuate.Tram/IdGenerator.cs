/*
 * Ported from:
 * repo:	https://github.com/eventuate-clients/eventuate-client-java
 * module:	eventuate-client-java-jdbc-common
 * package:	io.eventuate.javaclient.spring.jdbc
 */

using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace IO.Eventuate.Tram
{
	/// <summary>
	/// Generates unique IDs for messages based on the MAC address,
	/// Unix 1 ms ticks, and an index.
	/// Note that multiple applications on the same hardware (same MAC)
	/// have a small opportunity for collision if IDs are generated in
	/// the same 1 ms tick.
	/// </summary>
	public class IdGenerator : IIdGenerator
	{
		private readonly ILogger<IdGenerator> _logger;
		private const long MaxCounter = 1 << 16;
		
		private readonly object _lockObject = new object();
		private readonly long _macAddress;
		private long _currentPeriod = TimeNow();
		private long _counter;
		
		/// <summary>
		/// Construct the ID generator. Reads the MAC address of the first NIC.
		/// </summary>
		/// <param name="logger">Logger</param>
		public IdGenerator(ILogger<IdGenerator> logger)
		{
			_logger = logger;
			var logContext = $"{nameof(IdGenerator)} constructor";
			_logger.LogDebug($"+{logContext}");
			NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
			
			long macAddress = interfaces.Select(i =>
				{
					PhysicalAddress address = i.GetPhysicalAddress();
					byte[] macAddressBytes = address.GetAddressBytes();
					
					// If the address doesn't have the expected length of 6, return 0 to skip to the next interface
					if (macAddressBytes.Length != 6)
					{
						_logger.LogTrace($"{logContext}: skipping MAC address {address}.");
						return 0L;
					}

					return ToLong(macAddressBytes);
				})
				.FirstOrDefault(addressAsLong => addressAsLong != 0L);

			if (macAddress == default(long))
			{
				_logger.LogError($"{logContext}: cannot find MAC address");
				throw new InvalidOperationException("Cannot find mac address");
			}

			_macAddress = macAddress;
			_logger.LogDebug($"{logContext}: Mac address {_macAddress}");
		}

		/// <summary>
		/// Use the same tick counter as the Java implementation
		/// since CDC sorts by ID.
		/// </summary>
		private static long TimeNow()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); ;
		}

		/// <summary>
		/// Creates a long value from a byte array by left shifting the array byte by byte into the long variable.
		/// For byte arrays with length less than 8 (e.g. 6), the least significant bytes of the long value will be populated,
		/// leaving the most significant bytes as 0.
		/// </summary>
		/// <param name="bytes">Input byte array.</param>
		/// <returns>The long value.</returns>
		/// <exception cref="ArgumentException">Thrown if the input byte array exceeds the maximum length for a long value.</exception>
		private static long ToLong(byte[] bytes)
		{
			if (bytes.Length > 8)
			{
				throw new ArgumentException("Input byte array exceeds maximum length for a long value.", nameof(bytes));
			}
			
			long result = 0L;
			foreach (byte b in bytes)
			{
				result = (result << 8) + b;
			}

			return result;
		}

		private Int128 MakeId()
		{
			return new Int128(_currentPeriod, (_macAddress << 16) + _counter);
		}

		private Int128 GenIdInternal()
		{
			var logContext = $"{nameof(GenIdInternal)}";
			_logger.LogDebug($"+{logContext}");
			long now = TimeNow();
			if (_currentPeriod != now || _counter == MaxCounter)
			{
				long oldPeriod = _currentPeriod;
				while ((_currentPeriod = TimeNow()) <= oldPeriod)
				{
					_logger.LogInformation($"{logContext}: Need to delay to reset the counter");
					// Just do nothing
					Thread.Sleep(1);
				}
				_counter = 0;
			}

			Int128 id = MakeId();
			_logger.LogDebug($"-{logContext}: returning id={id}, _counter={_counter}");
			_counter = _counter + 1;
			return id;
		}

		/// <summary>
		/// Generate a 128 bit unique identifier.
		/// </summary>
		/// <returns></returns>
		public Int128 GenId()
		{
			lock (_lockObject)
			{
				return GenIdInternal();
			}
		}
	}
}