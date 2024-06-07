/*
 * Ported from:
 * repo:	https://github.com/eventuate-clients/eventuate-client-java
 * module:	eventuate-client-java-common-impl
 * package:	io.eventuate.javaclient.commonimpl
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IO.Eventuate.Tram
{
    public static class JsonMapper
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            // Use camel case for property names
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Ignore null values during serialization
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Make property name matching case-insensitive during deserialization
            PropertyNameCaseInsensitive = true
        };

        public static string ToJson(object o)
        {
            return JsonSerializer.Serialize(o, JsonSerializerOptions);
        }

        public static T FromJson<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
        }

        public static object FromJson(string json, Type type)
        {
            return JsonSerializer.Deserialize(json, type, JsonSerializerOptions);
        }
    }
}