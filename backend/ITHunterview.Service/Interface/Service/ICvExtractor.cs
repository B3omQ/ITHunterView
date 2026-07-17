using ITHunterview.Domain.Entities.Cv;

namespace ITHunterview.Service.Interface.Service;

public interface ICvExtractor
{
    Task<CvDocument> ExtractAsync(Stream fileStream);
}
