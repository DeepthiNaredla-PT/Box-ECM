using ECM_API.Models;
using System.IO.Compression;
using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net;

namespace ECM_API.Services
{
    public class BoxApiService
    {
        private readonly IHttpClientFactory _http;
        private readonly BoxAuthService _auth;
        private readonly Store _store;
        private readonly string _parentFolderId = "354218997349";
        public BoxApiService(IHttpClientFactory http, BoxAuthService auth, Store store)
        {
            _http = http;
            _auth = auth;
            _store = store;
        }

        public async Task<string?> EnsureAccessTokenAsync(string userId)
        {
            if (!_store.TryGetTokens(userId, out var token))
                return null;

            if (DateTime.UtcNow >= token.ExpiresAt)
            {
                var refreshed = await _auth.RefreshTokenAsync(token);
                if (refreshed == null) return null;

                _store.UpdateTokens(userId, refreshed);
                return refreshed.access_token;
            }

            return token.access_token;
        }

        public async Task<string?> GetUserProfileAsync(string userId)
        {
            var client = await GetClient(userId);

            var result = await client.GetAsync("https://api.box.com/2.0/users/me");
            return await result.Content.ReadAsStringAsync();
        }

        public async Task<APIResult<FileUploadResponse>> UploadFileAsync(
            string userId, Stream fileStream, string fileName, string parentFolderId = "")
        {
            var client = await GetClient(userId);

            var content = new MultipartFormDataContent();

            var attrs = new { name = fileName, parent = new { id = !string.IsNullOrWhiteSpace(parentFolderId) ? parentFolderId : _parentFolderId } };
            content.Add(new StringContent(JsonSerializer.Serialize(attrs)), "attributes");

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);           

            var result = await client.PostAsync("https://upload.box.com/api/2.0/files/content", content);

            var jsonString = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                var error = await ParseBoxError(result);

