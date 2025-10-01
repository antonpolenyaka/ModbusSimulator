namespace ModbusSimulator.Domain.Entities
{
    public class ModbusSlave
    {
        public byte SlaveId { get; set; }
        public List<RegisterMap> Maps { get; set; } = [];
    }
}
