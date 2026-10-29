using BfresLibrary;
using BfresLibrary.Switch;
using BfresToCast.Utils;
using ImageLibrary;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class BfresProcessor
{
    public static bool Process(ResFile modelRes, string currentFile, bool isSARCFile)
    {
        bool success = true;
        string dir = "";
        if (!isSARCFile)
        {
            // This is stupid, Bezel engine bfres uses the fmdb extension and the name inside the bfres has it too...
            dir = $@"{Path.GetDirectoryName(currentFile)}/{Path.GetFileNameWithoutExtension(modelRes.Name)}";
        }
        else
        {
            //why did they have to just name the bfres "output" half the time
            if (modelRes.Name.ToLower() == "output")
            {
                currentFile = currentFile.Replace(Path.GetExtension(currentFile), "");
                dir = $@"{currentFile.Replace(".Nin_NX_NVN", "")}";
            }
            else
            {
                dir = $@"{Path.GetDirectoryName(currentFile)}/{modelRes.Name}";
            }
        }

        string texDir = $@"{dir}/Textures";
        if (modelRes.Models.Count > 0)
        {
            Directory.CreateDirectory(dir);
        }
        if (modelRes.Textures.Count > 0 && modelRes.IsPlatformSwitch) // TODO: Support external textures
        {
            Directory.CreateDirectory(texDir);
        }

        Console.WriteLine($"Processing {modelRes.Name}...");

        if (modelRes.IsPlatformSwitch)
        {
            if (modelRes.ExternalFiles.TryGetValue("textures.bntx", out ExternalFile file) && file.LoadedFileData is BntxFile bntx)
            {
                TextureProcessor.ProcessBNTX(bntx, texDir);
            }
            // TODO: Support external textures
        }
        else
        {
            Console.WriteLine("Wii U textures are unsupported at this time.");
            return false;
        }
        
        foreach (Model model in modelRes.Models.Values)
        {
            success &= ModelProcessor.Process(model, dir, modelRes.ByteOrder);
        }
        
        return success;
    }
}