using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace MainApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromForm] string prompt, [FromForm] IFormFile? image)
        {
            var reply = await _chatService.AskAssistantAsync(prompt, image);
            return Ok(new { reply });
        }


        [HttpPost("generate-image")]
        public async Task<IActionResult> GenerateImage([FromForm] string prompt)
        {
            var result = await _chatService.GenerateImageFromPromptAsync(prompt);
            return Ok(new { imageUrl = result });
        }

    }
}
