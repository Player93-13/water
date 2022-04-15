using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using water.Data;
using water.Models;

namespace water.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class GraphController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GraphController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> ByYearAsync(DateTime dateStart, DateTime dateEnd)
        {
            var dates = new List<DateTime>();
            var sTemp = new DateTime(dateStart.Year, 1, 1);
            while (sTemp <= dateEnd.Date)
            {
                dates.Add(sTemp.Date);
                sTemp = sTemp.AddYears(1);
            }

            return await GetDataAsync(dates,
                dateStart,
                dateEnd,
                mr => new GroupBy { MeterId = mr.MeterId, Year = mr.Date.Year },
                d => d.ToString("yyyy"));
        }

        public async Task<object> ByMonthAsync(DateTime dateStart, DateTime dateEnd)
        {
            var dates = new List<DateTime>();
            var sTemp = dateStart.AddDays(1 - dateStart.Day);
            while (sTemp <= dateEnd.Date)
            {
                dates.Add(sTemp.Date);
                sTemp = sTemp.AddMonths(1);
            }

            return await GetDataAsync(dates,
                dateStart,
                dateEnd,
                mr => new GroupBy { MeterId = mr.MeterId, Year = mr.Date.Year, Month = mr.Date.Month },
                d => d.ToString("MMM yy"));
        }

        public async Task<object> ByDayAsync(DateTime dateStart, DateTime dateEnd)
        {
            var dates = new List<DateTime>();
            var sTemp = dateStart.Date;
            while (sTemp <= dateEnd.Date)
            {
                dates.Add(sTemp.Date);
                sTemp = sTemp.AddDays(1);
            }

            return await GetDataAsync(dates,
                dateStart, 
                dateEnd, 
                mr => new GroupBy (mr.MeterId, mr.Date.Date), 
                d => d.ToString("dd.MM"));
        }

        public async Task<object> ByHourAsync(DateTime dateStart, DateTime dateEnd)
        {
            var userId = HttpContext.User.GetId();
            var datasets = await _context.Meters
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    Meter = m,
                    StartReading = m.Readings.Where(r => r.Date < dateStart.Date).OrderByDescending(r => r.Id).Select(r => r.Value).FirstOrDefault()
                })
                .Select(m => new
                {
                    label = m.Meter.Title ?? m.Meter.Number,
                    backgroundColor = m.Meter.ColorGraphHex,
                    borderColor = m.Meter.ColorGraphHex,
                    cubicInterpolationMode = "monotone",
                    tension = 0.4,
                    fill = false,
                    data = m.Meter.Readings
                        .Where(mr => mr.Date >= dateStart.Date && mr.Date < dateEnd.AddDays(1).Date)
                        .Select(mr => new { x = mr.Date, y = mr.Value - m.StartReading })
                })
                .ToListAsync();

            var labels = new List<DateTime>();
            var sTemp = dateStart.Date;
            while (sTemp <= dateEnd.Date)
            {
                labels.Add(sTemp.Date);
                sTemp = sTemp.AddHours(1);
            }

            return new { datasets };
        }

        class GroupBy
        {
            public GroupBy(int meterId, DateTime date)
            {
                MeterId = meterId;
                Year = date.Year;
                Month = date.Month;
                Day = date.Day;
            }

            public GroupBy()
            {

            }

            public int MeterId;
            public int Year;
            public int? Month;
            public int? Week;
            public int? Day;
        }

        private async Task<object> GetDataAsync(List<DateTime> dates, DateTime dateStart, DateTime dateEnd, Expression<Func<MeterReading, GroupBy>> groupExpression, Func<DateTime, string> labelSelect)
        {
            var userId = HttpContext.User.GetId();
            var meters = await _context.Meters
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    Meter = m,
                    StartReading = m.Readings.Where(r => r.Date < dateStart.Date).OrderByDescending(r => r.Id).Select(r => r.Value).FirstOrDefault()
                })
                .ToListAsync();

            if (meters.Count > 0)
            {

                var readings = await _context.MeterReadings
                    .Where(mr => mr.Meter.UserId == userId)
                    .Where(mr => mr.Date >= dateStart.Date && mr.Date < dateEnd.AddDays(1).Date)
                    .GroupBy(groupExpression)
                    .Select(gr => new { key = gr.Key, val = gr.Max(c => c.Value) })
                    .ToListAsync();

                readings = (from r in readings
                           orderby r.key.MeterId, r.key.Year, r.key.Month ?? 0, r.key.Day ?? 0
                           select r).ToList();

                var tempdata = (from m in meters
                                orderby m.Meter.Id
                                select new
                                {
                                    m.Meter,
                                    startReading = m.StartReading,
                                    data = (from r in readings
                                            where r.key.MeterId == m.Meter.Id
                                            orderby r.key.MeterId, r.key.Year, r.key.Month, r.key.Day
                                            select r).ToList()
                                }).ToList();

                //delta calc
                foreach (var item in tempdata)
                {
                    if (item.data.Count > 0)
                    {
                        for (int i = item.data.Count - 1; i > 0; i--)
                        {
                            var val = item.data[i].val - item.data[i - 1].val;
                            item.data[i] = new { item.data[i].key, val };
                        }
                        var valt = item.data[0].val - item.startReading;
                        item.data[0] = new { item.data[0].key, val = valt };
                    }
                }

                var meterData = (from m in tempdata
                                 select new
                                 {
                                     m.Meter,
                                     data = dates
                                          .OrderBy(d => d)
                                          .Select(d => m.data.Where(r => r.key.MeterId == m.Meter.Id 
                                                                    && d.Year == r.key.Year 
                                                                    && (!r.key.Month.HasValue || r.key.Month == d.Month)
                                                                    && (!r.key.Day.HasValue || r.key.Day == d.Day))
                                                               .Select(r => r.val)
                                                               .FirstOrDefault())
                                          .ToList()
                                 }).ToList();

                var datasets = from m in meterData
                               orderby m.Meter.Id
                               select new
                               {
                                   label = m.Meter.Title ?? m.Meter.Number,
                                   backgroundColor = m.Meter.ColorGraphHex,
                                   borderColor = m.Meter.ColorGraphHex,
                                   m.data,
                                   cubicInterpolationMode = "monotone",
                                   tension = 0.4
                               };

                var labels = dates.Select(labelSelect);

                return new { labels, datasets };
            }

            return null;
        }
    }
}
