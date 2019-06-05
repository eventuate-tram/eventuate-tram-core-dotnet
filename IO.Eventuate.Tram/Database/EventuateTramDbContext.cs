using IO.Eventuate.Tram.Consumer.Database;
using IO.Eventuate.Tram.Messaging.Producer.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IO.Eventuate.Tram.Database
{
	/// <summary>
	/// The database context for the eventuate tracking tables.
	/// </summary>
	public class EventuateTramDbContext : DbContext
	{
		// TODO: Currently this is MSSQL specific
		private const string CurrentTimeInMillisecondsSqlExpression = "DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE())";
		
		private readonly EventuateSchema _eventuateSchema;

		/// <summary>
		/// Default constructor
		/// </summary>
		public EventuateTramDbContext()
		{
			
		}

		/// <summary>
		/// Create the context and specify the schema for the eventuate tables
		/// </summary>
		/// <param name="options">Database context options for the base DbContext</param>
		/// <param name="eventuateSchema">Name for the schema to add the eventuate tables to</param>
		public EventuateTramDbContext(DbContextOptions<EventuateTramDbContext> options, EventuateSchema eventuateSchema) : base(options)
		{
			_eventuateSchema = eventuateSchema;
		}

		/// <summary>
		/// Table to hold published messages to get sent to the messaging system by CDC
		/// </summary>
		public DbSet<Message> Messages { get; set; }
		
		/// <summary>
		/// Table to track which messages have been processed for subscribers
		/// </summary>
		public DbSet<ReceivedMessage> ReceivedMessages { get; set; }

		/// <summary>
		/// Override to get the tables created.
		/// </summary>
		/// <param name="builder">DbContext build object</param>
		protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.HasDefaultSchema(_eventuateSchema.EventuateDatabaseSchema);
			builder.Entity<Message>(ConfigureMessage);
			builder.Entity<ReceivedMessage>(ConfigureReceivedMessage);
		}
		
		private void ConfigureMessage(EntityTypeBuilder<Message> builder)
		{
			builder.ToTable("message");

			builder.HasKey(m => m.Id);

			builder.Property(m => m.Id).HasColumnType("VARCHAR").HasMaxLength(767);

			builder.Property(m => m.Destination).HasMaxLength(1000).IsRequired();

			builder.Property(m => m.Headers).HasMaxLength(1000).IsRequired();

			builder.Property(m => m.Payload).IsRequired();

			builder.Property(m => m.Published)
				.HasDefaultValue(0);

			builder.Property(m => m.CreationTime).HasColumnName("creation_time")
				.HasDefaultValueSql(CurrentTimeInMillisecondsSqlExpression);
		}
		
		private void ConfigureReceivedMessage(EntityTypeBuilder<ReceivedMessage> builder)
		{
			builder.ToTable("received_messages");

			builder.HasKey(rm => new {rm.ConsumerId, rm.MessageId});

			builder.Property(rm => rm.ConsumerId).HasColumnType("VARCHAR").HasMaxLength(767)
				.HasColumnName("consumer_id").IsRequired();

			builder.Property(rm => rm.MessageId).HasColumnType("VARCHAR").HasMaxLength(767).HasColumnName("message_id")
				.IsRequired();

			builder.Property(m => m.CreationTime).HasColumnName("creation_time")
				.HasDefaultValueSql(CurrentTimeInMillisecondsSqlExpression);
		}
	}
}