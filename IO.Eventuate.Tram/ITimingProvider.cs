namespace IO.Eventuate.Tram
{
	/// <summary>
	/// Interface to facilitate testing the ID generator.
	/// </summary>
	public interface ITimingProvider
	{
		/// <summary>
		/// Delay for specified number of milliseconds
		/// </summary>
		/// <param name="milliseconds">Number of milliseconds to delay</param>
		void DelayMilliseconds(int milliseconds);

		/// <summary>
		/// Get the current time in a millisecond resolution.
		/// </summary>
		/// <returns>Some number of relative milliseconds</returns>
		long GetNowMilliseconds();
	}
}
