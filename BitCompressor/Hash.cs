namespace BitCompressor
{
    static class ExtensionClass
    {
        public static uint Hash(this string s)
        {
            uint hash = 0;
            for (int i = 0; i < s.Length; i++)
                hash = (hash + s[i] + 1) * 271;
            return hash;
        }
    }
}
