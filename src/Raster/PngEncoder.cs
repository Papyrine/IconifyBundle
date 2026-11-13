namespace IconifyBundle.Raster;

/// <summary>
/// Writes 8-bit RGBA PNGs. The deflate stream is produced here (LZ77 with the fixed Huffman codes) rather
/// than by <c>System.IO.Compression</c>, whose output differs between zlib versions and runtimes: the same
/// icon must give the same bytes in the generator on .NET Framework (Visual Studio), on .NET (the CLI), and
/// in the build task, so builds stay deterministic and snapshots stay stable.
/// </summary>
static class PngEncoder
{
    static readonly byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static byte[] Encode(byte[] rgba, int width, int height)
    {
        using var output = new MemoryStream();
        output.Write(signature, 0, signature.Length);

        var header = new byte[13];
        BigEndian(header, 0, (uint)width);
        BigEndian(header, 4, (uint)height);
        // Bit depth 8, colour type 6 (RGBA), deflate, adaptive filtering, no interlace.
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        // 96 DPI (3780 pixels per metre), so System.Drawing and WPF draw the image at its pixel size.
        var physical = new byte[9];
        BigEndian(physical, 0, 3780);
        BigEndian(physical, 4, 3780);
        physical[8] = 1;
        WriteChunk(output, "pHYs", physical);

        WriteChunk(output, "IDAT", Zlib.Compress(Filter(rgba, width, height)));
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    // Each row gets the filter whose output has the smallest sum of absolute (signed) bytes, the usual
    // heuristic for picking what compresses best.
    static byte[] Filter(byte[] rgba, int width, int height)
    {
        const int bytesPerPixel = 4;
        var stride = width * bytesPerPixel;
        var result = new byte[(stride + 1) * height];
        var candidate = new byte[stride];
        var best = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var row = y * stride;
            var previous = row - stride;
            var bestType = 0;
            var bestScore = long.MaxValue;
            for (var type = 0; type <= 4; type++)
            {
                long score = 0;
                for (var x = 0; x < stride; x++)
                {
                    int left = 0, up = 0, upLeft = 0;
                    if (x >= bytesPerPixel)
                    {
                        left = rgba[row + x - bytesPerPixel];
                    }

                    if (y > 0)
                    {
                        up = rgba[previous + x];
                        if (x >= bytesPerPixel)
                        {
                            upLeft = rgba[previous + x - bytesPerPixel];
                        }
                    }

                    var value = rgba[row + x];
                    var filtered = type switch
                    {
                        1 => value - left,
                        2 => value - up,
                        3 => value - (left + up) / 2,
                        4 => value - Paeth(left, up, upLeft),
                        _ => value
                    };
                    var b = (byte)filtered;
                    candidate[x] = b;
                    score += Math.Abs((int)(sbyte)b);
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestType = type;
                    (best, candidate) = (candidate, best);
                }
            }

            var target = y * (stride + 1);
            result[target] = (byte)bestType;
            Array.Copy(best, 0, result, target + 1, stride);
        }

        return result;
    }

    static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb &&
            pa <= pc)
        {
            return a;
        }

        if (pb <= pc)
        {
            return b;
        }

        return c;
    }

    static void WriteChunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        BigEndian(length, 0, (uint)data.Length);
        output.Write(length, 0, 4);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes, 0, 4);
        output.Write(data, 0, data.Length);

        var crc = Crc32.Update(Crc32.Update(0xFFFFFFFF, typeBytes), data) ^ 0xFFFFFFFF;
        var crcBytes = new byte[4];
        BigEndian(crcBytes, 0, crc);
        output.Write(crcBytes, 0, 4);
    }

    static void BigEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}

static class Crc32
{
    static readonly uint[] table = BuildTable();

    static uint[] BuildTable()
    {
        var result = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                {
                    c = 0xEDB88320 ^ (c >> 1);
                }
                else
                {
                    c >>= 1;
                }
            }

            result[n] = c;
        }

        return result;
    }

    public static uint Update(uint crc, byte[] data)
    {
        foreach (var b in data)
        {
            crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }
}

/// <summary>A zlib stream (RFC 1950) holding one deflate block (RFC 1951) with the fixed Huffman codes.</summary>
static class Zlib
{
    const int windowSize = 32768;
    const int minMatch = 3;
    const int maxMatch = 258;
    const int maxChain = 128;
    const int hashSize = 1 << 15;

    static readonly int[] lengthBase = [3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258];
    static readonly int[] lengthExtra = [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0];
    static readonly int[] distanceBase = [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577];
    static readonly int[] distanceExtra = [0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13];

