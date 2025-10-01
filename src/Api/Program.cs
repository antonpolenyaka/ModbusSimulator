using ModbusSimulator.Domain.Entities;
using ModbusSimulator.Infrastructure.Configuration;
using ModbusSimulator.Infrastructure.ModbusTcp;
using ModbusSimulator.Infrastructure.Persistence;

namespace ModbusSimulator.Api
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting Modbus TCP Simulator...");

            // Load server configuration from JSON
            var repository = new SlaveJsonRepository();
            AppConfig appConfig;

            try
            {
                appConfig = repository.LoadServerConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading configuration: {ex.Message}");
                return;
            }

            if (appConfig == null || appConfig.Slaves.Count == 0)
            {
                Console.WriteLine("No slave configuration found.");
                return;
            }

            // Create server with IP and Port from JSON
            Console.WriteLine($"Server will bind to IP: {appConfig.Server.Ip}, Port: {appConfig.Server.Port}");
            var server = new ModbusServer(appConfig.Server.Ip, appConfig.Server.Port);

            // Add slaves
            foreach (var config in appConfig.Slaves)
            {
                var slave = new ModbusSlave { SlaveId = config.SlaveId, SupportsTimeSync = config.SupportsTimeSync };

                foreach (var mapConfig in config.Maps)
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

                        map.Ranges.Add(block);
                    }

                    slave.Maps.Add(map);
                }

                server.AddSlave(slave);
                Console.WriteLine($"Added Slave {slave.SlaveId} with {slave.Maps.Count} maps");
            }

            // Start the Modbus TCP server
            server.Start();
            Console.WriteLine($"Modbus TCP Server running on {appConfig.Server.Ip}:{appConfig.Server.Port}. Press ENTER to stop...");

            Console.ReadLine();
            server.Stop();

            Console.WriteLine("Server stopped.");
        }
    }
}
