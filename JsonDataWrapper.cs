using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;

namespace Flight_Reservations
{
    public class JsonDataWrapper
    {
        
        public void SaveToFile<T>(string filePath, List<T> data)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All, // (Business/Economy)
                    Formatting = Formatting.Indented
                };
                string json = JsonConvert.SerializeObject(data, settings);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public List<T> LoadFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<T>();
                var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<T>>(json, settings);
            }
            catch
            {
                return new List<T>();
            }
        }
    }
}