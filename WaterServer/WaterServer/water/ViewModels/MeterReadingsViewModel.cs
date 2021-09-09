using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using water.Models;
using X.PagedList;

namespace water.ViewModels
{
    public class MeterReadingsViewModel : PaginationViewModel
    {
        public IPagedList<DateTime> DateTimes { get; set; }

        public List<Meter> Meters { get; set; }

        public MeterReading[,] Readings { get; set; }
    }

    public class PaginationViewModel
    {
        [Display(Name = "Сортировать по:")]
        public OrderBy Order { get; set; } = OrderBy.DateDesc;

        [Display(Name = "Показывать по:")]
        public PageSize PageSize { get; set; } = PageSize.p20;

        public enum OrderBy
        {
            /// <summary>
            /// Дате добавления (сначала новые)
            /// </summary>
            [Display(Name = "Дате (сначала новые)")]
            DateDesc,

            /// <summary>
            /// Дате добавления (сначала новые)
            /// </summary>
            [Display(Name = "Дате (сначала старые)")]
            Date
        }
    }

    public enum PageSize
    {
        [Display(Name = "2")]
        p2 = 2,

        [Display(Name = "10")]
        p10 = 10,

        [Display(Name = "20")]
        p20 = 20,

        [Display(Name = "50")]
        p50 = 50,

        [Display(Name = "100")]
        p100 = 100
    }
}
