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