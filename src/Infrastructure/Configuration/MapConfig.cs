namespace ModbusSimulator.Infrastructure.Configuration
{
    public class MapConfig
    {
        public string Type { get; set; } = string.Empty; // "HoldingRegisters" or "Coils"
        public List<RangeConfig> Ranges { get; set; } = [];
    }
}
