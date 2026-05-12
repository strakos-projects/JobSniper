using System.Threading.Tasks;

namespace JobSniper.AiServices
{
    public interface IAiClient
    {
        Task<string> GetCompletionAsync(string systemPrompt, string userPrompt);
    }
}