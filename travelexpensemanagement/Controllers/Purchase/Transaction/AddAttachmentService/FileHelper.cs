using System.Text.RegularExpressions;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.AddAttachmentService
{

    public class FileHelper
    {
        public static async Task<List<SaveFileModel>> SaveBase64FilesAsync(
            List<(string FileName, string Base64Content)> files,
            string folderUnderAttachments
        )
        {
            var savedFiles = new List<SaveFileModel>();
            string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Attachments", folderUnderAttachments);

            if (!Directory.Exists(rootPath))
                Directory.CreateDirectory(rootPath);

            foreach (var file in files)
            {
                string cleanBase64 = Regex.Replace(file.Base64Content, @"^data:image\/[a-zA-Z]+;base64,", "");
                byte[] fileBytes = Convert.FromBase64String(cleanBase64);

                string filePath = Path.Combine(rootPath, file.FileName);
                await File.WriteAllBytesAsync(filePath, fileBytes);

                savedFiles.Add(new SaveFileModel
                {
                    FileName = file.FileName,
                    RelativePath = Path.Combine("Attachments", folderUnderAttachments, file.FileName).Replace("\\", "/"),
                    FullPath = filePath,
                    FileBytes = fileBytes
                });
            }

            return savedFiles;
        }
    }
}
