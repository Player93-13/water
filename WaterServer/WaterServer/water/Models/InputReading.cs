using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace water.Models
{
    public class InputReading
    {
        public int UserId { get; set; }

        public int Vcc { get; set; } = 0;

        public IEnumerable<InputMeter> Meters { get; set; }
    }

    public class InputMeter
    {
        public string Number { get; set; }

        public int Value { get; set; }
    }
}
