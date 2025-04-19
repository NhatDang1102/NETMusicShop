using Microsoft.AspNetCore.Http;

namespace Service.Interfaces
{
    public interface IChatService
    {
        Task<string> AskAssistantAsync(string prompt, IFormFile? image);
    }
}
