using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
    public class BibleLogViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> DateList { get; set; } = new List<string>(); // yyyyMMdd
        public List<BibleLogRecord> Records { get; set; } = new List<BibleLogRecord>();
    }

    public class BibleLogRecord
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public Dictionary<string, bool> DailyReadMap { get; set; } = new Dictionary<string, bool>();
    }
}
