using ModbusSimulator.Infrastructure.Configuration;
using System.Text.Json;

namespace ModbusSimulator.Infrastructure.Persistence
{
    public class SlaveJsonRepository(string? jsonFilePath = null)
    {
        private readonly string _jsonFilePath = jsonFilePath ?? Path.Combine(AppContext.BaseDirectory, "slaves.json");

        public AppConfig LoadServerConfig()
        {
            if (!File.Exists(_jsonFilePath))
                throw new FileNotFoundException($"Slave configuration file not found: {_jsonFilePath}");

            try
            {
                var json = File.ReadAllText(_jsonFilePath);
                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNameCaseInsensitive = true
                };
                var options = jsonSerializerOptions;

                AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json, options);

                return config ?? throw new Exception("No server and slaves configuration found in JSON.");
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error parsing JSON configuration: {ex.Message}", ex);
            }
        }
    }
}