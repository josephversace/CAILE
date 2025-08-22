using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{
    public class ChunkData
    {
        public string Hash { get; set; }
        public byte[] Data { get; set; }
        public int Size { get; set; }
        public int Offset { get; set; }
    }

    public class DeduplicationResult
    {
        public string FileHash { get; set; }
        public long TotalSize { get; set; }
        public List<string> ChunkHashes { get; set; } = new();
        public List<ChunkData> UniqueChunks { get; set; } = new();
        public List<ChunkData> DuplicateChunks { get; set; } = new();
        public long BytesSaved { get; set; }
        public double DeduplicationRatio { get; set; }
    }
}
