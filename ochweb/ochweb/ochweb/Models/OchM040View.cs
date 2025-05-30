﻿using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
    public class OchM040View
    {
        [Display(Name = "使用者帳號")]
        public string UserID { get; set; }

        [Display(Name = "使用者名稱")]
        public string UserNMC { get; set; }

        [Display(Name = "使用者密碼")]
        public string Password { get; set; }
        [Display(Name = "新密碼")]
        public string NewPassword { get; set; }
        [Display(Name = "使用者權限")]
        public string Permission { get; set; }

        [Display(Name = "確認密碼")]
        public string ConfirmPassword { get; set; }
        public string CreateDateTime { get; set; }
        public string ErrorMessage { get; set; }

        public OchM040View()
        {
        }
    }
}
