using System.Text.Json;

namespace Synapse
{
    public class NoteParser
    {
        public static Dictionary<string, string> ParseFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The file was not found: {path}", path);
            }

            string content = File.ReadAllText(path).Trim();

            // Step 1: Detect JSON vs plain text
            if (content.StartsWith("{") && content.Contains("\"data\""))
            {
                try
                {
                    var doc = JsonDocument.Parse(content);
                    content = doc.RootElement.GetProperty("data").GetString();
                }
                catch
                {
                    throw new FormatException("Invalid JSON format.");
                }
            }

            // Step 2: Parse the text content
            return ParsePatientData(content);
        }

        private static Dictionary<string, string> ParsePatientData(string text)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                    data[parts[0].Trim()] = parts[1].Trim();
            }

            return data;
        }
    }
}