using System;
using System.Text;
using System.Collections;

namespace BarcodeWriter.Core.Internal
{
    internal enum AIDataType
    {
        alphabetic,
        numeric,
        alphameric,
    }

    internal struct AIDataFormat
    {
        public AIDataType datatype;
        public int minLength;
        public int maxLength;

        public AIDataFormat(AIDataType datatype, int minLength, int maxLength)
        {
            this.datatype = datatype;
            this.minLength = minLength;
            this.maxLength = maxLength;
        }
    }


    /// <summary>
    /// AI identifiers used in GS1 type barcodes
    /// </summary>
    internal class AI
    {
        private string m_identifier;
        private int m_identifierLength;
        private ArrayList m_formatList = new ArrayList();

        public AI(string identifier, int idLength, AIDataType datatype, int length)
        {
            m_identifier = identifier;
            m_identifierLength = idLength;
            m_formatList.Add(new AIDataFormat(datatype, length, length));
        }

        public AI(string identifier, int idLength, AIDataType datatype, int minLength, int maxLength)
        {
            m_identifier = identifier;
            m_identifierLength = idLength;
            m_formatList.Add(new AIDataFormat(datatype, minLength, maxLength));
        }

        public AI(string identifier, int idLength, AIDataType datatype1, int length1, AIDataType datatype2, int minLength2, int maxLength2)
        {
            m_identifier = identifier;
            m_identifierLength = idLength;
            m_formatList.Add(new AIDataFormat(datatype1, length1, length1));
            m_formatList.Add(new AIDataFormat(datatype2, minLength2, maxLength2));
        }

        public string Identifier
        {
            get { return m_identifier; }
        }

        public int IdentifierLength
        {
            get { return m_identifierLength; }
        }

        public int FormatsCount
        {
            get { return m_formatList.Count; }
        }

        public AIDataFormat Format(int index)
        {
            return (AIDataFormat)m_formatList[index];
        }
    }

    internal class ApplicationIdentifiers
    {
        // on the basis of GS1 General Specifications, Version 12 - Section 3: GS1 Application Identifier Definitions
        private static AI[] m_ais =
        {
            new AI("00", 2, AIDataType.numeric, 18),
            new AI("01", 2, AIDataType.numeric, 14),
            new AI("02", 2, AIDataType.numeric, 14),
            new AI("10", 2, AIDataType.alphameric, 1, 20),
            new AI("11", 2, AIDataType.numeric, 6),
            new AI("12", 2, AIDataType.numeric, 6),
            new AI("13", 2, AIDataType.numeric, 6),
            new AI("15", 2, AIDataType.numeric, 6),
            new AI("17", 2, AIDataType.numeric, 6),
            new AI("20", 2, AIDataType.numeric, 2),
            new AI("21", 2, AIDataType.alphameric, 1, 20),
            new AI("22", 2, AIDataType.alphameric, 1, 29),
            new AI("240", 3, AIDataType.alphameric, 1, 30),
            new AI("241", 3, AIDataType.alphameric, 1, 30),
            new AI("242", 3, AIDataType.numeric, 1, 6),
            new AI("250", 3, AIDataType.alphameric, 1, 30),
            new AI("251", 3, AIDataType.alphameric, 1, 30),
            new AI("253", 3, AIDataType.numeric, 13, AIDataType.alphameric, 1, 17),
            new AI("254", 3, AIDataType.alphameric, 1, 20),
            new AI("30", 2, AIDataType.numeric, 1, 8),
            new AI("310", 4, AIDataType.numeric, 6),
            new AI("311", 4, AIDataType.numeric, 6),
            new AI("312", 4, AIDataType.numeric, 6),
            new AI("313", 4, AIDataType.numeric, 6),
            new AI("314", 4, AIDataType.numeric, 6),
            new AI("315", 4, AIDataType.numeric, 6),
            new AI("316", 4, AIDataType.numeric, 6),
            new AI("32", 4, AIDataType.numeric, 6),
            new AI("330", 4, AIDataType.numeric, 6),
            new AI("331", 4, AIDataType.numeric, 6),
            new AI("332", 4, AIDataType.numeric, 6),
            new AI("333", 4, AIDataType.numeric, 6),
            new AI("334", 4, AIDataType.numeric, 6),
            new AI("335", 4, AIDataType.numeric, 6),
            new AI("336", 4, AIDataType.numeric, 6),
            new AI("337", 4, AIDataType.numeric, 6),
            new AI("34", 4, AIDataType.numeric, 6),
            new AI("350", 4, AIDataType.numeric, 6),
            new AI("351", 4, AIDataType.numeric, 6),
            new AI("352", 4, AIDataType.numeric, 6),
            new AI("353", 4, AIDataType.numeric, 6),
            new AI("354", 4, AIDataType.numeric, 6),
            new AI("355", 4, AIDataType.numeric, 6),
            new AI("356", 4, AIDataType.numeric, 6),
            new AI("357", 4, AIDataType.numeric, 6),
            new AI("36", 4, AIDataType.numeric, 6),
            new AI("37", 2, AIDataType.numeric, 1, 8),
            new AI("390", 4, AIDataType.numeric, 1, 15),
            new AI("391", 4, AIDataType.numeric, 4, 18),
            new AI("392", 4, AIDataType.numeric, 1, 15),
            new AI("393", 4, AIDataType.numeric, 4, 18),
            new AI("400", 3, AIDataType.alphameric, 1, 30),
            new AI("401", 3, AIDataType.alphameric, 1, 30),
            new AI("402", 3, AIDataType.numeric, 17),
            new AI("403", 3, AIDataType.alphameric, 1, 30),
            new AI("410", 3, AIDataType.numeric, 13),
            new AI("411", 3, AIDataType.numeric, 13),
            new AI("412", 3, AIDataType.numeric, 13),
            new AI("413", 3, AIDataType.numeric, 13),
            new AI("414", 3, AIDataType.numeric, 13),
            new AI("415", 3, AIDataType.numeric, 13),
            new AI("420", 3, AIDataType.alphameric, 1, 20),
            new AI("421", 3, AIDataType.numeric, 4, 12),
            new AI("422", 3, AIDataType.numeric, 3),
            new AI("423", 3, AIDataType.numeric, 4, 15),
            new AI("424", 3, AIDataType.numeric, 3),
            new AI("425", 3, AIDataType.numeric, 3),
            new AI("426", 3, AIDataType.numeric, 3),
            new AI("7001", 4, AIDataType.numeric, 13),
            new AI("7002", 4, AIDataType.alphameric, 1, 30),
            new AI("7003", 4, AIDataType.numeric, 10),
            new AI("7004", 4, AIDataType.numeric, 1, 4),
            new AI("703", 4, AIDataType.numeric, 3, AIDataType.alphameric, 1, 27),
            new AI("8001", 4, AIDataType.numeric, 14),
            new AI("8002", 4, AIDataType.alphameric, 1, 20),
            new AI("8003", 4, AIDataType.numeric, 14, AIDataType.alphameric, 1, 16),
            new AI("8004", 4, AIDataType.alphameric, 1, 30),
            new AI("8005", 4, AIDataType.numeric, 6),
            new AI("8006", 4, AIDataType.numeric, 18),
            new AI("8007", 4, AIDataType.alphameric, 1, 30),
            new AI("8008", 4, AIDataType.numeric, 9, 12),
            new AI("8018", 4, AIDataType.numeric, 18),
            new AI("8020", 4, AIDataType.alphameric, 1, 25),
            new AI("8100", 4, AIDataType.numeric, 6),
            new AI("8101", 4, AIDataType.numeric, 10),
            new AI("8102", 4, AIDataType.numeric, 2),
            new AI("8110", 4, AIDataType.alphameric, 1, 70),
            new AI("8200", 4, AIDataType.alphameric, 1, 70),
            new AI("9", 2, AIDataType.alphameric, 1, 30),
        };

