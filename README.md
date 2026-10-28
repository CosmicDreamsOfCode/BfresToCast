# BfresToCast
Simple program to extract Nintendo BFRES models to [Cast](https://github.com/dtzxporter/cast) and textures to PNG (or DDS if HDR)

Usage:
Drag and drop BFRES or SARC files that contain BFRES onto the executable or set up a file association.

## Support
- ✔️ Uncompressed files
- ✔️ Zstd compressed files (.bfres.zs)
- ✔️ SARC or Yaz0 packed files (.pack, .szs, .sbfres)

## Tested with:
- Splatoon 1 (.Pack, .szs, works)
- Splatoon 2 (.szs, works)
- Splatoon 3 (.bfres.zs, works)
- Paper Mario TTYD (.bfres.zst, works)
- Mario Wonder (.bfres.zs, works)
- Tears of the Kingdom (does not work, requires external strings file, may not support)

## BfresToCast makes use of the following:
- [Astc-Encoder-csharp](https://github.com/Ash39/Astc-Encoder-csharp/tree/2638331143a786f9e3a9e1e7916639d39877cfde)
- [BCnEncoder.Net](https://github.com/Nominom/BCnEncoder.NET)
- [BCnEncoder.Net](https://github.com/Nominom/BCnEncoder.NET)
- [BfresLibrary](https://github.com/KillzXGaming/BfresLibrary)
- [Cast.NET](https://github.com/Scobalula/Cast.NET)
- [ImageLibrary](https://github.com/KillzXGaming/ImageLibrary)
- [ImageSharp](https://github.com/SixLabors/ImageSharp)
- [SarcLibrary](https://github.com/EPD-Libraries/SarcLibrary)
- [SixLabors ImageSharp](https://github.com/SixLabors/ImageSharp)
- [Syroot BinaryData](https://gitlab.com/Syroot/BinaryData)
- [Syroot Maths](https://gitlab.com/Syroot/Maths)
- [Syroot NintenTools NSW Bntx](https://github.com/KillzXGaming/LegacySwitchLibraries)
- [ZstdNet](https://github.com/skbkontur/ZstdNet)
- [image_dds](https://github.com/ScanMountGoat/image_dds)
- [tegra_swizzle](https://github.com/KillzXGaming/tegra_swizzle)


## Special thanks:
- KillzXGaming for easy to use libraries and being super helpful overall during this project and others :)

