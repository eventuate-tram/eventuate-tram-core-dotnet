using System.Collections.Generic;
using Confluent.Kafka;
using IO.Eventuate.Tram.Local.Kafka.Consumer;
using NUnit.Framework;

namespace IO.Eventuate.Tram.UnitTests.Events.Consumer;

public class BackPressureManagerTests
{
	private readonly BackPressureConfig _config = new()
	{
		PauseThreshold = 10,
		ResumeThreshold = 5
	};

	[Test]
	public void BackPressureManager_SinglePartitionBelowPauseThreshold_DoesNotApplyBackPressure()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		int backlog = (int)(_config.PauseThreshold - 1);
		
		// Act
		BackPressureActions actions = manager.Update(new HashSet<TopicPartition> {partition1}, backlog);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(actions.PartitionsToPause, Is.Empty, "PartitionsToPause");
			Assert.That(actions.PartitionsToResume, Is.Empty, "PartitionsToResume");
		});
	}
	
	[Test]
	public void BackPressureManager_SinglePartitionAbovePauseThreshold_AppliesBackPressure()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		int backlog = (int)(_config.PauseThreshold + 1);
		
		// Act
		BackPressureActions actions = manager.Update(new HashSet<TopicPartition> {partition1}, backlog);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(actions.PartitionsToPause, Is.EquivalentTo(new List<TopicPartition>{partition1}), "PartitionsToPause");
			Assert.That(actions.PartitionsToResume, Is.Empty, "PartitionsToResume");
		});
	}
	
	[Test]
	public void BackPressureManager_SinglePausedPartitionAboveResumeThreshold_MaintainsBackPressure()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		int backlog = (int)(_config.PauseThreshold + 1);
		manager.Update(new HashSet<TopicPartition> {partition1}, backlog);
		
		// Act
		backlog = (int)(_config.ResumeThreshold + 1);
		BackPressureActions actions = manager.Update(new HashSet<TopicPartition>(), backlog);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(actions.PartitionsToPause, Is.Empty, "PartitionsToPause");
			Assert.That(actions.PartitionsToResume, Is.Empty, "PartitionsToResume");
		});
	}
	
	[Test]
	public void BackPressureManager_SinglePausedPartitionBelowResumeThreshold_RemovesBackPressure()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		int backlog = (int)(_config.PauseThreshold + 1);
		manager.Update(new HashSet<TopicPartition> {partition1}, backlog);
		
		// Act
		backlog = (int)(_config.ResumeThreshold - 1);
		BackPressureActions actions = manager.Update(new HashSet<TopicPartition>(), backlog);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(actions.PartitionsToPause, Is.Empty, "PartitionsToPause");
			Assert.That(actions.PartitionsToResume, Is.EquivalentTo(new List<TopicPartition>{partition1}), "PartitionsToResume");
		});
	}
	
	[Test]
	public void BackPressureManager_MultiplePartitionsAbovePauseThreshold_AppliesBackPressureToBoth()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		TopicPartition partition2 = new TopicPartition("topic2", new Partition(2));
		int backlog = (int)(_config.PauseThreshold + 1);
		
		// Act
		BackPressureActions actions1 = manager.Update(new HashSet<TopicPartition> {partition1}, backlog);
		BackPressureActions actions2 = manager.Update(new HashSet<TopicPartition> {partition2}, backlog);

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(actions1.PartitionsToPause, Is.EquivalentTo(new List<TopicPartition>{partition1}), "PartitionsToPause 1");
			Assert.That(actions2.PartitionsToPause, Is.EquivalentTo(new List<TopicPartition>{partition2}), "PartitionsToPause 2");
		});
	}
	
	[Test]
	public void BackPressureManager_MultiplePausedPartitionsBelowResumeThreshold_RemovesBackPressureToBoth()
	{
		// Arrange
		BackPressureManager manager = new BackPressureManager(_config);
		TopicPartition partition1 = new TopicPartition("topic1", new Partition(1));
		TopicPartition partition2 = new TopicPartition("topic2", new Partition(2));
		int backlog = (int)(_config.PauseThreshold + 1);
		manager.Update(new HashSet<TopicPartition> {partition1}, backlog);
		manager.Update(new HashSet<TopicPartition> {partition2}, backlog);
		
		// Act
		backlog = (int)(_config.ResumeThreshold - 1);
		BackPressureActions actions = manager.Update(new HashSet<TopicPartition>(), backlog);
		
		// Assert
		Assert.That(actions.PartitionsToResume, Is.EquivalentTo(new List<TopicPartition>{partition1, partition2}), "PartitionsToResume 1"); ;
	}
}