    public static byte[] Compress(byte[] data)
    {
        var writer = new BitWriter();
        // CMF: deflate with a 32K window. FLG: no dictionary, check bits making CMF*256+FLG a multiple of 31.
        writer.WriteByte(0x78);
        writer.WriteByte(0x01);

        // BFINAL=1, BTYPE=01 (fixed Huffman codes).
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 2);

        var length = data.Length;
        var head = new int[hashSize];
        for (var i = 0; i < head.Length; i++)
        {
            head[i] = -1;
        }

        var previous = new int[windowSize];

        int Hash(int position) =>
            ((data[position] << 10) ^ (data[position + 1] << 5) ^ data[position + 2]) & (hashSize - 1);

        void Insert(int position)
        {
            if (position + minMatch > length)
            {
                return;
            }

            var hash = Hash(position);
            previous[position & (windowSize - 1)] = head[hash];
            head[hash] = position;
        }

        var index = 0;
        while (index < length)
        {
            var bestLength = 0;
            var bestDistance = 0;
            if (index + minMatch <= length)
            {
                var limit = Math.Min(maxMatch, length - index);
                var candidate = head[Hash(index)];
                var chain = maxChain;
                // Chains only ever step back, and a slot is only reused by a position a full window later,
                // so stopping at the window edge never follows a stale link.
                while (candidate >= 0 &&
                       index - candidate <= windowSize &&
                       chain-- > 0)
                {
                    var matched = 0;
                    while (matched < limit &&
                           data[candidate + matched] == data[index + matched])
                    {
                        matched++;
                    }

                    if (matched > bestLength)
                    {
                        bestLength = matched;
                        bestDistance = index - candidate;
                        if (matched == limit)
                        {
                            break;
                        }
                    }

                    candidate = previous[candidate & (windowSize - 1)];
                }
            }

            if (bestLength >= minMatch)
            {
                WriteLength(writer, bestLength);
                WriteDistance(writer, bestDistance);
                for (var i = 0; i < bestLength; i++)
                {
                    Insert(index + i);
                }

                index += bestLength;
                continue;
            }

            WriteSymbol(writer, data[index]);
            Insert(index);
            index++;
        }

        // End of block.
        WriteSymbol(writer, 256);
        writer.Flush();

        var adler = Adler32(data);
        writer.WriteByte((byte)(adler >> 24));
        writer.WriteByte((byte)(adler >> 16));
        writer.WriteByte((byte)(adler >> 8));
        writer.WriteByte((byte)adler);
        return writer.ToArray();
    }

    // The fixed literal/length code (RFC 1951 3.2.6), written most significant bit first.
    static void WriteSymbol(BitWriter writer, int symbol)
    {
        if (symbol <= 143)
        {
            writer.WriteCode(0x30 + symbol, 8);
            return;
        }

        if (symbol <= 255)
        {
            writer.WriteCode(0x190 + symbol - 144, 9);
            return;
        }

        if (symbol <= 279)
        {
            writer.WriteCode(symbol - 256, 7);
            return;
        }

        writer.WriteCode(0xC0 + symbol - 280, 8);
    }

    static void WriteLength(BitWriter writer, int length)
    {
        var code = lengthBase.Length - 1;
        while (lengthBase[code] > length)
        {
            code--;
        }

        WriteSymbol(writer, 257 + code);
        writer.WriteBits(length - lengthBase[code], lengthExtra[code]);
    }

    static void WriteDistance(BitWriter writer, int distance)
    {
        var code = distanceBase.Length - 1;
        while (distanceBase[code] > distance)
        {
            code--;
        }

        writer.WriteCode(code, 5);
        writer.WriteBits(distance - distanceBase[code], distanceExtra[code]);
    }

    static uint Adler32(byte[] data)
    {
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    sealed class BitWriter
    {
        readonly MemoryStream output = new();
        ulong buffer;
        int count;

        // Deflate packs data least significant bit first.
        public void WriteBits(int value, int bits)
        {
            buffer |= (ulong)(uint)value << count;
            count += bits;
            while (count >= 8)
            {
                output.WriteByte((byte)buffer);
                buffer >>= 8;
                count -= 8;
            }
        }

        // Huffman codes are packed most significant bit first.
        public void WriteCode(int code, int bits)
        {
            var reversed = 0;
            for (var i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | (code & 1);
                code >>= 1;
            }

            WriteBits(reversed, bits);
        }

        public void WriteByte(byte value) =>
            WriteBits(value, 8);

        public void Flush()
        {
            if (count > 0)
            {
                WriteBits(0, 8 - count);
            }
        }

        public byte[] ToArray() =>
            output.ToArray();
    }
}
