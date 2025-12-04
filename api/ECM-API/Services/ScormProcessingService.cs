using Aspose.Html;
using Aspose.Html.Converters;
using Aspose.Html.Saving;
using System.IO.Compression;

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
    }
}
