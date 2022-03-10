namespace BitCompressor
{
    static class ExtensionClass
    {
        const ulong PHI64 = 11400714819323198485u;
        
        public static ulong Hash(this ulong hash, byte b)
            => (hash + b + 1) * PHI64;
        
        public static uint FinalizeHash(this ulong hash, int hashBits)
            => (uint)(hash >> (64-hashBits));
    }
}
