using System;
using System.Collections.Generic;
using System.IO;

/* These are re-implementations of the Windows version of INI filing. */
public sealed class Ini
{
    public class Section
    {
        public string Name { get; set; }
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        public Section(string name)
        {
            Name = name;
        }
    }

    public List<Section> Sections { get; } = new List<Section>();

    public void ReadFromFile(string fileName)
    {
        if (!File.Exists(fileName))
            return;

        using (var reader = new StreamReader(fileName))
        {
            Section currentSection = null;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    currentSection = new Section(GetSectionName(line));
                    Sections.Add(currentSection);
                }
                if (line.Contains("=") && currentSection != null)
                {
                    string[] keyValuePair = line.Split('=');
                    if (keyValuePair.Length == 2)
                        currentSection.Values.Add(keyValuePair[0], keyValuePair[1]);
                }
            }
        }
    }

    public void WriteToFile(string fileName)
    {
        using (var writter = new StreamWriter(fileName))
        {
            foreach (var section in Sections)
            {
                writter.WriteLine($"[{section.Name}]");
                foreach (var keyValuePair in section.Values)
                    writter.WriteLine($"{keyValuePair.Key}={keyValuePair.Value}");
            }
        }
    }

    public Section GetSection(string sectionName)
    {
        return Sections.Find(s => s.Name == sectionName);
    }

    private static string GetSectionName(string rawLine)
    {
        return rawLine.Substring(1, rawLine.LastIndexOf(']') - 1);
    }

    public static void WriteINIString(string sectionName, string key, string value, string filename)
    {
        Ini ini = new Ini();
        ini.ReadFromFile(filename);

        Section section = ini.GetSection(sectionName);
        if (section == null)
        {
            section = new Section(sectionName);
            ini.Sections.Add(section);
        }

        section.Values[key] = value;

        ini.WriteToFile(filename);
    }

    public static string GetINIString(string sectionName, string key, string def, string filename)
    {
        Ini ini = new Ini();
        ini.ReadFromFile(filename);

        Section section = ini.GetSection(sectionName);
        if (section != null && section.Values.ContainsKey(key))
            return section.Values[key];

        return def;
    }

    public static int GetINIInt(string section, string key, int def, string filename)
    {
        string value = GetINIString(section, key, def.ToString(), filename);
        return int.Parse(value);
    }

    public static void WriteINIInt(string section, string key, int value, string filename)
    {
        WriteINIString(section, key, value.ToString(), filename);
    }

    public static bool GetINIBool(string section, string key, bool def, string filename)
    {
        string value = GetINIString(section, key, def.ToString(), filename);
        return bool.Parse(value);
    }

    public static void WriteINIBool(string section, string key, bool value, string filename)
    {
        WriteINIString(section, key, value.ToString(), filename);
    }
}
