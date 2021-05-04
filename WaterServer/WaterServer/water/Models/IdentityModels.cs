using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace water.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Display(Name = "Адрес электронной почты")]
        public override string Email { get => base.Email; set => base.Email = value; }

        [Display(Name = "Телефон")]
        public override string PhoneNumber { get => base.PhoneNumber; set => base.PhoneNumber = value; }

        /// <summary>
        /// Дата регистрации
        /// </summary>
        public DateTime RegisterDate { get; private set; } = DateTime.Now;

        /// <summary>
        /// Дата последнего входа в систему
        /// </summary>
        public DateTime? LastSigInDate { get; set; }

        public virtual ICollection<Meter> Meters { get; set; }
    }
}
