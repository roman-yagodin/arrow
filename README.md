# arrow

Loosless compressor/decompressor (experiment) based on "crazy idea" of splitting arbitrary sequence of bytes to procedurally generated "signal" (carrier) and "noise".
The "signal" can be represented then as a single value (pseudo-RNG seed, etc.) and "noise" compressed independently using known algorithms.

I intentionally try to keep it as simple and stupid as possible.
