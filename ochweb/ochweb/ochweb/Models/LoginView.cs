using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace ochweb.Models
{
	public class LoginView
	{
		[Display(Name = "使用者帳號")]
		public string UserID { get; set; }

		[Display(Name = "使用者名稱")]
		public string UserNMC { get; set; }

		[Display(Name = "使用者密碼")]
		public string Password { get; set; }
		public string CreateDateTime { get; set; }
        public string ErrorMessage { get; set; }

        public LoginView()
		{
		}
	}
}
