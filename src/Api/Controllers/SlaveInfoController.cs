using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Domain.Entities;

namespace ModbusSimulator.Api.Controllers
{
    [ApiController]
    [Route("api/slaves/info")]
    public class SlaveInfoController(SlaveStateService state) : ControllerBase
    {
        private readonly SlaveStateService _state = state;

        // GET api/slaves/info/summary
        [HttpGet("summary")]
        public IActionResult GetSlavesSummary()
        {
            // Convert byte SlaveId to int to avoid Base64 serialization
            var slaveIds = _state.Slaves.Select(s => (int)s.SlaveId).ToArray();

            var summary = new
            {
                TotalSlaves = slaveIds.Length,
                SlaveIds = slaveIds
            };

            return Ok(summary);
        }

        // GET api/slaves/info/{unitId}
        [HttpGet("{unitId}")]
        public IActionResult GetSlave(byte unitId)
        {
            var slave = _state.GetSlave(unitId);
            if (slave == null) return NotFound("Slave not found");

            var info = new
            {
                slave.SlaveId,
                slave.SupportsTimeSync,
                Maps = slave.Maps.Select(m => new
                {
                    m.Type,
                    Ranges = m.Ranges.Select(r => new
                    {
                        r.Name,
                        r.StartAddress,
                        r.Size
                    })
                })
            };

            return Ok(info);
        }
    }
}
