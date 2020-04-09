using GotaSoundIO.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GotaSequenceLib {

    /// <summary>
    /// Variable length.
    /// </summary>
    public static class VariableLength {

        /// <summary>
        /// Read variable length.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="limit">Max size the variable length can be.</param>
        /// <returns>Variable length value.</returns>
        public static uint ReadVariableLength(FileReader r, int limit = -1) {

            //Get the temporary value.
            uint temp = (uint)r.ReadByte();

            //Get value.
            uint val = (uint)temp & 0x7F;
            int bytesRead = 1;

            //Run until MSB is not set.
            while ((temp & 0x80) > 0 && (limit == -1 || bytesRead < limit)) {

                //Shift value to the left 7 bits.
                val <<= 7;

                //Get new temp value.
                temp = r.ReadByte();
                bytesRead++;

                //Add the value to the value.
                val |= temp & 0x7F;

            }

            return val;

        }

        /// <summary>
        /// Write write variable length.
        /// </summary>
        /// <param name="w">The writer.</param>
        /// <param name="val">Value to write.</param>
        public static void WriteVariableLength(FileWriter w, uint val) {

            //Write the value.
            List<byte> nums = new List<byte>();
            while (val > 0) {

                //Add number.
                nums.Insert(0, (byte)(val & 0x7F));
                val >>= 7;

            }

            //Add MSB.
            for (int i = 0; i < nums.Count - 1; i++) {

                //Set MSB.
                nums[i] |= 0x80;

            }

            //Safety.
            if (nums.Count < 1) {
                nums.Add(0);
            }

            //Write the value.
            w.Write(nums.ToArray());

        }

        /// <summary>
        /// Get the size of a variable length parameter.
        /// </summary>
        /// <param name="val">Value.</param>
        /// <returns>The size of the variable length in bytes.</returns>
        public static int CalcVariableLengthSize(uint val) {

            //Write the value.
            List<byte> nums = new List<byte>();
            while (val > 0) {

                //Add number.
                nums.Insert(0, (byte)(val & 0x7F));
                val >>= 7;

            }

            //Add MSB.
            for (int i = 0; i < nums.Count - 1; i++) {

                //Set MSB.
                nums[i] |= 0x80;

            }

            //Safety.
            if (nums.Count < 1) {
                nums.Add(0);
            }

            //Return the size.
            return nums.Count;

        }

    }

}
