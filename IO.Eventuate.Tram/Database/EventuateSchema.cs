/*
 * Ported from:
 * repo:	https://github.com/eventuate-clients/eventuate-client-java
 * module:	eventuate-client-java-jdbc-common
 * package:	io.eventuate.javaclient.spring.jdbc
 */

using System;

namespace IO.Eventuate.Tram.Database
{
	public class EventuateSchema
	{
		public const string DefaultSchema = "eventuate";

		public EventuateSchema() {
			EventuateDatabaseSchema = DefaultSchema;
		}

		public EventuateSchema(string eventuateDatabaseSchema) {
			EventuateDatabaseSchema = String.IsNullOrWhiteSpace(eventuateDatabaseSchema) ? DefaultSchema : eventuateDatabaseSchema;
		}

		public string EventuateDatabaseSchema { get; }
	}
}