namespace ModbusSimulator.Domain.Entities
{
    public class RegisterMap
    {
        /// <summary>
        /// Type of register map: "HoldingRegisters" or "Coils"
        /// </summary>
        public string Type { get; set; } = string.Empty;

        public List<RegisterBlock> Ranges { get; set; } = [];
    }
}
