using System;
namespace Easymakemoney.Models
{
	public class LoginRequest
	{
        public required string username { get; set; }
        public required string password { get; set; }
    }
}

