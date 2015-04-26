/*!
Copyright 2014 Yaminike

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Crypto
{
    /// <summary>
    /// Initialization vector used by the Cipher class
    /// </summary>
    internal class InitializationVector
    {
        /// <summary>
        /// IV Container
        /// </summary>
        private UInt32 Value = 0;

        /// <summary>
        /// Gets the bytes of the current container
        /// </summary>
        internal byte[] Bytes
        {
            get
            {
                return BitConverter.GetBytes(Value);
            }
        }

        /// <summary>
        /// Gets the HIWORD from the current container
        /// </summary>
        internal UInt16 HIWORD
        {
            get
            {
                return unchecked((UInt16)(Value >> 16));
            }
        }

        /// <summary>
        /// Gets the LOWORD from the current container
        /// </summary>
        internal UInt16 LOWORD
        {
            get
            {
                return (UInt16)Value;
            }
        }

#if KMS || EMS
        /// <summary>
        /// IV Security check
        /// </summary>
        internal bool MustSend
        {
            get
            {
                return LOWORD % 0x1F == 0;
            }
        }
#endif

        /// <summary>
        /// Creates a IV instance using <paramref name="vector"/>
        /// </summary>
        /// <param name="vector">Initialization vector</param>
        internal InitializationVector(UInt32 vector)
        {
            Value = vector;
        }

        /// <summary>
        /// Shuffles the current IV to the next vector using the shuffle table
        /// </summary>
        internal unsafe void Shuffle()
        {
            UInt32 Key = Constants.DefaultKey;
            UInt32* pKey = &Key;
            fixed (UInt32* pIV = &Value)
            {
                fixed (byte* pShuffle = Constants.Shuffle)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        *((byte*)pKey + 0) += (byte)(*(pShuffle + *((byte*)pKey + 1)) - *((byte*)pIV + i));
                        *((byte*)pKey + 1) -= (byte)(*((byte*)pKey + 2) ^ *(pShuffle + *((byte*)pIV + i)));
                        *((byte*)pKey + 2) ^= (byte)(*((byte*)pIV + i) + *(pShuffle + *((byte*)pKey + 3)));
                        *((byte*)pKey + 3) = (byte)(*((byte*)pKey + 3) - *(byte*)pKey + *(pShuffle + *((byte*)pIV + i)));

                        *(uint*)pKey = (*(uint*)pKey << 3) | (*(uint*)pKey >> (32 - 3));
                    }
                }
            }

            Value = Key;
        }
    }
}
