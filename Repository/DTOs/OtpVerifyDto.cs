using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.DTOs
{
    public class OtpVerifyDto
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
    }
}
