using ModbusSimulator.Domain.Entities;

namespace ModbusSimulator.Infrastructure.ModbusTcp
{
    public static class ModbusRequestHandler
    {
        /// <summary>
        /// Processes a Modbus TCP request and returns the response bytes
        /// </summary>
        public static byte[] ProcessRequest(byte[] request, int length, ModbusSlave slave)
        {
            if (length < 8) return []; // Invalid MBAP header

            // MBAP header: 7 bytes
            ushort transactionId = (ushort)((request[0] << 8) + request[1]);
            // ushort protocolId = (ushort)((request[2] << 8) + request[3]);
            // ushort lengthField = (ushort)((request[4] << 8) + request[5]);
            byte unitId = request[6];
            byte functionCode = request[7];

            byte[] response = functionCode switch
            {
                // Read Coils
                0x01 => ReadCoils(request, slave, transactionId, unitId),
                // Read Holding Registers
                0x03 => ReadHoldingRegisters(request, slave, transactionId, unitId),
                // Read Input Registers
                0x04 => ReadInputRegisters(request, slave, transactionId, unitId),
                // Write Single Coil
                0x05 => WriteSingleCoil(request, slave, transactionId, unitId),
                // Write Single Register
                0x06 => WriteSingleRegister(request, slave, transactionId, unitId),
                // Write Multiple Registers (time sync case)
                0x10 => WriteMultipleRegisters(request, slave, transactionId, unitId),
                // Illegal function
                _ => ExceptionResponse(transactionId, unitId, functionCode, 0x01)
            };

            return response;
        }

        #region Function Implementations
        private static byte[] WriteMultipleRegisters(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            // Extract starting address and quantity of registers from request
            ushort startAddress = (ushort)((request[8] << 8) + request[9]);
            ushort quantity = (ushort)((request[10] << 8) + request[11]);

            // Decode the values sent
            byte byteCount = request[12];
            ushort[] values = new ushort[quantity];
            for (int i = 0; i < quantity; i++)
                values[i] = (ushort)((request[13 + i * 2] << 8) + request[14 + i * 2]);

            // Find the holding register block containing these addresses
            var regMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("HoldingRegisters", StringComparison.OrdinalIgnoreCase));
            if (regMap == null)
            {
                // No holding registers defined -> return illegal function
                return ExceptionResponse(transactionId, unitId, 0x10, 0x01);
            }

            bool written = false;

            foreach (var block in regMap.Ranges)
            {
                // Check if the block overlaps the request
                if (startAddress >= block.StartAddress && (startAddress + quantity) <= (block.StartAddress + block.Size))
                {
                    for (int i = 0; i < quantity; i++)
                    {
                        block.HoldingRegisters[startAddress - block.StartAddress + i] = values[i];
                    }

                    // If it's a time sync block, also store last sync time
                    if (block.IsTimeSync)
                    {
                        if (slave.SupportsTimeSync)
                            slave.LastTimeSync = DateTime.UtcNow;
                        else
                            return ExceptionResponse(transactionId, unitId, 0x10, 0x01); // Illegal function
                    }

                    written = true;
                    break; // written to one block
                }
            }

            if (!written)
            {
                // Requested address range does not exist in this slave
                return ExceptionResponse(transactionId, unitId, 0x10, 0x02); // illegal data address
            }

            // Build normal response: echo FunctionCode + StartAddress + Quantity
            byte[] response = new byte[12];
            response[0] = (byte)(transactionId >> 8);
            response[1] = (byte)(transactionId & 0xFF);
            response[2] = 0;
            response[3] = 0;
            response[4] = 0;
            response[5] = 6;
            response[6] = unitId;

            response[7] = 0x10; // Function code
            response[8] = (byte)(startAddress >> 8);
            response[9] = (byte)(startAddress & 0xFF);
            response[10] = (byte)(quantity >> 8);
            response[11] = (byte)(quantity & 0xFF);

            return response;
        }

