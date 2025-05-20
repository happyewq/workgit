using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
    public class BibleLogRecord
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public bool HasRead { get; set; }
    }

    public class BibleLogViewModel
    {
        [Required(ErrorMessage = "請選擇查詢日期")]
        public DateTime? QueryDate { get; set; }
        public List<BibleLogRecord> Records { get; set; }
    }
}
