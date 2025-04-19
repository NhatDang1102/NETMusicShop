using Microsoft.AspNetCore.Http;

namespace Repository.DTOs
{
    public class ChatRequestDto
    {
        public string Prompt { get; set; }
        public IFormFile? Image { get; set; } 
    }
}
