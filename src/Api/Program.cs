using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModbusSimulator.Domain.Entities;
using ModbusSimulator.Infrastructure.Configuration;
using ModbusSimulator.Infrastructure.ModbusTcp;
using ModbusSimulator.Infrastructure.Persistence;

namespace ModbusSimulator.Api
{
    class Program
    {
        static void Main(string[] args)
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

            // Build ASP.NET Core WebApplication (for Swagger + REST endpoints)
            var builder = WebApplication.CreateBuilder(args);

            // Register services
            builder.Services.AddSingleton<SlaveStateService>();

            // Add controllers support
            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI();
            Console.WriteLine("Swagger UI available at http://localhost:5000/swagger/index.html");

            // Map controller routes
            app.MapControllers();

            // Resolve SlaveStateService to preload slaves from JSON
            var slaveState = app.Services.GetRequiredService<SlaveStateService>();

            // Initialize slaves from JSON config
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

                        if (map.Type.Equals("InputRegisters", StringComparison.OrdinalIgnoreCase))
                            block.InputRegisters = new ushort[block.Size];

                        map.Ranges.Add(block);
                    }

                    slave.Maps.Add(map);
                }

                slaveState.Slaves.Add(slave);
                Console.WriteLine($"Added Slave {slave.SlaveId} with {slave.Maps.Count} maps");
            }

            // Start Modbus server in background task
            Task.Run(() =>
            {
                Console.WriteLine($"Server will bind to IP: {appConfig.Server.Ip}, Port: {appConfig.Server.Port}");
                var server = new ModbusServer(slaveState, appConfig.Server.Ip, appConfig.Server.Port);
                server.Start();
                Console.WriteLine($"Modbus TCP Server running on {appConfig.Server.Ip}:{appConfig.Server.Port}");
            });

            // Run Web API
            app.Run();
        }
    }
}
