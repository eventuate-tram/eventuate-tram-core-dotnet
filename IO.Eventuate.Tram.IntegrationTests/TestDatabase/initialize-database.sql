IF (NOT EXISTS(
    SELECT name
    FROM sys.schemas
    WHERE name = '$(TRAM_SCHEMA)'))
BEGIN
    EXEC('CREATE SCHEMA [$(TRAM_SCHEMA)]')
END

DROP TABLE IF EXISTS $(TRAM_SCHEMA).message;
DROP TABLE IF EXISTS $(TRAM_SCHEMA).received_messages;

CREATE TABLE $(TRAM_SCHEMA).message (
  id VARCHAR(450) PRIMARY KEY,
  destination NVARCHAR(1000) NOT NULL,
  headers NVARCHAR(1000) NOT NULL,
  payload NVARCHAR(MAX) NOT NULL,
  published SMALLINT DEFAULT 0,
  creation_time BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE $(TRAM_SCHEMA).message ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR creation_time

CREATE INDEX message_published_idx ON $(TRAM_SCHEMA).message(published, id);

CREATE TABLE $(TRAM_SCHEMA).received_messages (
  consumer_id VARCHAR(450),
  message_id VARCHAR(450),
  PRIMARY KEY(consumer_id, message_id),
  creation_time BIGINT
);

-- ADD default value expression for creation_time
ALTER TABLE $(TRAM_SCHEMA).received_messages ADD DEFAULT DATEDIFF_BIG(ms, '1970-01-01 00:00:00', GETUTCDATE()) FOR creation_time