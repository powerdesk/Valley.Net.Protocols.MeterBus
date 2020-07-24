using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    public static class SecondaryAddressExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static char BcdToChar(this byte self)
        {
            char result;

            if (self < 10)
                result = (char)(self + 48);
            else
                throw new ArgumentOutOfRangeException(nameof(self));

            return result;
        }

        /// <summary>
        /// Converts a character between 0 and 9 into its BCD eqivalent or you can use the wildcard 'F' instead of digit.
        /// </summary>
        /// <param name="self">Digit to convert or wildcard 'F'</param>
        /// <returns>The BCD code or 0xF</returns>
        public static byte CharToBcd(this char self)
        {
            byte result;

            if (Char.IsDigit(self))
                result = (byte)(self - 48);
            else if (Char.ToUpper(self).Equals('F'))
                result = 0xF;
            else
                throw new ArgumentOutOfRangeException(nameof(self));

            return result;
        }

        /// <summary>
        /// Converts the argument string into its binary-coded decimal (BCD) or Wildcard representation, e.g.
        ///  "1234" -> { 0x12, 0x34 } (for Big Endian byte order)
        ///  "1234" -> { 0x34, 0x12 } (for Big Endian reversed byte order)
        ///  "1234" -> { 0x43, 0x21 } (for Little Endian byte order)
        /// </summary>
        /// <param name="self">String representation of BCD bytes.</param>
        /// <param name="reversed">True to invert the order of the elements in a sequence.</param>
        /// <param name="isLittleEndian">True if the byte order is "little end first (leftmost)".</param>
        /// <returns>Byte array representation of the string as BCD.</returns>
        /// <exception cref="ArgumentException">Thrown if the argument string isn't entirely made up of BCD pairs.</exception>
        public static byte[] ToBCDArray(this string self, bool reversed = false, bool isLittleEndian = true)
        {
            if (self == null)
                throw new ArgumentNullException();

            if (self.Length == 0)
                return new byte[0];

            if (self.Length % 2 == 1)
                self = "0" + self;

            char[] chars = self.ToCharArray();

            byte[] bytes = new byte[self.Length >> 1];

            if (isLittleEndian)
            {
                int byteIndex = bytes.Length - 1;

                for (int x = 0; x < self.Length; x += 2)
                    bytes[byteIndex--] = (byte)((chars[x + 1].CharToBcd() << 4) | (chars[x].CharToBcd()));
            }
            else
            {
                int byteIndex = 0;

                for (int x = 0; x < self.Length; x += 2)
                    bytes[byteIndex++] = (byte)((chars[x].CharToBcd() << 4) | (chars[x + 1].CharToBcd()));
            }

            if (reversed)
                return bytes.Reverse<byte>().ToArray();

            return bytes;
        }

    }

}
