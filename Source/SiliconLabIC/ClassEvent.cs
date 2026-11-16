/*
Copyright (c) 2018 Convergence Systems Limited

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLibrary
{
    /// <summary>
    /// Provides access to the Silicon Labs microcontroller of the connected reader.
    /// </summary>
    /// <remarks>
    /// The instance is reached through <see cref="HighLevelInterface.siliconlabIC"/>.
    /// </remarks>
    public partial class SiliconLabIC
    {
        /// <summary>
        /// Groups the constant values of the Silicon Labs microcontroller interface.
        /// </summary>
        public class Constants
        {
            /// <summary>
            /// SiliconLab IC Callback Type
            /// </summary>
            public enum AccessCompletedCallbackType
            {
                /// <summary>
                /// The reported information is the serial number of the reader.
                /// </summary>
                SERIALNUMBER,
                /// <summary>
                /// The kind of the reported information is not known.
                /// </summary>
                UNKNOWN
            }
        }

        /// <summary>
        /// Groups the event argument types of the Silicon Labs microcontroller interface.
        /// </summary>
        public class Events
        {
            /// <summary>
            /// Carries the information reported by the reader when an access command completes.
            /// </summary>
            public class OnAccessCompletedEventArgs : EventArgs
            {
                /// <summary>
                /// Represents the reported information, which is the serial number of the reader.
                /// </summary>
                public readonly object info;
                /// <summary>
                /// Represents the kind of information carried by <see cref="info"/>.
                /// </summary>
                public readonly Constants.AccessCompletedCallbackType type = Constants.AccessCompletedCallbackType.UNKNOWN;

                /// <summary>
                /// Initializes a new instance of the <see cref="OnAccessCompletedEventArgs"/> class.
                /// </summary>
                /// <param name="info">The reported information, which is the serial number of the reader.</param>
                /// <param name="type">The kind of information carried by <paramref name="info"/>.</param>
                public OnAccessCompletedEventArgs(object info, Constants.AccessCompletedCallbackType type)
                {
                    this.info = info;
                    this.type = type;
                }
            }
        }
    }
}
