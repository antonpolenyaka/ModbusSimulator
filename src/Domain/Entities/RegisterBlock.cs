namespace ModbusSimulator.Domain.Entities
{
    public class RegisterBlock
    {
        public int StartAddress { get; set; }
        public int Size { get; set; }
        public string Name { get; set; } = string.Empty;

        // Array for Holding Registers (read/write)
        public ushort[] HoldingRegisters { get; set; } = [];

        // Array for Coils (read/write as booleans)
        public bool[] Coils { get; set; } = [];

        // Array for Input Registers (read-only registers)
        public ushort[] InputRegisters { get; set; } = [];
    }
}
