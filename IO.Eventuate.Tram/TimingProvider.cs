using System;
using System.Threading;

namespace IO.Eventuate.Tram
{
	/// <inheritdoc />
	public class TimingProvider : ITimingProvider
	{
		/// <inheritdoc />
		public void DelayMilliseconds(int milliseconds)
		{
			Thread.Sleep(milliseconds);
		}

		/// <inheritdoc />
		public long GetNowMilliseconds()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		}
	}
}