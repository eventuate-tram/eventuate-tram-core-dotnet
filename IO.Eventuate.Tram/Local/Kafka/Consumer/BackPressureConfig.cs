using System;

namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureConfig
{
	public uint ResumeThreshold { get; set; } = 0;
	public uint PauseThreshold { get; set; } = int.MaxValue;
}