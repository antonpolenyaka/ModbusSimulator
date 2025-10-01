namespace ModbusSimulator.Infrastructure.Configuration
{
    public class SlaveConfig
    {
        public byte SlaveId { get; set; }
        public bool SupportsTimeSync { get; set; }
        public List<MapConfig> Maps { get; set; } = [];
    }
}
