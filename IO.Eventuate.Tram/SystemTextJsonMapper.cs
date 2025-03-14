using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Eventuate.Tram;

public class SystemTextJsonMapper : IJsonMapper
{
	public static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNameCaseInsensitive = true
	};
	
	public string ToJson(object o)
	{
		return JsonSerializer.Serialize(o, JsonSerializerOptions);
	}

	public T FromJson<T>(string json)
	{
		return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
	}

	public object FromJson(string json, Type type)
	{
		return JsonSerializer.Deserialize(json, type, JsonSerializerOptions);
	}
}