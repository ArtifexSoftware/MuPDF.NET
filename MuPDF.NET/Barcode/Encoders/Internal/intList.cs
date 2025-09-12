using System;
using System.Text;
using System.Collections;

namespace BarcodeWriter.Core.Internal
{
    class intList
    {
        private ArrayList m_list = new ArrayList();

        /// <summary>
        /// Gets the <see cref="int"/> at the specified index.
        /// </summary>
        /// <value>The zero-based index of the element to get</value>
        public int this[int index]
        {
            get 
            { 
                return (int)m_list[index]; 
            }
            set
            {
                m_list[index] = value;
            }
        }

        /// <summary>
        /// Gets the number of elements actually contained in the intList.
        /// </summary>
        /// <value>The number of elements actually contained in the intList.</value>
        public int Count
        {
            get { return m_list.Count; }
        }

        /// <summary>
        /// Adds an <see cref="int"/> to the end of the intList.
        /// </summary>
        /// <param name="item">The <see cref="int"/> to be added to the end of the intList.</param>
        public void Add(int item)
        {
            m_list.Add(item);
        }

        /// <summary>
        /// Removes the element at the specified index of the intList.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            m_list.RemoveAt(index);
        }

        /// <summary>
        /// Inserts an element into the intList at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The value to insert.</param>
        public void Insert(int index, int value)
        {
            m_list.Insert(index, value);
        }
    }
}
