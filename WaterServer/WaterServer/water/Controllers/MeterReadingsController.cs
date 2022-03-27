using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using water.Data;
using water.Models;

namespace water.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeterReadingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MeterReadingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Get()
        {
            return "Pong";
        }

        [HttpPost]
        public async Task<ActionResult<MeterReading>> PostMeterReading(InputReading meterReading)
        {
            var userMeters = await _context.Meters.Where(m => m.UserId == meterReading.UserId).ToListAsync();
            var date = DateTime.Now;

            foreach (var item in meterReading.Meters)
            {
                var meter = userMeters.FirstOrDefault(m => m.Number == item.Number);
                if (meter != null)
                {
                    _context.MeterReadings.Add(new MeterReading 
                    { 
                        MeterId = meter.Id, 
                        Value = item.Value, 
                        Vcc = item.Vcc ?? meterReading.Vcc, 
                        Date = item.Date ?? date 
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
