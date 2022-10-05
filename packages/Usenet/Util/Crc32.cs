﻿namespace Usenet.Util
{
    /// <summary>
    /// Utiliy class for calculating CRC-32 checksums.
    /// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
    /// </summary>
    internal static class Crc32
    {
        private const uint polynomial = 0xEDB88320;
        private const uint seed = 0xFFFFFFFF;
        private static readonly uint[] lookupTable;

        static Crc32()
        {
            lookupTable = CreateLookupTable();
        }

        public static uint CalculateChecksum(byte[] buffer)
        {
            Guard.ThrowIfNull(buffer, nameof(buffer));
            
            uint value = seed;
            foreach (byte b in buffer)
            {
                value = (value >> 8) ^ lookupTable[(value & 0xFF) ^ b];
            }
            return value ^ seed;
        }

        public static uint Initialize() => seed;

        public static uint Calculate(uint value, int @byte) => 
            (value >> 8) ^ lookupTable[(value & 0xFF) ^ @byte];

        public static uint Finalize(uint value) => value ^ seed;

        private static uint[] CreateLookupTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (var j = 0; j < 8; j++)
                {
                    entry = (entry & 1) == 1 ? (entry >> 1) ^ polynomial : entry >> 1;
                }
                table[i] = entry;
            }
            return table;
        }
    }
}
