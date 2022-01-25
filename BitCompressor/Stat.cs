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
    }
}
