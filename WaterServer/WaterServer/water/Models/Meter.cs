using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace water.Models
{
    public class Meter
    {
        public int Id { get; set; }

        public string Number { get; set; }

        public int UserId { get; set; }

        public virtual ApplicationUser User { get; set; }

        public virtual ICollection<MeterReading> Readings { get; set; }
    }

    public class MeterReading
    {
        public int Id { get; set; }

        public int MeterId { get; set; }

        public virtual Meter Meter { get; set; }

        public int Value { get; set; }

        /// <summary>
        /// Напряжение питания контроллера
        /// </summary>
        public int Vcc { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
    }
}
