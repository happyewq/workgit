using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ochweb.Models
{
	public class OchC010View
	{

        [Display(Name = "名稱")]
        public string name { get; set; }

        [Display(Name = "人數")]
        public string people { get; set; }

        [Display(Name = "次數")]
        public string times { get; set; }

        [Display(Name = "對話")]
        public string talk { get; set; }

        [Display(Name = "Line")]
		public string line { get; set; }
        [Display(Name = "使用者帳號")]
        public string date { get; set; }
        [Display(Name = "刪除字串")]
        public string deletestring { get; set; }

        [Display(Name = "使用者名稱")]
		public string UserNMC { get; set; }

		[Display(Name = "使用者密碼")]
		public string Password { get; set; }
		public string CreateDateTime { get; set; }
		public string ErrorMessage { get; set; }

		public OchC010View()
		{
		}
	}
}
