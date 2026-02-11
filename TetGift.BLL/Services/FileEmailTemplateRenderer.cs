using TetGift.BLL.Interfaces;

namespace TetGift.BLL.Services
{
    public class FileEmailTemplateRenderer : IEmailTemplateRenderer
    {
        public string RenderOtp(string otp, int minutes)
        {
            // Try multiple possible paths
            var possiblePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "OtpEmail.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OtpEmail.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "OtpEmail.html")
            };

            string? path = null;
            foreach (var possiblePath in possiblePaths)
            {
                if (File.Exists(possiblePath))
                {
                    path = possiblePath;
                    break;
                }
            }

            if (path == null || !File.Exists(path))
            {
                var searchedPaths = string.Join(", ", possiblePaths);
                throw new FileNotFoundException(
                    $"Email template 'OtpEmail.html' not found. Searched paths: {searchedPaths}. " +
                    $"Current directory: {Directory.GetCurrentDirectory()}, " +
                    $"Base directory: {AppContext.BaseDirectory}");
            }

            var html = File.ReadAllText(path);

            return html.Replace("{{OTP}}", otp)
                       .Replace("{{MINUTES}}", minutes.ToString());
        }
    }
}
