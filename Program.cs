using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class Program
{
	public static void Main()
	{
		var data = File.ReadAllBytes("lorem_ipsum.txt");
		
		var deltas = new byte[256];
		var signal = new byte[data.Length];
		for (var seed = 0; seed < 256; seed++) {
			var rnd = new Random(seed);
			rnd.Next();
			
			for (var i = 0; i < data.Length; i++) {
				signal[i] = (byte)rnd.Next(256);
			    deltas[seed] += (byte)Math.Abs(data[i] - signal[i]);
	        }
		}
		
		var minDelta = int.MaxValue;
		var minDeltaSeed = -1;
		for (var seed = 0; seed < 256; seed++) {
		    if (deltas[seed] < minDelta) {
				minDelta = deltas[seed];
				minDeltaSeed = seed;
			}
		}
		
		Console.WriteLine($"minDelta {minDelta} on seed {minDeltaSeed}");	
	}
}