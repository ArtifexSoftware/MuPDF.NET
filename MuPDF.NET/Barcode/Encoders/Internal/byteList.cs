using System;
using System.Text;
using System.Collections;

namespace BarcodeWriter.Core.Internal
{
    class byteList
    {        
        // Use of ArrayList slows down byteList, it is critical for large number of
        // elements and intensive read/write. Slowdowns are caused by necessity of
        // boxing/unboxing of value type byte when putting into/getting from ArrayList
        private ArrayList m_list = new ArrayList();

        /// <summary>
        /// Gets the <see cref="byte"/> at the specified index.
        /// </summary>
        /// <value>The zero-based index of the element to get</value>
        public byte this[int index]
        {
            get
            {
                return (byte)m_list[index];
            }
            set
            {
                m_list[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the byteList.
        /// </summary>
        /// <value>The number of elements actually contained in the byteList.</value>
        public int Count
        {
            get { return m_list.Count; }
        }

        /// <summary>
        /// Adds an <see cref="int"/> to the end of the byteList.
        /// </summary>
        /// <param name="item">The <see cref="byte"/> to be added to the end of the byteList.</param>
        public void Add(byte item)
        {
            m_list.Add(item);
        }

        public void AddRange(byte[] c)
        {
            m_list.AddRange(c);
        }
        
        /// <summary>
        /// Removes the element at the specified index of the byteList.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            m_list.RemoveAt(index);
        }

        /// <summary>
        /// Removes all elements from the byteList.
        /// </summary>
        public void Clear()
        {
            m_list.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (object o in m_list)
            {
                sb.Append((char)(byte)o);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Copies the elements of the <see cref="byteList"/> to a new array.
        /// </summary>
        /// <returns>An array containing copies of the elements of the byteList</returns>
        public byte[] ToArray()
        {
            return (byte[])m_list.ToArray(typeof(byte));
        }
    }
}
