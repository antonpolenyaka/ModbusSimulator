namespace ModbusSimulator.Infrastructure.Configuration
{
    public class AppConfig
    {
        public ServerConfig Server { get; set; } = new();
        public List<SlaveConfig> Slaves { get; set; } = [];
    }
}
