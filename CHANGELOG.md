# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.0] - 2026-10-28

### Added
- Swimlane mapping, ported from the Java implementation: register an `ITopicPartitionToSwimlaneMapping` to choose how a
  subscriber's messages are assigned to swimlanes. `OriginalTopicPartitionToSwimlaneMapping` (one swimlane per
  partition number) is the default, as before; `SwimlanePerTopicPartition` gives each topic partition its own swimlane,
  so one topic's messages don't wait behind another's
- `BackPressure.Scope` consumer configuration property: `Topic` applies the back pressure thresholds to each topic on its
  own, pausing and resuming only that topic's partitions, and gives each topic partition its own swimlane unless another
  mapping is registered. `AllPartitions` (all of the consumer's partitions together) is the default, as before

### Changed
- `AllPartitions` back pressure now pauses all of the consumer's assigned partitions, including ones that haven't
  delivered a message yet; before, it paused only the partitions it had received messages from

### Fixed
- A Kafka consumer disposed before its consume loop had started was never closed, and disposing it threw
  `TaskCanceledException`
- Back pressure kept pausing and resuming partitions revoked from the consumer in a rebalance, and didn't pause
  partitions assigned to it while paused until one of their messages arrived

## [0.7.0] - 2025-12-05

### Changed
- Upgraded target framework to net10.0
- Upgraded Microsoft package dependencies to 10.0 versions
- Upgraded Confluent.Kafka package to version 2.12.0

## [0.6.0] - 2025-05-09

### Fixed
- Catch and log KafkaException in the consume loop so that the loop continues

## [0.5.0] - 2025-03-18

### Changed
- Switched from Newtonsoft.Json to System.Text.Json.

## [0.4.0] - 2024-05-03

### Changed
- Upgraded target framework to net8.0
- Upgraded Microsoft package dependencies to 8.0 versions
- Upgraded Confluent.Kafka package to version 2.3.0

## [0.3.0] - 2023-09-20

### Added
- Initial port of Eventuate Tram in .NET supporting the following functionality:
  - Messaging - send and receive messages over named channels
  - Events - publish domain events and subscribe to domain events
  - Support for Kafka message broker and Microsoft SQL database

[Unreleased]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/commits/v0.3.0