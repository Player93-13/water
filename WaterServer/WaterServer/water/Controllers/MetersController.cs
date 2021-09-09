using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using water.Data;
using water.Models;
using water.ViewModels;
using X.PagedList;

namespace water.Controllers
{
    [Authorize]
    public class MetersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MetersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Readings(int? page, int? pagesize)
        {
            var search = new MeterReadingsViewModel();

            await TryUpdateModelAsync(search);

            var userId = HttpContext.User.GetId();

            var dates = _context.MeterReadings
                         .Where(mr => mr.Meter.UserId == userId)
                         .GroupBy(mr => mr.Date)
                         .Select(gr => gr.Key);

            switch (search.Order)
            {
                case PaginationViewModel.OrderBy.DateDesc:
                    dates = dates.OrderByDescending(dt => dt);
                    break;
                case PaginationViewModel.OrderBy.Date:
                    dates = dates.OrderBy(dt => dt);
                    break;
            }

            var pgs = (int)search.PageSize;
            var pg = page ?? 1;

            search.DateTimes = new PagedList<DateTime>(dates, pg, pgs);

            var mindate = search.DateTimes.Min();
            var maxdate = search.DateTimes.Max();

            var readings = await _context.MeterReadings.Include(mr => mr.Meter)
                .Where(mr => mr.Meter.UserId == userId && mr.Date >= mindate && mr.Date <= maxdate)
                .OrderBy(mr => mr.Date).ThenBy(mr => mr.Id).ToListAsync();

            search.Meters = readings.Select(mr => mr.Meter).Distinct().ToList(); ;

            search.Readings = new MeterReading[search.DateTimes.Count, search.Meters.Count];
            for (int i = 0; i < search.DateTimes.Count; i++)
            {
                for (int j = 0; j < search.Meters.Count; j++)
                {
                    search.Readings[i,j] = readings.Where(mr => mr.Date == search.DateTimes[i] && mr.MeterId == search.Meters[j].Id).FirstOrDefault();
                }
            }

            return View(search);
        }

        // GET: Meters
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.User.GetId();
            var result = _context.Meters
                .Where(m => m.UserId == userId);

            return View(await result.ToListAsync());
        }

        // GET: Meters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            var userId = HttpContext.User.GetId();
            if (id == null)
            {
                return NotFound();
            }

            var meter = await _context.Meters
                .Where(m => m.UserId == userId)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (meter == null)
            {
                return NotFound();
            }

            return View(meter);
        }

        // GET: Meters/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Meters/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Number")] Meter meter)
        {
            meter.UserId = HttpContext.User.GetId();
            if (ModelState.IsValid)
            {
                _context.Add(meter);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(meter);
        }

        // GET: Meters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.User.GetId();
            var meter = await _context.Meters.Where(m => m.UserId == userId).FirstOrDefaultAsync(m => m.Id == id);
            if (meter == null)
            {
                return NotFound();
            }
            return View(meter);
        }

        // POST: Meters/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.User.GetId();
            var meter = await _context.Meters.Where(m => m.UserId == userId).FirstOrDefaultAsync(m => m.Id == id);
            if (meter == null)
            {
                return NotFound();
            }

            await TryUpdateModelAsync(meter);

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(meter);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MeterExists(meter.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(meter);
        }

        // GET: Meters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.User.GetId();
            var meter = await _context.Meters.Where(m => m.UserId == userId).FirstOrDefaultAsync(m => m.Id == id);
            if (meter == null)
            {
                return NotFound();
            }

            return View(meter);
        }

        // POST: Meters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = HttpContext.User.GetId();
            var meter = await _context.Meters.Where(m => m.UserId == userId).FirstOrDefaultAsync(m => m.Id == id);
            if (meter == null)
            {
                return NotFound();
            }
            _context.Meters.Remove(meter);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MeterExists(int id)
        {
            return _context.Meters.Any(e => e.Id == id);
        }
    }
}
