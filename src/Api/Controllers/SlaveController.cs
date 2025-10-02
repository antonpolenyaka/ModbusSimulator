using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Domain.Entities;

namespace ModbusSimulator.Api.Controllers
{
    [ApiController]
    [Route("api/slaves")]
    public class SlaveController(SlaveStateService state) : ControllerBase
    {
        private readonly SlaveStateService _state = state;

        #region GET Endpoints
        [HttpGet("{unitId}/timesync")]
        public IActionResult GetTimeSync(byte unitId)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var block = slave.Maps.SelectMany(m => m.Ranges)
                          .FirstOrDefault(b => b.IsTimeSync);
            if (block == null) return NotFound("No TimeSync block defined");

            DateTime? dateTime = null;

            try
            {
                if (block.HoldingRegisters.Length >= 4)
                {
                    // Decode year
                    int year = 2000 + block.HoldingRegisters[0];

                    // Decode month and day
                    int month = (block.HoldingRegisters[1] >> 8) & 0xFF;
                    int day = block.HoldingRegisters[1] & 0xFF;

                    // Decode hour and minute
                    int hour = (block.HoldingRegisters[2] >> 8) & 0xFF;
                    int minute = block.HoldingRegisters[2] & 0xFF;

                    // Decode seconds + milliseconds
                    int second = block.HoldingRegisters[3] / 1000;
                    int millisecond = block.HoldingRegisters[3] % 1000;

                    dateTime = new DateTime(year, month, day, hour, minute, second, millisecond);
                }
            }
            catch
            {
                dateTime = null;
            }

            return Ok(new
            {
                block.StartAddress,
                Values = block.HoldingRegisters,
                FormattedDateTime = dateTime?.ToString("dd/MM/yyyy HH:mm:ss.fff") ?? "Invalid DateTime",
                LastSync = slave.LastTimeSync.HasValue
                    ? slave.LastTimeSync.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
                    : "Never"
            });
        }

        [HttpPut("{unitId}/timesync")]
        public IActionResult UpdateTimeSync(byte unitId, [FromBody] ushort[] values)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var block = slave.Maps.SelectMany(m => m.Ranges)
                          .FirstOrDefault(b => b.IsTimeSync);
            if (block == null) return NotFound("No TimeSync block defined");

            if (values.Length != block.Size)
                return BadRequest($"Expected {block.Size} words");

            for (int i = 0; i < block.Size; i++)
                block.HoldingRegisters[i] = values[i];

            slave.LastTimeSync = DateTime.UtcNow;
            return Ok("TimeSync registers updated");
        }

        // GET api/slaves/{unitId}/holding/{address}?length=5
        [HttpGet("{unitId}/holding/{address}")]
        public IActionResult GetHoldingRegisters(byte unitId, int address, [FromQuery] int length = 1)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetHoldingBlock(address);
            if (regBlock == null) return NotFound("Register block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + length > regBlock.HoldingRegisters.Length)
                return BadRequest("Requested range exceeds block size");

            var values = regBlock.HoldingRegisters[startIndex..(startIndex + length)];
            return Ok(values);
        }

        // GET api/slaves/{unitId}/input/{address}?length=3
        [HttpGet("{unitId}/input/{address}")]
        public IActionResult GetInputRegisters(byte unitId, int address, [FromQuery] int length = 1)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetInputBlock(address);
            if (regBlock == null) return NotFound("Input register block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + length > regBlock.InputRegisters.Length)
                return BadRequest("Requested range exceeds block size");

            var values = regBlock.InputRegisters[startIndex..(startIndex + length)];
            return Ok(values);
        }

        // GET api/slaves/{unitId}/coil/{address}?length=4
        [HttpGet("{unitId}/coil/{address}")]
        public IActionResult GetCoils(byte unitId, int address, [FromQuery] int length = 1)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetCoilBlock(address);
            if (regBlock == null) return NotFound("Coil block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + length > regBlock.Coils.Length)
                return BadRequest("Requested range exceeds block size");

            var values = regBlock.Coils[startIndex..(startIndex + length)];
            return Ok(values);
        }

        #endregion

        #region PUT Endpoints

        // PUT api/slaves/{unitId}/holding/{address}
        [HttpPut("{unitId}/holding/{address}")]
        public IActionResult UpdateHoldingRegisters(byte unitId, int address, [FromBody] ushort[] values)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetHoldingBlock(address);
            if (regBlock == null) return NotFound("Register block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + values.Length > regBlock.HoldingRegisters.Length)
                return BadRequest("Input values exceed block size");

            Array.Copy(values, 0, regBlock.HoldingRegisters, startIndex, values.Length);

            return Ok($"Updated {values.Length} holding registers starting at {address}");
        }

        // PUT api/slaves/{unitId}/input/{address}
        [HttpPut("{unitId}/input/{address}")]
        public IActionResult UpdateInputRegisters(byte unitId, int address, [FromBody] ushort[] values)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetInputBlock(address);
            if (regBlock == null) return NotFound("Input register block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + values.Length > regBlock.InputRegisters.Length)
                return BadRequest("Input values exceed block size");

            Array.Copy(values, 0, regBlock.InputRegisters, startIndex, values.Length);

            return Ok($"Updated {values.Length} input registers starting at {address}");
        }

        // PUT api/slaves/{unitId}/coil/{address}
        [HttpPut("{unitId}/coil/{address}")]
        public IActionResult UpdateCoils(byte unitId, int address, [FromBody] bool[] values)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var regBlock = slave.GetCoilBlock(address);
            if (regBlock == null) return NotFound("Coil block not found");

            int startIndex = address - regBlock.StartAddress;
            if (startIndex + values.Length > regBlock.Coils.Length)
                return BadRequest("Input values exceed block size");

            Array.Copy(values, 0, regBlock.Coils, startIndex, values.Length);

            return Ok($"Updated {values.Length} coils starting at {address}");
        }

        #endregion
    }
}
