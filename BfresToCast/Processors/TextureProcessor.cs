using BfresLibrary;
using BfresLibrary.Switch;
using BfresToCast.Utils;
using BymlLibrary;
using ImageLibrary;
using ImageLibrary.Formats.Encoders;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class TextureProcessor
{
    public static string ExternalTextureDirectory = "";

    public static bool Process(ResFile modelRes, List<string> referencedTextures, string outputDir)
    {
        bool success = true;

        // Prepare external textures
        List<string> externalTextures = new List<string>();
        if (modelRes.VersionMajor >= 10 && referencedTextures.Count > 0 && modelRes.Textures.Count == 0)
        {
            if (ExternalTextureDirectory == "")
            {
                Console.WriteLine("Please provide a path to search for textures. (Press Enter to skip, Choice will be remembered for this instance)");

                ExternalTextureDirectory = Console.ReadLine();

                if (ExternalTextureDirectory == "")
                {
                    ExternalTextureDirectory = "!skip";
                }
            }

            if (ExternalTextureDirectory != "!skip")
            {
                // Check if a lookup BYML exists
                Byml byml = null;
                if (modelRes.ExternalFiles.TryGetValue("textureTable.byml", out ExternalFile file))
                {
                    byml = Byml.FromBinary(file.Data);
                }

                foreach (string texture in referencedTextures)
                {
                    // Typical case
                    string fileName = Path.Combine(ExternalTextureDirectory, $"{texture}.bntx.zs");
                    if (File.Exists(fileName))
                    {
                        externalTextures.Add(fileName);
                        continue;
                    }

                    if (byml != null)
                    {
                        // Lookup byml
                        string texturefile = byml.GetMap()[texture].GetString();
                        texturefile = Path.Combine(ExternalTextureDirectory, $"{texturefile}.bntx.zs");

                        if (File.Exists(texturefile))
                        {
                            externalTextures.Add(texturefile);
                            continue;
                        }
                    }

                    Console.WriteLine($"Failed to find external texture : {texture}");

                    success = false;
                }
            }
        } // if (modelRes.VersionMajor >= 10 && referencedTextures.Count > 0 && modelRes.Textures.Count == 0)

        if (externalTextures.Count > 0 || modelRes.Textures.Count > 0)
        {
            string textureOutputDir = Path.Combine(outputDir, "Textures");
            Directory.CreateDirectory(textureOutputDir);

            if (modelRes.IsPlatformSwitch)
            {
                if (modelRes.ExternalFiles.TryGetValue("textures.bntx", out ExternalFile file) && file.LoadedFileData is BntxFile bntx)
                {
                    success &= ProcessBNTX(bntx, textureOutputDir);
                }

                if (externalTextures.Count > 0)
                {
                    foreach (string texture in externalTextures)
                    {
                        MemoryStream stream = new MemoryStream();
                        byte[] data = File.ReadAllBytes(texture);
                        data = DecompressionHelpers.DecompressIfNeeded(data);
                        stream = new MemoryStream(data);

                        BntxFile exBntx = new BntxFile(stream);
                        success &= ProcessBNTX(exBntx, textureOutputDir);
                    }
                }
            }
            else
            {
                success &= ProcessWiiU(modelRes, textureOutputDir);
            }
        }

        return success;
    }

    public static bool ProcessBNTX(BntxFile bntx, string outDir)
    {
        ExportSettings exportSettings = new ExportSettings { UseComponentChannels = true };
        bool isOrin = bntx.PlatformTarget == "Ounc";

        foreach (Texture texture in bntx.Textures)
        {
            SwitchTexture switchTexture = new SwitchTexture(bntx, texture);
            GenericTextureBase generic = TextureUtils.SwitchTextureToGeneric(switchTexture, isOrin);

            if (TextureUtils.IsHDR(switchTexture.Format) || generic.Type == TextureType.TextureCube)
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.dds"), exportSettings);
            }
            else
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.png"), exportSettings);
            }
            
            Console.WriteLine($"Saved texture : {switchTexture.Name}");
        }
        
        return true;
    }

    public static bool ProcessWiiU(ResFile modelRes, string outDir)
    {
        ExportSettings exportSettings = new ExportSettings { UseComponentChannels = true };

        foreach (BfresLibrary.WiiU.Texture gx2Tex in modelRes.Textures.Values)
        {
            GenericTextureBase generic = TextureUtils.GX2TextureToGeneric(gx2Tex);
            generic.Export(Path.Combine(outDir, $"{gx2Tex.Name}.png"), exportSettings);

            Console.WriteLine($"Saved texture : {gx2Tex.Name}");
        }

        return true;
    }
}