using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BitCompressor
{
    public class RNG
    {
        const ulong PHI64 = 11400714819323198485u;
        ulong _state = 0;

        // This pseudo random number generator is a 
        // Mixed Congruential Generator with a period of 2^64
        // https://en.wikipedia.org/wiki/Linear_congruential_generator

        public uint Next(int numberOfBits) 
        {
            Debug.Assert(numberOfBits > 0 && numberOfBits <= 32);
            _state = (_state + 1) * PHI64;
            return (uint)(_state >> (64 - numberOfBits));
        }
    }
}
