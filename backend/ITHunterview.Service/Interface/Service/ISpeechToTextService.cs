using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface ISpeechToTextService
    {
        Task<string> TranscribeAudioAsync(byte[] audioBytes, string contentType, string? languageCode = "vi");
    }
}
