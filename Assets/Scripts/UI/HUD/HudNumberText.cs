using TMPro;

namespace UI.HUD
{
    /// <summary>
    /// Writes integers into TMP labels as "48,750" without allocating a string. HUD counters roll
    /// their value every frame for a few hundred milliseconds after each change; formatting through
    /// <c>int.ToString("N0")</c> there would churn the GC on every kill.
    /// </summary>
    public static class HudNumberText
    {
        // int.MinValue is 11 digits + sign + 3 separators; 16 leaves headroom. Main-thread only.
        private static readonly char[] Buffer = new char[16];

        public static void SetGrouped(TMP_Text label, int value)
        {
            if (label == null)
                return;

            bool negative = value < 0;
            // Work in long so int.MinValue negates safely.
            long remaining = negative ? -(long)value : value;

            int index = Buffer.Length;
            int digits = 0;
            do
            {
                if (digits > 0 && digits % 3 == 0)
                    Buffer[--index] = ',';
                Buffer[--index] = (char)('0' + (int)(remaining % 10));
                remaining /= 10;
                digits++;
            } while (remaining > 0);

            if (negative)
                Buffer[--index] = '-';

            label.SetCharArray(Buffer, index, Buffer.Length - index);
        }
    }
}
