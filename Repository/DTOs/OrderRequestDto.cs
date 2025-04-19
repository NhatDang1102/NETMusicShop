using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Repository.DTOs
{
    public class OrderRequestDto
    {
        public string? VoucherCode { get; set; }

        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        public string ShippingAddress { get; set; } = null!;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string PhoneNumber { get; set; } = null!;
    }


}
