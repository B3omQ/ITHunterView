using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ITHunterview.Service.Config;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service
{
    public class CloudinaryService : IFileUploadService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            if (string.IsNullOrEmpty(config.Value.CloudName) || config.Value.CloudName == "your-cloud-name")
            {
                // Bypassed for local testing
                return;
            }

            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName)
        {
            // Tạm thời trả về link ảo nếu chưa cài đặt Cloudinary, không ảnh hưởng đến code gốc bên dưới
            if (_cloudinary == null)
            {
                return $"https://dummyimage.com/cv-mock/{folderName}/{fileName}";
            }

            if (fileStream == null || fileStream.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }

            var uploadResult = new RawUploadResult();

            // For Stream we don't have ContentType easily, but we can check extension
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            bool isImage = extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".webp";
            
            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folderName
                };
                var imgResult = await _cloudinary.UploadAsync(uploadParams);
                if (imgResult.Error != null)
                {
                    throw new Exception(imgResult.Error.Message);
                }
                return imgResult.SecureUrl.ToString();
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folderName
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
                
                if (uploadResult.Error != null)
                {
                    throw new Exception(uploadResult.Error.Message);
                }
                return uploadResult.SecureUrl.ToString();
            }
        }
    }
}
