using System.IO;

namespace ITHunterview.Service.Interface.Service
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName);
    }
}
