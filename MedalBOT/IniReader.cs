using System;
using System.Collections.Generic;
using System.IO;

namespace MedalBot
{
    public static class IniReader
    {
        public static Dictionary<string, string> Read(string path, string section)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return result;

            string currentSection = "";
            foreach (var line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith(";") || trimmed == "") continue;
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed[1..^1];
                    continue;
                }
                if (currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                {
                    var kv = trimmed.Split('=', 2);
                    if (kv.Length == 2)
                        result[kv[0].Trim()] = kv[1].Trim();
                }
            }
            return result;
        }
    }
}