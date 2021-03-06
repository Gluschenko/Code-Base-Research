﻿using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

public class JsonUtility
{
    public static string ToJson(object obj)
    {
        if (obj == null)
            return "";

        var jsonFormatter = new DataContractJsonSerializer(obj.GetType());

        using var stream = new MemoryStream();
        jsonFormatter.WriteObject(stream, obj);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static object FromJson(string json, Type type)
    {
        if (string.IsNullOrEmpty(json))
            return null;
        if (type == null)
            throw new ArgumentNullException("type");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var jsonFormatter = new DataContractJsonSerializer(type);
        return jsonFormatter.ReadObject(stream);
    }

    public static T FromJson<T>(string json)
    {
        return (T)FromJson(json, typeof(T));
    }
}
