namespace ModbusSimulator.Infrastructure.Configuration
{
    public class RangeConfig
    {
        public int StartAddress { get; set; }
        public int Size { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
