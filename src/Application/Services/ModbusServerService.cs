using ModbusSimulator.Domain.Entities;
using ModbusSimulator.Infrastructure.Configuration;
using ModbusSimulator.Infrastructure.ModbusTcp;
using ModbusSimulator.Infrastructure.Persistence;

namespace ModbusSimulator.Application.Services
{
    public class ModbusServerService(SlaveJsonRepository slaveRepository)
    {
        private readonly SlaveJsonRepository _slaveRepository = slaveRepository ?? throw new ArgumentNullException(nameof(slaveRepository));
        private ModbusServer? _server;

        public void StartServer()
        {
            // Load server config (IP, Port and Slaves)
            AppConfig config = _slaveRepository.LoadServerConfig();

            if (config.Slaves == null || config.Slaves.Count == 0)
                throw new Exception("No slave configurations found.");

            // Create server with IP and Port from JSON
            _server = new ModbusServer(config.Server.Ip, config.Server.Port);

            foreach (var slaveConfig in config.Slaves)
            {
                var slave = new ModbusSlave { SlaveId = slaveConfig.SlaveId, SupportsTimeSync = slaveConfig.SupportsTimeSync };

                foreach (var mapConfig in slaveConfig.Maps)
                {
                    var map = new RegisterMap { Type = mapConfig.Type };

                    foreach (var range in mapConfig.Ranges)
                    {
                        var block = new RegisterBlock
                        {
                            StartAddress = range.StartAddress,
                            Size = range.Size,
                            Name = range.Name
                        };

                        if (map.Type.Equals("HoldingRegisters", StringComparison.OrdinalIgnoreCase))
                            block.HoldingRegisters = new ushort[block.Size];

                        if (map.Type.Equals("Coils", StringComparison.OrdinalIgnoreCase))
                            block.Coils = new bool[block.Size];

                        if (map.Type.Equals("InputRegisters", StringComparison.OrdinalIgnoreCase))
                            block.InputRegisters = new ushort[block.Size];

                        map.Ranges.Add(block);
                    }

                    slave.Maps.Add(map);
                }

                _server.AddSlave(slave);
                Console.WriteLine($"Added Slave {slave.SlaveId} with {slave.Maps.Count} maps");
            }

            _server.Start();
            Console.WriteLine($"Modbus TCP Server started on {config.Server.Ip} : {config.Server.Port}");
        }

        public void StopServer()
        {
            _server?.Stop();
            Console.WriteLine("Modbus TCP Server stopped.");
        }
    }
}
