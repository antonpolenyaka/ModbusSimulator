namespace ModbusSimulator.Domain.Entities
{
    /// <summary>
    /// Service that stores and provides access to the state of all Modbus slaves.
    /// This allows both the Modbus TCP server and the Web API to work on the same data.
    /// </summary>
    public class SlaveStateService
    {
        // In-memory list of slaves
        public List<ModbusSlave> Slaves { get; } = [];

        /// <summary>
        /// Get a slave by its UnitId.
        /// </summary>
        public ModbusSlave? GetSlave(int unitId)
        {
            return Slaves.FirstOrDefault(s => s.SlaveId == unitId);
        }
    }
}
