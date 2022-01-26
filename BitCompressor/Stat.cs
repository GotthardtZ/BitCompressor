namespace BitCompressor
{
    struct Stat
    {
        public ulong n0;
        public ulong n1;
        public double p=>(n1 + 0.5d) / (n0 + n1 + 1.0d);
        public void Update(uint bit)
        {
            n0 += bit == 0 ? 1u : 0u;
            n1 += bit == 1 ? 1u : 0u;
        }
        public bool IsMature => n0 + n1 >= 5; // we encounteder this context at least 5 times
        public bool IsCertain => n0 + n1 >= 5 && (n0 == 0 || n1 == 0); //mature statistics with a strong trend of only zeroes or ones so far
    }
}
