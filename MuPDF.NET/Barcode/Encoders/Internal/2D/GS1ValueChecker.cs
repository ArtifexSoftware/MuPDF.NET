using System;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Checks whether a value can be encoded using GS1 compatible symbology.
    /// </summary>
    class GS1ValueChecker
    {
        private static string[] m_fixedLengthAIs =
        {
            "00", "01", "02", "03", "04", "11", "12", "13", "14", "15", "16",
            "17", "18", "19", "20", "31", "32", "33", "34", "35", "36", "41"
        };
                
        private static int[] m_fixedLengthAILengths =
        {
            20, 16, 16, 16, 18, 8, 8, 8, 8, 8, 8, 8, 8, 8, 4,
            10, 10, 10, 10, 10, 10, 16
        };

        private GS1ValueChecker()
        {
        }

        public static string[] FixedLengthAIs
        {
            get
            {
                return m_fixedLengthAIs;
            }
        }

        public static string GetStripped(string value)
        {
            StringBuilder stripped = new StringBuilder();
            int lastAI = 0;
            bool fixedLengthAI = true;
            
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != '(' && value[i] != ')')
                    stripped.Append(value[i]);

                if (value[i] == '(')
                {
                    if (!fixedLengthAI)
                        stripped.Append('(');

                    string ai = value.Substring(i + 1, 2);
                    lastAI = int.Parse(ai);
                    fixedLengthAI = false;

                    if ((lastAI >= 0) && (lastAI <= 4))
                        fixedLengthAI = true;
                    
                    if ((lastAI >= 11) && (lastAI <= 20))
                        fixedLengthAI = true;

                    if ((lastAI >= 31) && (lastAI <= 36))
                        fixedLengthAI = true;

                    if (lastAI == 41)
                        fixedLengthAI = true;
                }
            }

            return stripped.ToString();
        }

        public static bool Check(string value)
        {
            foreach (char c in value)
            {
                if ((int)c >= 128)
                    return false;

                if ((int)c < 32)
                    return false;
            }

            if (value == null || value.Length == 0)
                return false;

            // we allow values without () at the start so we will put brackets ourselves
            //if (value[0] != '(')
            //    return false;

            intList leftBracketPos = new intList();
            intList rightBracketPos = new intList();
            if (!FindBracketPositions(value, leftBracketPos, rightBracketPos))
                return false;

            // value should contain at least one AI (application identifier) and
            // each AI should be enclosed in parenthesis
            if (leftBracketPos.Count != rightBracketPos.Count || leftBracketPos.Count == 0)
                return false;

            if (!eachAIisValid(value, leftBracketPos, rightBracketPos))
                return false;

            return true;
        }

        public static bool FindBracketPositions(string value, intList leftBracketPos, intList rightBracketPos)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '(')
                {
                    if (leftBracketPos.Count != rightBracketPos.Count)
                    {
                        // should not find next '(' before ')' found
                        return false;
                    }

                    leftBracketPos.Add(i);
                }
                else if (c == ')')
                {
                    if (leftBracketPos.Count != (rightBracketPos.Count + 1))
                    {
                        // should not find next ')' before '(' found
                        return false;
                    }

                    rightBracketPos.Add(i);
                }
            }

            return true;
        }

        private static bool eachAIisValid(string value, intList leftBracketPos, intList rightBracketPos)
        {
            for (int i = 0; i < leftBracketPos.Count; i++)
            {
                int start = leftBracketPos[i];
                int stop = rightBracketPos[i];

                // AI should not be longer that 4 characters
                if ((stop - start - 1) > 4)
                    return false;

                // AI should be at least 1 character long
                if ((stop - start - 1) <= 1)
                    return false;

                for (int pos = start + 1; pos < stop; pos++)
                {
                    if (!char.IsDigit(value[pos]))
                        return false;
                }

                if ((start + 3) >= value.Length)
                    return false;

                string aiStart = value.Substring(start + 1, 2);
                for (int j = 0; j < m_fixedLengthAIs.Length; j++)
                {
                    string ai = m_fixedLengthAIs[j];
                    if (aiStart == ai)
                    {
                        // found fixed length AI
                        // let's check data field length

                        // find next AI start. for last AI use string end as next AI start
                        int nextStart = value.Length;
                        if (i != (leftBracketPos.Count - 1))
                            nextStart = leftBracketPos[i + 1];

                        int aiLength = nextStart - start - 2; // subtract 2 for ( and )
                        if (aiLength != m_fixedLengthAILengths[j])
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
