using System;

namespace IO.Eventuate.Tram;

public interface IJsonMapper
{
	public string ToJson(object o);

	public T FromJson<T>(string json);

	public object FromJson(string json, Type type);
}