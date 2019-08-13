namespace IO.Eventuate.Tram.IntegrationTests
{
	/// <summary>
	/// Test configuration settings
	/// </summary>
	public class TestSettings
	{
	    public string KafkaBootstrapServers { get; set; } = "kafka:9092";
        /// <summary>
        /// Database connection strings
        /// </summary>
        public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();
	}
	
	/// <summary>
	/// Set of database connections
	/// </summary>
	public class ConnectionStrings
	{
	    /// <summary>
	    /// Eventuate Tram database connection string
	    /// </summary>
	    public string EventuateTramDbConnection { get; set; } = "Server=mssql,1433;Database=TramDb;User Id=sa;Password=TestPa$$word";

	}
}