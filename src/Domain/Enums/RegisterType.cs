namespace ModbusSimulator.Domain.Enums
{
    /// <summary>
    /// Enum representing Modbus register types.
    /// </summary>
    public enum RegisterType
    {
        Coils,              // Discrete outputs (0x01)
        DiscreteInputs,     // Discrete inputs (0x02)
        HoldingRegisters,   // Read/Write registers (0x03)
        InputRegisters      // Read-only registers (0x04)
    }
}
