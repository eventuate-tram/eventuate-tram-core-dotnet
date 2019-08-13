using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests
{
	public class IdGeneratorTests
	{
		private IdGenerator _generator;
		private ITimingProvider _timingProvider;
		private bool _wasDelayed;
		private long _nowMilliseconds;

		[SetUp]
		public void SetupIdGenerator()
		{
			var logger = Substitute.For<ILogger<IdGenerator>>();
			_timingProvider = Substitute.For<ITimingProvider>();
			_generator = new IdGenerator(_timingProvider, logger);
		}

		[TestCase(1)]
		[TestCase(0x10000)]
		public void GetId_Get65536OrLessIds_NoDelay(int numberOfIds)
		{
			// Arrange
			// When delay is called, start moving the millisecond timer.
			_timingProvider.When(x => x.DelayMilliseconds(Arg.Any<int>()))
				.Do(arg => _wasDelayed = true);
			_timingProvider.GetNowMilliseconds()
				.ReturnsForAnyArgs(x => _wasDelayed ? ++_nowMilliseconds : _nowMilliseconds);

			// Act
			Int128 id = null;
			for (int i = 0; i < numberOfIds; i++)
			{
				id = _generator.GenId();
			}

			// Assert
			_timingProvider.DidNotReceiveWithAnyArgs().DelayMilliseconds(Arg.Any<int>());
			Assert.That(id?.Lo & 0xffff, Is.EqualTo(numberOfIds-1), "Count part of ID");
		}

		[Test]
		public void GetId_Get65537Ids_DelaysToNextMillisecond()
		{
			// Arrange
			// When delay is called, start moving the millisecond timer.
			_timingProvider.When(x => x.DelayMilliseconds(Arg.Any<int>()))
				.Do(arg => _wasDelayed = true);
			_timingProvider.GetNowMilliseconds()
				.ReturnsForAnyArgs(x => _wasDelayed ? ++_nowMilliseconds : _nowMilliseconds);

			// Act
			int numberOfIds = 0x10001;
			Int128 id = null;
			for (int i = 0; i < numberOfIds; i++)
			{
				id = _generator.GenId();
			}

			// Assert
			_timingProvider.Received(1).DelayMilliseconds(Arg.Any<int>());
			Assert.That(id?.Lo & 0xffff, Is.EqualTo(0), "Count part of ID");
		}
	}
}
