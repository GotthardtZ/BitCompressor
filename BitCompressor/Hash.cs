namespace BitCompressor
{
    static class ExtensionClass
    {
        public static uint Hash(this uint hash, byte b)
            => (hash + b + 1) * 271;
    }
}
