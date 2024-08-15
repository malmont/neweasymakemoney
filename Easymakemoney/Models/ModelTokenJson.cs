using System;
namespace Easymakemoney.Models
{
	public class ModelTokenJson
	{
        public required string iat { get; set; }
        public required string exp { get; set; }
        public required string username { get; set; }
        public required List<string> roles { get; set; }
    }
}

