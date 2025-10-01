namespace ModbusSimulator.Domain.Entities
{
    public class RegisterBlock
    {
        public int StartAddress { get; set; }
        public int Size { get; set; }
        public string Name { get; set; } = string.Empty;

        public ushort[] HoldingRegisters { get; set; } = [];
        public bool[] Coils { get; set; } = [];
    }
}
