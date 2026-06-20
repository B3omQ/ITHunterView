using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.Interface.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ITHunterview.WebAPI.Controllers
{
    [ApiController]
    [Route("api/upload")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService;

        public UploadController(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        [HttpPost]
        public async Task<ActionResult<ResponseBase<string>>> UploadFile(IFormFile file, [FromQuery] string folderName = "general")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ResponseBase<string>("File is missing or empty."));
            }

            try
            {
                using var stream = file.OpenReadStream();
                var url = await _fileUploadService.UploadFileAsync(stream, file.FileName, folderName);
                return Ok(new ResponseBase<string>(url, "File uploaded successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseBase<string>($"Error uploading file: {ex.Message}"));
            }
        }
    }
}
