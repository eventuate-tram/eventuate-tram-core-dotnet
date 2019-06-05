/*
 * Ported from:
 * repo:	https://github.com/eventuate-clients/eventuate-client-java
 * module:	eventuate-client-java-jdbc-common
 * package:	io.eventuate.javaclient.spring.jdbc
 */

namespace IO.Eventuate.Tram
{
	public interface IIdGenerator
	{
		Int128 GenId();
	}
}