        /// <summary>
        /// Converts the input string like 019931265099999891ZLE000119601000930207|4201890|9261683880|8008150601081850
        /// into string like "(01)99312650999998(91)ZLE000119601000930207(420)1890(92)61683880(8008)150601081850"
        /// where values are devided by appropriate AI and force ending separator '|'
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string SelectAIs(string value)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                for (int j = 0; j < m_ais.Length; j++)
                {
                    if (m_ais[j].Identifier.Length > value.Length - i)
                        throw new BarcodeException("Incorrect length for the AI identifier");

                    if (m_ais[j].Identifier == value.Substring(i, m_ais[j].Identifier.Length))
                    {
                        sb.Append('(');
                        sb.Append(value.Substring(i, m_ais[j].IdentifierLength));
                        sb.Append(')');
                        i += m_ais[j].IdentifierLength;
                        // search if we have any separator ahead
                        int nextSeparatorPos = value.IndexOf('|', i);

                        for (int k = 0; k < m_ais[j].FormatsCount; k++)
                        {
                            if (m_ais[j].Format(k).minLength == m_ais[j].Format(k).maxLength)
                            {
                                if (m_ais[j].Format(k).minLength > value.Length - i)
                                    throw new BarcodeException("Incorrect part length for the GS1 barcode value");

                                int valLength = m_ais[j].Format(k).minLength;
                                if (nextSeparatorPos > -1 && (nextSeparatorPos - i) < valLength)
                                    valLength = nextSeparatorPos - i;

                                sb.Append(value.Substring(i, valLength));
                                i += valLength;
                            }
                            else
                            {
                                // if we have left the string greater than max length allowed for AI
                                // then we cut the string to max length only 
                                // so we will move further to another AI
                                if (value.Length - i > m_ais[j].Format(k).maxLength)
                                {
                                    int valLength = m_ais[j].Format(k).maxLength;
                                    if (nextSeparatorPos > -1 && (nextSeparatorPos - i) < valLength)
                                        valLength = nextSeparatorPos - i;

                                    sb.Append(value.Substring(i, valLength));
                                    i += valLength;
                                }
                                else 
                                {
                                    // otherwise just cut to the end 

                                    int valLength = value.Length - i;
                                    if (nextSeparatorPos > -1 && (nextSeparatorPos - i) < valLength)
                                        valLength = nextSeparatorPos - i;


                                    sb.Append(value.Substring(i, valLength));
                                    i += valLength;
                                }
                                //if (i + m_ais[j].Format(k).maxLength >= value.Length - 1)
                                //{
                                //    sb.Append(value.Substring(i));
                                //}
                            }
                        }
                        i--;
                        break;
                    }
                }
            }

            return sb.ToString();
        }
    }
}
