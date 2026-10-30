using BfresLibrary;
using BfresLibrary.Switch;
using BfresToCast.Utils;
using BymlLibrary;
using ImageLibrary;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class BfresProcessor
{
    public static string ExternalTextureDirectory = "";

    public static bool Process(ResFile modelRes, string currentFile, bool isSARCFile)
    {
        bool success = true;
        Console.WriteLine($"Processing {modelRes.Name}...");

        string outputDir = Path.GetDirectoryName(currentFile);

        if (!isSARCFile)
        {
            // Bezel Engine names BFRES files with the FMDB extension and the name inside the BFRES has it too
            // Conveniently this also handles newer games which have the project AND model name with a . seperator
            outputDir = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(modelRes.Name));
        }
        else
        {
            // LunchPack games often have the BFRES just titled "output", Pull the name from the name of the SARC instead in that case
            if (modelRes.Name.ToLower() == "output")
            {
                // Have to remove the targets and extension
                currentFile = currentFile.Replace(Path.GetExtension(currentFile), "");
                outputDir = $@"{currentFile.Replace(".Nin_NX_NVN", "")}";
            }
            else
            {
                outputDir = Path.Combine(outputDir, modelRes.Name);
            }
        }

        List<string> referencedTextures = new List<string>();

        if (modelRes.Models.Count > 0)
        {
            Directory.CreateDirectory(outputDir);

            // Process all models in this BFRES
            foreach (Model model in modelRes.Models.Values)
            {
                try
                {
                    success &= ModelProcessor.Process(model, outputDir, modelRes.ByteOrder);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process model {model.Name}: {e}");
                    success = false;
                }

                // Newer games might not have textures in the BFRES so we have to gather references
                if (modelRes.VersionMajor >= 10)
                {
                    foreach (Material mat in model.Materials.Values)
                    {
                        foreach (TextureRef texRef in mat.TextureRefs)
                        {
                            if (!referencedTextures.Contains(texRef.Name))
                            {
                                referencedTextures.Add(texRef.Name);
                            }
                        }
                    }
                }
            }
        }

        try
        {
            success &= TextureProcessor.Process(modelRes, referencedTextures, outputDir);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Texture processing failed: {e}");
            success = false;
        }

        return success;
    }
}