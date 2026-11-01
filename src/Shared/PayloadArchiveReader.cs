using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace MuseDashSteamPresence
{
    internal static class PayloadArchiveReader
    {
        // Read managed resources as data. Never load the main mod assembly or
        // resolve game/Unity types during the early plugin's callback.
        internal static byte[] Read(string mainFile)
        {
            using (var file = File.OpenRead(mainFile))
            using (var pe = new PEReader(file))
            {
                if (!pe.HasMetadata || pe.PEHeaders.CorHeader == null) throw new InvalidDataException("Not a managed mod DLL.");
                var directory = pe.PEHeaders.CorHeader.ResourcesDirectory;
                if (directory.Size <= 0 || directory.Size > 32*1024*1024) throw new InvalidDataException("Invalid resource directory.");
                var metadata = pe.GetMetadataReader();
                foreach (var handle in metadata.ManifestResources)
                {
                    var resource = metadata.GetManifestResource(handle);
                    if (metadata.GetString(resource.Name) != PayloadDeployment.ResourceName || !resource.Implementation.IsNil) continue;
                    byte[] content = pe.GetSectionData(directory.RelativeVirtualAddress).GetContent(0,directory.Size).ToArray();
                    int offset = checked((int)resource.Offset);
                    if (offset < 0 || offset > content.Length-4) throw new InvalidDataException("Invalid resource offset.");
                    int length = BitConverter.ToInt32(content,offset);
                    if (length < 0 || length > content.Length-offset-4) throw new InvalidDataException("Invalid resource size.");
                    var result = new byte[length]; Buffer.BlockCopy(content,offset+4,result,0,length); return result;
                }
                throw new InvalidDataException("Embedded deployment payload absent.");
            }
        }
    }
}
