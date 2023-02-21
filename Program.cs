using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace Arrow;

public static class Program
{
	struct OutChunk
	{
		public int Seed;

		public int DataLength;

		public byte[]? Data;

		public void Write(BinaryWriter writer)
		{
			writer.Write(Seed);
			writer.Write((byte)DataLength);
			if (Data != null) {
				writer.Write(Data);
			}
		}
	}

	public static void Main()
	{
		using var inStream = new FileStream("lorem_ipsum.txt", FileMode.Open, FileAccess.Read);
		using var outStream = new FileStream("compressed.bin", FileMode.Create, FileAccess.Write);

		using var binaryReader = new BinaryReader(inStream);
		using var binaryWriter = new BinaryWriter(outStream);

		byte[] dataChunk;
		var chunkNum = 1;
		do {
			Console.WriteLine($"Chunk number: {chunkNum}");

			dataChunk = binaryReader.ReadBytes(256);
			var outChunk = CompressChunkA1(dataChunk);
			outChunk.Write(binaryWriter);

			chunkNum++;
		} while (dataChunk.Length == 256);

		binaryWriter.Close();
	}

	static OutChunk CompressChunkA1(byte[] data)
	{
		const int maxSeed = int.MaxValue;
		var deltas = new Dictionary<int,int>();
		var signal = new byte[data.Length];
		
		// try to generate sequence of bytes that match original sequence 
		for (var seed = 0; seed < maxSeed; seed++) {

			// TODO: Replace with custom RNG independent from .NET
			// can also use other generators here
			var rnd = new Random(seed);
			rnd.Next();
			rnd.Next();
			
			var delta = 0;
			for (var i = 0; i < data.Length; i++) {
				signal[i] = (byte)rnd.Next(256);

				// can we encode resulting noise in the same range as in original sequence?
				var noise = (int)data[i] - (int)signal[i];
				if (noise > sbyte.MaxValue || noise < sbyte.MinValue) {
					goto endFor1;
				}

				// calc deltas[seed] as number of points in generated sequence (signal) that differ from original for current seed value
				// TODO: Detect minimal noise by value
				if (noise != 0) delta++;
	        }

			if (delta < data.Length / 2) {
				deltas[seed] = delta;
				Console.WriteLine($"Found nice seed: {seed}!");

				if (delta == 0) {
					Console.WriteLine($"Found cool seed: {seed}!!!");
 					break;
				}
			}

			endFor1: ;
		}
		
		// find minumum delta (maximize signal, minimize noise)
		var minDelta = int.MaxValue;
		var minDeltaSeed = -1;
		foreach (var seed in deltas.Keys) {
		    if (deltas[seed] < minDelta) {
				minDelta = deltas[seed];
				minDeltaSeed = seed;
			}
		}

		Console.WriteLine($"Min. number of different points: {minDelta}");

		var outChunk = new OutChunk();
		outChunk.Seed = minDeltaSeed;
		if (minDelta == 0) {
			outChunk.DataLength = 0;
			outChunk.Data = null;
		}
		else {
			var noise = new byte[data.Length];
			for (var i = 0; i < data.Length; i++) {
				// calc noise and move value to a byte range
				noise[i] = (byte)((int)data[i] - (int)signal[i] - (int)sbyte.MinValue);
			}

			outChunk.Data = CompressNoise(noise);
			outChunk.DataLength = outChunk.Data.Length;
		}

		return outChunk;
	}

	static byte[] CompressNoise(byte[] data)
	{
		using var inStream = new MemoryStream(data);
		using var outStream = new MemoryStream();
		using var gzipStream = new GZipStream(outStream, CompressionMode.Compress);
		inStream.CopyTo(gzipStream);
		gzipStream.Flush();
		return outStream.ToArray();
	}
}