using BfresLibrary;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using SarcLibrary;

namespace BfresToCast.Utils.Helpers;

public static class ExternalFileHelpers
{
    private static string GameRootDirectory = "";

    private static string ConfigName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameRoots.cfg");

    public static List<string> Config = new List<string>();

    public static ResFile ExternalStringsFile;

    public static byte[] ZstdDictionary;

    public static Dictionary<string,string> TextureRemaps = new Dictionary<string,string>();

    public static void SetUpExternalFiles(string path)
    {
        ProcessConfig(path);
        GetZstdDictionary(path);
        GetExternalBinaryStrings(path);
        GetTextureRemaps(path);
    }

    public static void ProcessConfig(string pathToSearch) // TODO: fix it adding to Config for every time this is called (oops)
    {
        // User can put paths in this config to automatch to texture folder
        string configName = Path.GetFullPath(ConfigName);
        if (File.Exists(configName))
        {
            foreach (string line in File.ReadLines(configName))
            {
                Config.Add(line);
            }

            foreach (string line in Config)
            {
                if (line.StartsWith("#"))
                    continue;

                if (pathToSearch.Contains(Path.GetDirectoryName(line)))
                {
                    GameRootDirectory = line;
                    break;
                }
            }
        }

        if (GameRootDirectory == "")
        {
            Console.WriteLine("Please provide the path to the root of your game dump. (Press Enter to skip)");

            GameRootDirectory = Console.ReadLine();

            if (GameRootDirectory == "")
            {
                GameRootDirectory = "!skip";
            }
        }
    }

    public static void SaveConfig(List<string> lines)
    {
        File.WriteAllLines(Path.GetFullPath(ConfigName), lines);
    }

    public static void GetZstdDictionary(string path)
    {
        if (ZstdDictionary == null)
        {
            string dictSarc = Path.Combine(GameRootDirectory, @"Pack\ZsDic.pack.zs");

            if (File.Exists(dictSarc))
            {
                byte[] data = File.ReadAllBytes(dictSarc);
                data = DecompressionHelpers.DecompressIfNeeded(data);

                Sarc sarc = Sarc.FromBinary(data);

                if (sarc.TryGetValue("zs.zsdic", out ArraySegment<byte> dictData))
                {
                    ZstdDictionary = dictData.ToArray();
                }
            }
        }
    }

    public static void GetExternalBinaryStrings(string path)
    {
        if (ExternalStringsFile == null)
        {
            string ebsPath = Path.Combine(GameRootDirectory, @"Shader\ExternalBinaryString.bfres.zs");

            if (File.Exists(ebsPath))
            {
                byte[] data = File.ReadAllBytes(ebsPath);
                data = DecompressionHelpers.DecompressIfNeeded(data);

                MemoryStream stream = new MemoryStream(data);
                ExternalStringsFile = new ResFile(stream, true);
            }
        }
    }

    public static void GetTextureRemaps(string path)
    {
        if (TextureRemaps.Count == 0)
        {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(GameRootDirectory, @"System\Gfx")))
            {
                if (Path.GetFileName(file).StartsWith("UnifyTex"))
                {
                    byte[] data = File.ReadAllBytes(file);
                    data = DecompressionHelpers.DecompressIfNeeded(data);

                    Byml byml = Byml.FromBinary(data);
                    BymlMap map = byml.GetMap();
                    foreach (string tex in map.Keys)
                    {
                        TextureRemaps.Add(tex, map[tex].GetString());
                    }
                    break;
                }
            }
        }
    }

    public static bool GetExternalTextures(ResFile modelRes, List<string> referencedTextures, string outputDir, out List<string> externalTextures)
    {
        bool success = true;
        externalTextures = new List<string>();

        // Check if a lookup BYML exists
        Byml byml = null;
        if (modelRes.ExternalFiles.TryGetValue("textureTable.byml", out ExternalFile file))
        {
            byml = Byml.FromBinary(file.Data);
            referencedTextures.Clear();

            foreach (Byml tex in byml.GetMap().Values)
            {
                referencedTextures.Add(tex.GetString());
            }
        }

        bool refButNoTex = referencedTextures.Count > 0 && modelRes.Textures.Count == 0;

        if ((modelRes.VersionMajor >= 10 && refButNoTex) || byml != null)
        {
            if (GameRootDirectory != "!skip")
            {
                List<string> remappedTextures = new List<string>();

                foreach (string texture in referencedTextures)
                {
                    string texName = texture;
                    if (TextureRemaps.ContainsKey(texture))
                    {
                        texName = TextureRemaps[texName];
                        remappedTextures.Add($"{texture} -> {texName}");
                    }

                    bool hasVariations = int.TryParse(Path.GetExtension(texName).TrimStart('.'), out int index);

                    string baseTextureName = Path.GetFileNameWithoutExtension(texName);
                    string textureDirectory = Path.Combine(GameRootDirectory, "Tex");

                    string fileName = Path.Combine(textureDirectory, $"{texName}.bntx.zs");

                    if (hasVariations)
                    {
                        foreach (string variant in Directory.GetFiles(textureDirectory, $"*{baseTextureName}*"))
                        {
                            externalTextures.Add(variant);
                        }
                        continue;
                    }

                    if (File.Exists(fileName))
                    {
                        externalTextures.Add(fileName);
                        continue;
                    }

                    Console.WriteLine($"Failed to find external texture : {texName}");

                    success = false;
                }

                if (success && remappedTextures.Count > 0)
                {
                    File.WriteAllLines(Path.Combine(outputDir, "RemappedTextures.txt"), remappedTextures);
                }

                if (success && !Config.Contains(GameRootDirectory))
                {
                    // Add this path to the config if it's not already there
                    Config.Add(GameRootDirectory);
                    SaveConfig(Config);
                }
            }
        }

        return success;
    }
}