                // Return the Box error model directly to UI
                return new APIResult<FileUploadResponse>
                {
                    Success = false,
                    Error = error
                };

            }
            var boxResponse = JsonSerializer.Deserialize<BoxUploadResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new APIResult<FileUploadResponse>
            {
                Success = true,
                Data = new FileUploadResponse { Id = boxResponse?.Entries?.FirstOrDefault()?.Id, Name = boxResponse?.Entries?.FirstOrDefault()?.Name }
            };
        }

        public async Task<APIResult<FileUploadResponse>> UploadNewVersionAsync(string userId, string fileId, Stream fileStream, string fileName)
        {
            var client = await GetClient(userId);

            var content = new MultipartFormDataContent();

            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", fileName);

            var result = await client.PostAsync(
                $"https://upload.box.com/api/2.0/files/{fileId}/content",
                content
            );

            var jsonString = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                var error = await ParseBoxError(result);

                // Return the Box error model directly to UI
                return new APIResult<FileUploadResponse>
                {
                    Success = false,
                    Error = error
                };

            }
            var boxResponse = JsonSerializer.Deserialize<BoxUploadResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new APIResult<FileUploadResponse>
            {
                Success = true,
                Data = new FileUploadResponse { Id = boxResponse?.Entries?.FirstOrDefault()?.Id, Name = boxResponse?.Entries?.FirstOrDefault()?.Name }
            };
        }

        public async Task<string?> GetFileInfoAsync(string userId, string fileId)
        {
            var client = await GetClient(userId);

            var result = await client.GetAsync($"https://api.box.com/2.0/files/{fileId}");
            return await result.Content.ReadAsStringAsync();
        }

        public async Task<string> GetFileNameAsync(string userId, string fileId)
        {
            var metaJson = await GetFileInfoAsync(userId, fileId);

            var doc = JsonDocument.Parse(metaJson);

            string fileName = doc.RootElement.GetProperty("name").GetString();
            return fileName;
        }

        public async Task<FileDownload> DownloadFileAsync(string userId, string fileId)
        {
            FileDownload fileDownload = new FileDownload();
            
            var client = await GetClient(userId);

            // Follow redirects automatically
            client.DefaultRequestHeaders.Add("Accept", "application/octet-stream");

            var response = await client.GetAsync(
                $"https://api.box.com/2.0/files/{fileId}/content",
                HttpCompletionOption.ResponseHeadersRead
            );

            response.EnsureSuccessStatusCode();

            fileDownload.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            fileDownload.Stream = await response.Content.ReadAsStreamAsync();

            return fileDownload;
        }

        public async Task<APIResult<FileUploadResponse>> UploadLargeFileAsync(
    string userId, IFormFile file, int chunkSize = 8 * 1024 * 1024)
        {
            long fileSize = file.Length;
            string fileName = file.FileName;

            // 1. Create upload session
            var sessionJson = await CreateUploadSessionAsync(userId, fileName, fileSize);
            string sessionId = sessionJson.Id;

            byte[] buffer = new byte[chunkSize];
            long uploaded = 0;

            var parts = new List<BoxPart>();

            using (var stream = file.OpenReadStream())
            {
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
                {
                    long start = uploaded;
                    long end = uploaded + bytesRead - 1;

                    int retries = 3;

                    while (retries > 0)
                    {
                        try
                        {
                            // Upload chunk and get part metadata
                            var part = await UploadPartAsync(
                                userId,
                                sessionId,
                                buffer,
                                bytesRead,
                                start,
                                end,
                                fileSize
                            );

                            parts.Add(part);
                            break;
                        }
                        catch (Exception)
                        {
                            retries--;
                            if (retries == 0)
                                throw;
                        }
                    }

                    uploaded += bytesRead;
                }
            }

            // 2. Compute SHA1 digest for complete file (Box requirement)
            string sha1Base64;
            using (var sha1 = SHA1.Create())
            using (var fileStream = file.OpenReadStream())
            {
                var hash = sha1.ComputeHash(fileStream);
                sha1Base64 = Convert.ToBase64String(hash);
            }

            // 3. Commit upload session with collected parts
            var commitResult = await CommitUploadAsync(userId, sessionId, sha1Base64, parts);

            return commitResult;
        }

        public async Task<bool> UpdateTagsAsync(string userId, string fileId, List<string> tags)
        {
            var client = await GetClient(userId);

            var body = new
            {
                tags = tags
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync(
                $"https://api.box.com/2.0/files/{fileId}",
                content
            );

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateMetadataAsync(
                                                    string userId,
                                                    string fileId,
                                                    string templateKey,
                                                    string scope,
                                                    List<BoxMetadataOp> operations)
        {
            var client = await GetClient(userId);

            var json = JsonSerializer.Serialize(operations);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            var url = $"https://api.box.com/2.0/files/{fileId}/metadata/{scope}/{templateKey}";

            var response = await client.PutAsync(url, content);

            return response.IsSuccessStatusCode;
        }

        public async Task<BoxUploadSessionResponse?> CreateUploadSessionAsync(string userId, string fileName, long fileSize)
        {
            var client = await GetClient(userId);

            var body = new
            {
                file_name = fileName,
                file_size = fileSize,
                folder_id = _parentFolderId
            };

            var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://upload.box.com/api/2.0/files/upload_sessions", json);
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BoxUploadSessionResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        public async Task<BoxPart?> UploadPartAsync(
                                                        string userId,
                                                        string sessionId,
                                                        byte[] buffer,
                                                        int bytesRead,
                                                        long start,
                                                        long end,
                                                        long totalSize)
        {
            var client = await GetClient(userId);

            // compute digest for this chunk
            string digest = ComputeSHA1Base64(buffer, bytesRead);

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"https://upload.box.com/api/2.0/files/upload_sessions/{sessionId}")
            {
                Content = new ByteArrayContent(buffer, 0, bytesRead)
            };

            request.Content.Headers.Add("Digest", $"sha={digest}");

            request.Content.Headers.ContentRange =
                new System.Net.Http.Headers.ContentRangeHeaderValue(start, end, totalSize);

            var response = await client.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            var partResp = JsonSerializer.Deserialize<BoxUploadPartResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            return partResp.Part;
        }

        public async Task<APIResult<FileUploadResponse>> CommitUploadAsync(string userId, string sessionId, string sha1Digest, List<BoxPart> parts)
        {
            var client = await GetClient(userId);
            client.DefaultRequestHeaders.Add("Digest", $"sha={sha1Digest}");

            var body = new
            {
                parts = parts.Select(x => new
                {
                    part_id = x.Part_Id,
                    offset = x.Offset,
                    size = x.Size
                })
            };
            var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var result = await client.PostAsync(
                $"https://upload.box.com/api/2.0/files/upload_sessions/{sessionId}/commit",
                json);

            //return await response.Content.ReadAsStringAsync();

            var jsonString = await result.Content.ReadAsStringAsync();
            if (!result.IsSuccessStatusCode)
            {
                var error = await ParseBoxError(result);

                // Return the Box error model directly to UI
                return new APIResult<FileUploadResponse>
                {
                    Success = false,
                    Error = error
                };

            }
            var boxResponse = JsonSerializer.Deserialize<BoxUploadResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return new APIResult<FileUploadResponse>
            {
                Success = true,
                Data = new FileUploadResponse { Id = boxResponse?.Entries?.FirstOrDefault()?.Id, Name = boxResponse?.Entries?.FirstOrDefault()?.Name }
            };
        }

        public async Task<string> CreateOrGetFolderAsync(string userId, string folderName)
        {
            var client = await GetClient(userId);

            var body = new
            {
                name = folderName,
                parent = new { id = _parentFolderId }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.box.com/2.0/folders", content);

            var responseJson = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement.GetProperty("id").GetString();
            }

            // HANDLE 409 "item_name_in_use"
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var conflictInfo = JsonSerializer.Deserialize<BoxErrorResponse>(
                    responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (conflictInfo?.Context_Info?.Conflicts?.Id != null)
                {
                    // folder already exists → return existing id
                    return conflictInfo.Context_Info.Conflicts.Id;
                }
            }

            throw new Exception("Folder creation failed: " + responseJson);
        }

        private string ComputeSHA1Base64(byte[] data, int length)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(data, 0, length);
            return Convert.ToBase64String(hash);
        }

        private async Task<BoxErrorResponse> ParseBoxError(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BoxErrorResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<HttpClient> GetClient(string userId)
        {
            var token = await EnsureAccessTokenAsync(userId);

            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
