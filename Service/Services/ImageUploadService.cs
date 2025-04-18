using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Service.Helpers;
using Service.Interfaces;

namespace Service.Services
{
    public class ImageUploadService : IImageUploadService
    {
        private readonly Cloudinary _cloudinary;

        public ImageUploadService(IOptions<CloudinarySettings> config) 
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
                );

            _cloudinary = new Cloudinary( acc );
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = "products"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.AbsoluteUri;
        }
    }
}
