namespace IO.Eventuate.Tram
{
	/// <summary>
	/// Interface for getting the current time and delaying for a specified amount of time
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
