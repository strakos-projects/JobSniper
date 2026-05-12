using System.Threading.Tasks;

namespace JobSniper.AiServices
{
    public interface IPipelineStep
    {
        string StepName { get; }
        Task ExecuteAsync(PipelineContext context, IAiClient aiClient);
    }
}