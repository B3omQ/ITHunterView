using ITHunterview.Domain.Entities.Cv;

namespace ITHunterview.Service.Interface.Service;

public interface ICvRenderer
{
    Task<Stream> RenderFinalAsync(CvDocument doc);
    Task<Stream> RenderPreviewImageAsync(CvDocument doc);
}
