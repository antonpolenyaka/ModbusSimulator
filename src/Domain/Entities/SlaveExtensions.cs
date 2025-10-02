namespace ModbusSimulator.Domain.Entities
{
    // Extension methods to simplify access to register/coil blocks
    public static class SlaveExtensions
    {
        /// <summary>
        /// Get the holding register block containing the given address.
        /// </summary>
        public static RegisterBlock? GetHoldingBlock(this ModbusSlave slave, int address)
        {
            return slave.Maps
                .Where(m => m.Type.Equals("HoldingRegisters", StringComparison.OrdinalIgnoreCase))
                .SelectMany(m => m.Ranges)
                .FirstOrDefault(b => address >= b.StartAddress && address < b.StartAddress + b.Size);
        }

        /// <summary>
        /// Get the coil block containing the given address.
        /// </summary>
        public static RegisterBlock? GetCoilBlock(this ModbusSlave slave, int address)
        {
            return slave.Maps
                .Where(m => m.Type.Equals("Coils", StringComparison.OrdinalIgnoreCase))
                .SelectMany(m => m.Ranges)
                .FirstOrDefault(b => address >= b.StartAddress && address < b.StartAddress + b.Size);
        }

        /// <summary>
        /// Get the input register block containing the given address.
        /// </summary>
        public static RegisterBlock? GetInputBlock(this ModbusSlave slave, int address)
        {
            return slave.Maps
                .Where(m => m.Type.Equals("InputRegisters", StringComparison.OrdinalIgnoreCase))
                .SelectMany(m => m.Ranges)
                .FirstOrDefault(b => address >= b.StartAddress && address < b.StartAddress + b.Size);
        }
    }
}
