using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
    public class OchC020View
    {
        public int SessionID { get; set; }
        public string SessionName { get; set; }
        public string SessionContent { get; set; }
        public string SessionLocation { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string CreateDateTime { get; set; }

        public string UserID { get; set; }
        public string UserType { get; set; }
        public decimal FeeAmount { get; set; }
        public string PaidYN { get; set; }
        public string RegisterTime { get; set; }
        public OchC020View()
        {
        }
    }


    public class SessionDetailViewModel
    {
        public int SessionID { get; set; }
        public string SessionName { get; set; }
        public List<RegistrantInfo> Registrants { get; set; }

        public class RegistrantInfo
        {
            public string UserID { get; set; }

            public string UserNMC { get; set; }
            
            public string UserType { get; set; }
            public decimal FeeAmount { get; set; }
            public string PaidYN { get; set; }
            public string RegisterTime { get; set; }
        }
    }

    public class RegistrantCreateViewModel
    {
        [Required]
        public int SessionID { get; set; }

        [Display(Name = "帳號/員編")]
        [Required, StringLength(50)]
        public string UserID { get; set; }

        [Display(Name = "姓名")]
        [Required, StringLength(100)]
        public string UserNMC { get; set; }

        [Display(Name = "身分")]
        [Required] // 預期 "w"=上班族, "s"=學生
        public string UserType { get; set; } = "w";

        [Display(Name = "費用")]
        [Range(0, 999999)]
        public decimal FeeAmount { get; set; } = 0;

        [Display(Name = "是否繳交")]
        [Required] // "Y" 或 "N"
        public string PaidYN { get; set; } = "N";

        [Display(Name = "報名時間")]
        public DateTime? RegisterTime { get; set; } = DateTime.Now; // 也可改用 DB 預設 NOW()
    }

    public class RegistrantEditViewModel
    {
        [Required]
        public int SessionID { get; set; }

        [Display(Name = "帳號/員編")]
        [Required, StringLength(50)]
        public string UserID { get; set; }   // 編輯頁面禁改

        [Display(Name = "姓名")]
        [Required, StringLength(100)]
        public string UserNMC { get; set; }

        [Display(Name = "身分")]
        [Required] // "w"=上班族, "s"=學生
        public string UserType { get; set; }

        [Display(Name = "費用")]
        [Range(0, 999999)]
        public decimal FeeAmount { get; set; }

        [Display(Name = "是否繳交")]
        [Required] // "Y" 或 "N"
        public string PaidYN { get; set; }

        [Display(Name = "報名時間")]
        public DateTime? RegisterTime { get; set; }
    }

}
