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
		
		// Try to generate sequence of bytes that match original sequence.
		for (var seed = 0; seed < maxSeed; seed++) {

			if (seed % 1000_000 == 0)
				Console.WriteLine($"seed: {seed}");

			// Can replace with custom RNG independent from .NET and/or use other generators here...
			var rnd = new Random(seed);
			rnd.Next();
			rnd.Next();
			
			var delta = 0;
			for (var i = 0; i < data.Length; i++) {
				signal[i] = (byte)rnd.Next(256);

				// Cutting original sequence to signal and noise increase noise range!
				var noise = (int)data[i] - (int)signal[i];

				delta += noise >= 0 ? noise : -noise;
	        }

			// Take sequences with minimal overall (integral) noise.
			if (delta < data.Length * 58) {
				deltas[seed] = delta;
				Console.WriteLine($"Found nice seed: {seed}!");
				
				if (delta == 0) {
					Console.WriteLine($"Found cool seed: {seed}!!!");
					break;
				}

				if (deltas.Count >= 3) {
					break;
				}
			}
		}
		
		// Find minumum delta.
		var minDelta = int.MaxValue;
		var minDeltaSeed = -1;
		foreach (var seed in deltas.Keys) {
		    if (deltas[seed] < minDelta) {
				minDelta = deltas[seed];
				minDeltaSeed = seed;
			}
		}

		Console.WriteLine($"Min. delta: {minDelta}");

		var outChunk = new OutChunk();
		outChunk.Seed = minDeltaSeed;
		if (minDelta == 0) {
			outChunk.DataLength = 0;
			outChunk.Data = null;
		}
		else if (minDelta > 0) {
			// Extract and compress noise.
			var rnd = new Random(outChunk.Seed);
			rnd.Next();
			rnd.Next();

			var noise = new ushort[data.Length];
			for (var i = 0; i < data.Length; i++) {
				var signal_i = rnd.Next(256);
				noise[i] = (ushort)((int)data[i] - signal_i);
			}

			outChunk.Data = CompressData(ConvertNoiseToBytes(noise));
			outChunk.DataLength = outChunk.Data.Length;
		}
		else {
			// Compress raw data.
			outChunk.Data = CompressData(data);
			outChunk.DataLength = outChunk.Data.Length;
		}

		return outChunk;
	}

	static byte[] ConvertNoiseToBytes(ushort[] noise)
	{
		var data = new byte[noise.Length * 2];
		for (var i = 0; i < noise.Length; i++) {
			var j = i * 2;
			unchecked {
				data[j] = (byte)(noise[i] / 256);
				data[j + 1] = (byte)(noise[i] % 256); 
			}
		}
		return data;
	}

	static byte[] CompressData(byte[] data)
	{
		using var inStream = new MemoryStream(data);
		using var outStream = new MemoryStream();
		using var gzipStream = new GZipStream(outStream, CompressionMode.Compress);
		inStream.CopyTo(gzipStream);
		gzipStream.Flush();
		return outStream.ToArray();
	}
}