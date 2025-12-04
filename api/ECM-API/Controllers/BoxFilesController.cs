using ECM_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using ECM_API.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ECM_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BoxFilesController : ControllerBase
    {
        private readonly BoxApiService _api;
        private readonly TokenService _tokenService;
        private readonly ScormProcessingService _scormProcessingService;

        public BoxFilesController(BoxApiService api, TokenService tokenService, ScormProcessingService scormProcessingService)
        {
            _api = api;
            _tokenService = tokenService;
            _scormProcessingService = scormProcessingService;
        }

        [HttpGet("/box/token")]
        public async Task<TokenResponse?> GetAccessToken(string userId)
        {
            var token = await _tokenService.GetToken(userId);
            return token;
        }

        [HttpGet("/box/profile")]
        public async Task<IActionResult> Profile(string userId)
        {
            var result = await _api.GetUserProfileAsync(userId);
            return Content(result ?? "Token invalid", "application/json");
        }

        [HttpPost("/box/upload")]
        public async Task<IActionResult> Upload(string userId, IFormFile file)
        {
            if (file == null) return BadRequest("No file");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var result = await _api.UploadFileAsync(userId, ms, file.FileName);
            if (!result.Success)
                return BadRequest(result.Error);  // VALID HERE

            return Ok(result.Data);
        }

        [HttpPost("/box/upload-version/{fileId}")]
        public async Task<IActionResult> UploadVersion(string fileId, string userId, IFormFile file)
        {
            if (file == null)
                return BadRequest("No file provided");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var result = await _api.UploadNewVersionAsync(userId, fileId, ms, file.FileName);
            if (!result.Success)
                return BadRequest(result.Error);  // VALID HERE

            return Ok(result.Data);
        }

        [HttpPost("/box/upload-large")]
        public async Task<IActionResult> UploadLargeFile(string userId, IFormFile file)
        {
            if (file == null) return BadRequest("File missing");

            var result = await _api.UploadLargeFileAsync(userId, file);
            if (!result.Success)
                return BadRequest(result.Error);  // VALID HERE

            return Ok(result.Data);
        }

        [HttpGet("/box/create-upload-session")]
        public async Task<BoxUploadSessionResponse> CreateUploadSession(
                                                            string userId, string fileName, long fileSize)
        {
            var response = await _api.CreateUploadSessionAsync(userId, fileName, fileSize);
            return response;
        }

        [HttpPost("/box/commit-upload")]
        public async Task<IActionResult> CommitUpload(string userId, string sessionId, [FromBody] CommitRequest body)
        {
            var result = await _api.CommitUploadAsync(userId, sessionId, body.Digest, body.Parts);
            if (!result.Success)
                return BadRequest(result.Error);  // VALID HERE

            return Ok(result.Data);
        }


        [HttpGet("/box/file/{id}")]
        public async Task<IActionResult> FileInfo(string id, string userId)
        {
            var result = await _api.GetFileInfoAsync(userId, id);
            return Content(result ?? "Unable to fetch file info", "application/json");
        }

        [HttpGet("/box/download/{fileId}")]
        public async Task<IActionResult> Download(string fileId, string userId)
        {
            var fileName = await _api.GetFileNameAsync(userId, fileId);
            var response = await _api.DownloadFileAsync(userId, fileId);

            return File(
                response.Stream,
                response.ContentType,
                fileName
            );
        }

        [HttpGet("/box/scorm-process/{fileId}")]
        public async Task<IActionResult> ProcessScorm(string fileId, string userId)
        {
            await _scormProcessingService.ProcessScormAsync(userId, fileId);
            return Ok("SCORM processing completed");
        }

        //[HttpPut("box/files/{fileId}/tags")]
        //public async Task<IActionResult> AddOrUpdateTags(string userId, string fileId, [FromBody] List<string> tags)
        //{
        //    var success = await _api.UpdateTagsAsync(userId, fileId, tags);

        //    if (!success)
        //        return BadRequest("Failed to update tags");

        //    return Ok(new { fileId, tags, message = "Tags updated successfully" });
        //}

    }
}
