using Aspose.Html;
using Aspose.Html.Converters;
using Aspose.Html.Saving;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Net.Http.Headers;

namespace ECM_API.Services
{
    public class ScormProcessingService
    {
        private readonly IWebHostEnvironment _env;
        private readonly BoxApiService _boxApiService;

        public ScormProcessingService(IWebHostEnvironment env, BoxApiService boxApiService)
        {
            _env = env;
            _boxApiService = boxApiService;
        }

        public async Task ProcessScormAsync(string userId, string fileId)
        {
            var file = await _boxApiService.DownloadFileAsync(userId, fileId);
            var fileName = await _boxApiService.GetFileNameAsync(userId, fileId);
            fileName = fileName.Replace(".zip", "");

            var zipStream = file.Stream;
            var folderId = await _boxApiService.CreateOrGetFolderAsync(userId, fileName);
            string extractPath = Path.Combine(_env.ContentRootPath, "temp", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractPath);

            // Extract SCORM
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    string entryPath = Path.Combine(extractPath, entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(entryPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(entryPath)!);

                    using var entryStream = entry.Open();
                    using var outFile = File.Create(entryPath);

                    await entryStream.CopyToAsync(outFile);
                }
            }

            // Find HTML SCO pages
            var htmlFiles = Directory.GetFiles(extractPath, "*.htm*", SearchOption.AllDirectories);

            foreach (var htmlPath in htmlFiles)
            {
                string pdfPath = Path.ChangeExtension(htmlPath, ".pdf");

                // Aspose HTML Document
                using var document = new HTMLDocument(htmlPath);

                // Convert HTML -> PDF
                Converter.ConvertHTML(
                    document,
                    new PdfSaveOptions(),
                    pdfPath
                );

                // Upload to Box
                await using var pdfStream = File.OpenRead(pdfPath);
                await _boxApiService.UploadFileAsync(userId, pdfStream, Path.GetFileName(pdfPath), folderId);
            }

            // Cleanup
            Directory.Delete(extractPath, true);
        }

        public async Task<string> DownloadZipFromBoxAsync(string userId, string fileId, string localPath)
        {
            var response = await _boxApiService.DownloadFileAsync(userId, fileId);

            await using var fs = new FileStream(localPath, FileMode.Create);
            await response.Stream.CopyToAsync(fs);

            return localPath;
        }


        public string ExtractZip(string zipPath)
        {
            string extractDir = Path.Combine(
                Path.GetDirectoryName(zipPath)!,
                "unzipped_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(extractDir);

            ZipFile.ExtractToDirectory(zipPath, extractDir);

            return extractDir;
        }

        public async Task UploadDirectoryToBoxAsync(string userId, string localDir, string parentBoxFolderId)
        {
            // upload files
            foreach (var file in Directory.GetFiles(localDir))
            {
                await _boxApiService.UploadFileAutoAsync(userId, parentBoxFolderId, file);
            }

            // upload folders
            foreach (var dir in Directory.GetDirectories(localDir))
            {
                string folderName = Path.GetFileName(dir);

                string newBoxFolderId = await _boxApiService.CreateOrGetFolderAsync(
                    userId,
                    folderName, parentBoxFolderId);

                await UploadDirectoryToBoxAsync(userId, dir, newBoxFolderId);
            }
        }

        public async Task UnzipAndReupload(
                                            string userId,
                                            string zipFileId)
        {
            var fileName = await _boxApiService.GetFileNameAsync(userId, zipFileId);
            fileName = fileName.Replace(".zip", "");
            var targetBoxFolderId = await _boxApiService.CreateOrGetFolderAsync(userId, fileName);

            // 1️⃣ Create temp folder (SAFE ANYWHERE)
            string tempRoot = Path.Combine(Path.GetTempPath(), "box_unzip_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            string zipPath = Path.Combine(tempRoot, "input.zip");

            // 2️⃣ Download ZIP from Box
            await DownloadZipFromBoxAsync(userId, zipFileId, zipPath);

            // 3️⃣ Extract ZIP
            string extractDir = ExtractZip(zipPath);

            // 4️⃣ Re-upload extracted content preserving structure
            await UploadDirectoryToBoxAsync(userId, extractDir, targetBoxFolderId);
            
            // 5️⃣ Cleanup folder
            Directory.Delete(tempRoot, true);
        }
    }
}
