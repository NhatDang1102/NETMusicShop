using System;
using System.Collections.Generic;

namespace Repository.DTOs
{
    public class OrderRequestDto
    {
        public string? VoucherCode { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }


}
