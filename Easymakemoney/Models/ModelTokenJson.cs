using System;
namespace Easymakemoney.Models
{
	public class ModelTokenJson
	{
        public string iat { get; set; }
        public string exp { get; set; }
        public string username { get; set; }
        public List<string> roles { get; set; }
    }
}

