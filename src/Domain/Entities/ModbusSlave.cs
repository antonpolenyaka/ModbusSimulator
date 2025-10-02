namespace ModbusSimulator.Domain.Entities
{
    public class ModbusSlave
    {
        public byte SlaveId { get; set; }
        public bool SupportsTimeSync { get; set; }
        public List<RegisterMap> Maps { get; set; } = [];
        public DateTime? LastTimeSync { get; set; } = null;
    }
}
