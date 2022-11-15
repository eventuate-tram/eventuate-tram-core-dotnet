using System;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureConfig
{
	public int Low { get; set; } = 0;
	public int High { get; set; } = int.MaxValue;
}