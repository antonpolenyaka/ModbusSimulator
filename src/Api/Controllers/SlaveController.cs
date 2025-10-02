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
