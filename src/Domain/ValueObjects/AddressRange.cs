namespace ModbusSimulator.Domain.ValueObjects
{
    /// <summary>
    /// Value Object representing a range of Modbus addresses.
    /// </summary>
    public class AddressRange
    {
        public int Start { get; }
        public int End { get; }

        public int Size => End - Start + 1;

        public AddressRange(int start, int end)
        {
            if (end < start)
                throw new ArgumentException("End address must be greater than or equal to start address.");

            Start = start;
            End = end;
        }

        /// <summary>
        /// Checks if an address is within this range.
        /// </summary>
        public bool Contains(int address)
        {
            return address >= Start && address <= End;
        }
    }
}
