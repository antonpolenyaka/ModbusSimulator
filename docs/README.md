# Modbus TCP Simulator

## Overview

This project is a **Modbus TCP Simulator** built with **.NET 8.0** using **C#**, following **Domain-Driven Design (DDD)** and **Clean Architecture** principles.  

It allows simulating multiple Modbus slaves, each with multiple independent register ranges, including:  

- **Holding Registers** (e.g., measurements, events)  
- **Coils** (e.g., commands)  
- Supports non-contiguous address ranges per slave  
- Fully configurable through a JSON file (`slaves.json`) in the application root  

The simulator is designed to be scalable, testable, and maintainable, with clear separation of concerns between domain, application logic, infrastructure, and API layers.

---

## Project Structure

ModbusSimulator/
│
├── src/
│ ├── ModbusSimulator.Api/
│ │ ├── Program.cs # Entry point of the application
│ │ ├── appsettings.json # Configuration file
│ │ ├── Controllers/ # API controllers (optional)
│ │ └── Extensions/ # DI and middleware extensions
│ │
│ ├── ModbusSimulator.Application/
│ │ ├── Services/ # Application services (use cases)
│ │ │ └── ModbusServerService.cs
│ │ └── Interfaces/ # Interfaces for domain and infrastructure
│ │
│ ├── ModbusSimulator.Domain/
│ │ ├── Entities/
│ │ │ ├── Slave.cs
│ │ │ ├── RegisterBlock.cs
│ │ │ └── RegisterMap.cs
│ │ ├── ValueObjects/
│ │ │ └── AddressRange.cs
│ │ ├── Enums/
│ │ │ └── RegisterType.cs
│ │ ├── Events/
│ │ └── Exceptions/
│ │
│ ├── ModbusSimulator.Infrastructure/
│ │ ├── ModbusTcp/
│ │ │ ├── ModbusServer.cs
│ │ │ └── ModbusRequestHandler.cs
│ │ ├── Persistence/
│ │ │ └── SlaveJsonRepository.cs
│ │ └── Logging/
│ │
│ └── ModbusSimulator.Shared/
│ ├── Helpers/
│ └── Extensions/
│
├── tests/
│ ├── ModbusSimulator.Domain.Tests/
│ ├── ModbusSimulator.Application.Tests/
│ └── ModbusSimulator.Infrastructure.Tests/
│
└── docs/


---

## Configuration

Slaves are configured via a `slaves.json` file in the application root. Example:

```json
[
  {
    "SlaveId": 1,
    "Maps": [
      {
        "Type": "HoldingRegisters",
        "Ranges": [
          { "StartAddress": 0, "Size": 11, "Name": "Measures1" },
          { "StartAddress": 15, "Size": 11, "Name": "Measures2" }
        ]
      },
      {
        "Type": "Coils",
        "Ranges": [
          { "StartAddress": 0, "Size": 16, "Name": "Commands1" }
        ]
      }
    ]
  },
  {
    "SlaveId": 2,
    "Maps": [
      {
        "Type": "HoldingRegisters",
        "Ranges": [
          { "StartAddress": 0, "Size": 5, "Name": "Measures" }
        ]
      },
      {
        "Type": "Coils",
        "Ranges": [
          { "StartAddress": 0, "Size": 8, "Name": "Commands" }
        ]
      }
    ]
  }
]

Features

Multiple Modbus slaves running on the same TCP port

Multiple ranges per register type (holding registers, coils, etc.)

Supports non-contiguous ranges for measurements, events, commands

Configurable via JSON

Easy to extend to Input Registers, Discrete Inputs, and additional Modbus functions

Getting Started

Clone the repository:

git clone <repository-url>


Build the solution using .NET 8.0:

dotnet build


Place your slaves.json file in the root of the application (bin/Debug/net8.0/ or bin/Release/net8.0/)

Run the application:

dotnet run --project src/ModbusSimulator.Api


The server will start listening on TCP port 502 (default) and simulate all slaves defined in the JSON.

Testing

Unit tests and integration tests are organized by layer:

Domain.Tests → tests for entities and domain logic

Application.Tests → tests for services and use cases

Infrastructure.Tests → tests for JSON repository and TCP request handling

Run tests with:

dotnet test

License

This project is open-source under MIT License.