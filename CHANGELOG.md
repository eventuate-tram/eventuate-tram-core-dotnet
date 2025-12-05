# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - ???

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

[Unreleased]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.7.0...HEAD
[0.7.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/eventuate-tram/eventuate-tram-core-dotnet/commits/v0.3.0