        private static byte[] ReadHoldingRegisters(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            ushort startAddress = (ushort)((request[8] << 8) + request[9]);
            ushort quantity = (ushort)((request[10] << 8) + request[11]);

            var regMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("HoldingRegisters", StringComparison.OrdinalIgnoreCase));
            if (regMap == null) return ExceptionResponse(transactionId, unitId, 0x03, 0x02);

            ushort[] result = new ushort[quantity];

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddress + i;
                bool found = false;

                foreach (var block in regMap.Ranges)
                {
                    int index = addr - block.StartAddress;
                    if (index >= 0 && index < block.Size)
                    {
                        result[i] = block.HoldingRegisters[index];
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Address out of range → Modbus exception
                    return ExceptionResponse(transactionId, unitId, 0x03, 0x02);
                }
            }

            byte byteCount = (byte)(quantity * 2);
            byte[] data = new byte[byteCount];
            for (int i = 0; i < quantity; i++)
            {
                data[i * 2] = (byte)(result[i] >> 8);
                data[i * 2 + 1] = (byte)(result[i] & 0xFF);
            }

            return BuildResponseWithByteCount(transactionId, unitId, 0x03, data);
        }

        private static byte[] ReadCoils(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            ushort startAddress = (ushort)((request[8] << 8) + request[9]);
            ushort quantity = (ushort)((request[10] << 8) + request[11]);

            var coilsMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("Coils", StringComparison.OrdinalIgnoreCase));
            if (coilsMap == null) return ExceptionResponse(transactionId, unitId, 0x01, 0x02);

            bool[] result = new bool[quantity];

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddress + i;
                bool found = false;

                foreach (var block in coilsMap.Ranges)
                {
                    int index = addr - block.StartAddress;
                    if (index >= 0 && index < block.Size)
                    {
                        result[i] = block.Coils[index];
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Address out of range → Modbus exception
                    return ExceptionResponse(transactionId, unitId, 0x01, 0x02);
                }
            }

            byte byteCount = (byte)((quantity + 7) / 8);
            byte[] data = new byte[byteCount];
            for (int i = 0; i < quantity; i++)
            {
                if (result[i])
                    data[i / 8] |= (byte)(1 << (i % 8));
            }

            return BuildResponseWithByteCount(transactionId, unitId, 0x01, data);
        }

        private static byte[] WriteSingleCoil(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            ushort address = (ushort)((request[8] << 8) + request[9]);
            ushort value = (ushort)((request[10] << 8) + request[11]);

            var coilsMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("Coils", StringComparison.OrdinalIgnoreCase));
            if (coilsMap == null)
            {
                // No coil map -> illegal data address
                return ExceptionResponse(transactionId, unitId, 0x05, 0x02);
            }

            bool written = false;

            foreach (var block in coilsMap.Ranges)
            {
                int index = address - block.StartAddress;
                if (index >= 0 && index < block.Size)
                {
                    block.Coils[index] = value == 0xFF00;
                    written = true;
                    break;
                }
            }

            if (!written)
            {
                // Address not found -> illegal data address
                return ExceptionResponse(transactionId, unitId, 0x05, 0x02);
            }

            // Echo request as response (only if write succeeded)
            byte[] response = new byte[12];
            Array.Copy(request, 0, response, 0, 12);
            return response;
        }

        private static byte[] WriteSingleRegister(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            ushort address = (ushort)((request[8] << 8) + request[9]);
            ushort value = (ushort)((request[10] << 8) + request[11]);

            var regMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("HoldingRegisters", StringComparison.OrdinalIgnoreCase));
            if (regMap == null) return ExceptionResponse(transactionId, unitId, 0x06, 0x02);

            foreach (var block in regMap.Ranges)
            {
                int index = address - block.StartAddress;
                if (index >= 0 && index < block.Size)
                {
                    block.HoldingRegisters[index] = value;
                    break;
                }
            }

            // Echo request as response
            byte[] response = new byte[12];
            Array.Copy(request, 0, response, 0, 12);
            return response;
        }

        public static byte[] ExceptionResponse(ushort transactionId, byte unitId, byte functionCode, byte exceptionCode)
        {
            return BuildResponse(transactionId, unitId, (byte)(functionCode | 0x80), [exceptionCode]);
        }
        #endregion

        #region Helpers
        private static byte[] BuildResponse(ushort transactionId, byte unitId, byte functionCode, byte[] data)
        {
            // Length = UnitId (1 byte) + FunctionCode (1 byte) + Data.Length
            ushort length = (ushort)(1 + 1 + data.Length);

            // Total response = MBAP header (7 bytes) + FunctionCode + Data
            byte[] response = new byte[7 + length];

            // MBAP Header
            response[0] = (byte)(transactionId >> 8);
            response[1] = (byte)(transactionId & 0xFF);
            response[2] = 0; // Protocol ID high
            response[3] = 0; // Protocol ID low
            response[4] = (byte)(length >> 8); // Length high
            response[5] = (byte)(length & 0xFF); // Length low
            response[6] = unitId; // Unit ID

            // Function code
            response[7] = functionCode;

            // Copy data starting at index 8
            if (data != null && data.Length > 0)
                Array.Copy(data, 0, response, 8, data.Length);

            return response;
        }

        private static byte[] BuildResponseWithByteCount(ushort transactionId, byte unitId, byte functionCode, byte[] data)
        {
            byte byteCount = (byte)data.Length;

            // MBAP header (7) + FunctionCode (1) + ByteCount (1) + Data
            byte[] response = new byte[7 + 1 + 1 + data.Length];

            // MBAP
            response[0] = (byte)(transactionId >> 8);
            response[1] = (byte)(transactionId & 0xFF);
            response[2] = 0;
            response[3] = 0;

            ushort length = (ushort)(1 + 1 + 1 + data.Length); // UnitId + Func + ByteCount + Data
            response[4] = (byte)(length >> 8);
            response[5] = (byte)(length & 0xFF);

            response[6] = unitId;
            response[7] = functionCode;
            response[8] = byteCount;

            Array.Copy(data, 0, response, 9, data.Length);

            return response;
        }

        private static byte[] ReadInputRegisters(byte[] request, ModbusSlave slave, ushort transactionId, byte unitId)
        {
            // Extract starting address and number of registers to read
            ushort startAddress = (ushort)((request[8] << 8) + request[9]);
            ushort quantity = (ushort)((request[10] << 8) + request[11]);

            // Find the InputRegisters map in the slave definition
            var regMap = slave.Maps.FirstOrDefault(m => m.Type.Equals("InputRegisters", StringComparison.OrdinalIgnoreCase));
            if (regMap == null)
                // If no InputRegisters are defined for this slave → Modbus exception "Illegal data address"
                return ExceptionResponse(transactionId, unitId, 0x04, 0x02);

            ushort[] result = new ushort[quantity];

            for (int i = 0; i < quantity; i++)
            {
                int addr = startAddress + i;
                bool found = false;

                foreach (var block in regMap.Ranges)
                {
                    int index = addr - block.StartAddress;
                    if (index >= 0 && index < block.Size)
                    {
                        // Access the InputRegisters array from the block
                        result[i] = block.InputRegisters[index];
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Address not mapped in JSON → Modbus exception "Illegal data address"
                    return ExceptionResponse(transactionId, unitId, 0x04, 0x02);
                }
            }

            // Convert ushort values into bytes (big-endian: high byte first)
            byte byteCount = (byte)(quantity * 2);
            byte[] data = new byte[byteCount];
            for (int i = 0; i < quantity; i++)
            {
                data[i * 2] = (byte)(result[i] >> 8);
                data[i * 2 + 1] = (byte)(result[i] & 0xFF);
            }

            // Build and return the Modbus TCP response with byte count field
            return BuildResponseWithByteCount(transactionId, unitId, 0x04, data);
        }
        #endregion
    }
}
