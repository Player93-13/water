using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace water.Models
{
    public class Meter
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Номер")]
        public string Number { get; set; }

        public int UserId { get; set; }

        [StringLength(50)]
        [Display(Name = "Наименование")]
        public string Title { get; set; } = "Вода";

        [Range(0, 0xFFFFFF)]
        [Display(Name = "Цвет на графике")]
        public int ColorGraph { get; set; } = 0xFF0000;

        [NotMapped]
        [Display(Name = "Цвет на графике")]
        public string ColorGraphHex { get => "#" + ColorGraph.ToString("X6"); set => ColorGraph = int.Parse(value.Substring(1, 6), System.Globalization.NumberStyles.HexNumber); }

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
