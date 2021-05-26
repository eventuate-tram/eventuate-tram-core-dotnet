using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests
{
	public class IdGeneratorTests
	{
		private ITimingProvider _timingProvider;
		private IOptions<IdGeneratorOptions> _idGeneratorOptions;
		private ILogger<IdGenerator> _logger;
		private bool _wasDelayed;
		private long _nowMilliseconds;

		[SetUp]
		public void SetupIdGeneratorDependencies()
		{
			_logger = Substitute.For<ILogger<IdGenerator>>();
			_timingProvider = Substitute.For<ITimingProvider>();
			_idGeneratorOptions = Substitute.For<IOptions<IdGeneratorOptions>>();
		}

		private IdGenerator CreateIdGenerator()
		{
			return new IdGenerator(_timingProvider, _idGeneratorOptions, _logger);
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
			IdGenerator generator = CreateIdGenerator();

			// Act
			Int128 id = null;
			for (int i = 0; i < numberOfIds; i++)
			{
				id = generator.GenId();
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
			IdGenerator generator = CreateIdGenerator();

			// Act
			int numberOfIds = 0x10001;
			Int128 id = null;
			for (int i = 0; i < numberOfIds; i++)
			{
				id = generator.GenId();
			}

			// Assert
			_timingProvider.Received(1).DelayMilliseconds(Arg.Any<int>());
			Assert.That(id?.Lo & 0xffff, Is.EqualTo(0), "Count part of ID");
		}
		
		[Test]
		public void GetId_MacAddressOverridden_IdUsesOverriddenMacAddress()
		{
			// Arrange
			const string macAddressOverride = "12345";
			const long expectedMacAddress = 12345L;
			_idGeneratorOptions.Value.Returns(new IdGeneratorOptions {MacAddress = macAddressOverride});
			IdGenerator generator = CreateIdGenerator();

			// Act
			Int128 id = generator.GenId();

			// Assert
			Assert.That(id?.Lo >> 16, Is.EqualTo(expectedMacAddress), "MAC address part of ID");
		}

		[Test]
		public void Constructor_MacAddressOverriddenWithInvalidValue_ThrowsFormatException()
		{
			// Arrange
			const string macAddressOverride = "BigMac";
			_idGeneratorOptions.Value.Returns(new IdGeneratorOptions {MacAddress = macAddressOverride});

			// Act / Assert
			Assert.Throws<FormatException>(() => new IdGenerator(_timingProvider, _idGeneratorOptions, _logger));
		}
	}
}
