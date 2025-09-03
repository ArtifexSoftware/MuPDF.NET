/**************************************************
 Bytescout BarCode SDK
 Copyright (c) 2008 - 2015 Bytescout
 All rights reserved
 www.bytescout.com
**************************************************/

using SkiaSharp;
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Code 93 (aka USS-93) symbology rules.
    /// </summary>
    class Code93Symbology : SymbologyDrawing
    {

        /// <summary>
        /// full set of 48 characters 
        /// </summary>
        private static char[] m_alphabet = {
            '0', 
            '1', 
            '2', 
            '3', 
            '4', 
            '5', 
            '6',
            '7',
            '8',
            '9',
            'A',
            'B',
            'C',
            'D',
            'E',
            'F',
            'G',
            'H', 
            'I',
            'J', 
            'K',
            'L',
            'M',
            'N',
            'O',
            'P',
            'Q',
            'R',
            'S',
            'T',
            'U',
            'V',
            'W',
            'X',
            'Y',
            'Z',
            '-',
            '.',
            ' ',
            '$',
            '/',
            '+',             
            '%', 
            'a', //we use it instead of "($)", see getValueAsSimpleChars and ValueIsValid()
            'b', //we use it instead of "(%)", see getValueAsSimpleChars and ValueIsValid()
            'c', //we use it instead of "(/)", see getValueAsSimpleChars and ValueIsValid()
            'd', //we use it instead of "(+)", see getValueAsSimpleChars and ValueIsValid()
    };
        
        // Code 93 extended characters is similar to Code 39 extended characters table
        // DIFFERENCE: it uses up to 4 characters instead of up to 2 characters for extended symbols encoding
        // https://en.wikipedia.org/wiki/Code_93#Full_ASCII_Code_93
        private static string[] m_extendedAlphabet = 
            {
                "(%)U",  //NUL
		        "($)A",  //SOH
		        "($)B",  //STX
		        "($)C",  //ETX
		        "($)D",  //EOT
		        "($)E",  //ENQ
		        "($)F",  //ACK
		        "($)G",  //BEL
		        "($)H",  //BS
		        "($)I",  //TAB
		        "($)J",  //LF
		        "($)K",  //VT
		        "($)L",  //FF
		        "($)M",  //CR
		        "($)N",  //SO
		        "($)O",  //SI
		        "($)P",  //DLE
		        "($)Q",  //DC1
		        "($)R",  //DC2
		        "($)S",  //DC3
		        "($)T",  //DC4
		        "($)U",  //NAK
		        "($)V",  //SYN
		        "($)W",  //ETB
		        "($)X",  //CAN
		        "($)Y",  //EM
		        "($)Z",  //SUB
		        "(%)A",  //ESC
		        "(%)B",  //FS
		        "(%)C",  //GS
		        "(%)D",  //RS
		        "(%)E",  //US
		        " ",  //Space

                "(/)A",  //!
		        "(/)B",  //"
		        "(/)C",  //#

		        "$",  //$ 	    It's also valid to use "#D"but it uses an extra char
		        //"/D",  //$ 	    

		        "%",  //%		It's also valid to use "#E"but it uses an extra char
		        //"/E",  //%		

		        "(/)F",  //&
		        "(/)G",  //'
		        "(/)H",  //(
		        "(/)I",  //)
		        "(/)J",  //*
                
                "+",  //+		It's also valid to use "#K"but it uses an extra char		        
                //"/K",  //+		
		        
                "(/)L",  //,

		        "-",  //-		It's also valid to use "#M" but it uses an extra char
                //"/M",  //-		
		        
                ".",  //.		It's also valid to use "#N" but it uses an extra char
                //"/N",  //.		
		        
                "/",  // /		It's also valid to use "#O" but it uses an extra char
                //"/O",  ///		
		        
                "0",  //0		It's also valid to use "#P" but it uses an extra char
                //"/P",  //0		

		        "1",  //1		It's also valid to use "#Q" but it uses an extra char
                //"/Q",  //1		

		        "2",  //2		It's also valid to use "#R" but it uses an extra char
                //"/E",  //2	

		        "3",  //3		It's also valid to use "#S" but it uses an extra char
                //"/S",  //3	

		        "4",  //4		It's also valid to use "#T" but it uses an extra char
                //"/T",  //4

		        "5",  //5		It's also valid to use "#U" but it uses an extra char
                //"/U",  //5	

		        "6",  //6		It's also valid to use "#V" but it uses an extra char
                //"/V",  //6

		        "7",  //7		It's also valid to use "#W" but it uses an extra char
                //"/W",  //7	

		        "8",  //8		It's also valid to use "#X" but it uses an extra char
                //"/X",  //8	

		        "9",  //9		It's also valid to use "#Y" but it uses an extra char
                //"/Y",  //9		

		        "(/)Z",  //:
		        "(%)F",  //;
		        "(%)G",  //<
		        "(%)H",  //;
		        "(%)I",  //;
		        "(%)J",  //;
		        "(%)V",  //@
		        "A",  //A
		        "B",  //B
		        "C",  //C
		        "D",  //D
		        "E",  //E
		        "F",  //F
		        "G",  //G
		        "H",  //H
		        "I",  //I
		        "J",  //J
		        "K",  //K
		        "L",  //L
		        "M",  //M
		        "N",  //N
		        "O",  //O
		        "P",  //P
		        "Q",  //Q
		        "R",  //R
		        "S",  //S
		        "T",  //T
		        "U",  //U
		        "V",  //V
		        "W",  //W
		        "X",  //X
		        "Y",  //Y
		        "Z",  //Z
		        "(%)K",  //[
		        "(%)L",  //\
		        "(%)M",  //]
		        "(%)N",  //^
		        "(%)O",  //_
		        "(%)W",  //`
		        "(+)A",  //a
		        "(+)B",  //b
		        "(+)C",  //c
		        "(+)D",  //d
		        "(+)E",  //e
		        "(+)F",  //f
		        "(+)G",  //g
		        "(+)H",  //h
		        "(+)I",  //i
		        "(+)J",  //j
		        "(+)K",  //k
		        "(+)L",  //l
		        "(+)M",  //m
		        "(+)N",  //n
		        "(+)O",  //o
		        "(+)P",  //p
		        "(+)Q",  //q
		        "(+)R",  //r
		        "(+)S",  //s
		        "(+)T",  //t
		        "(+)U",  //u
		        "(+)V",  //v
		        "(+)W",  //w
		        "(+)X",  //x
		        "(+)Y",  //y
		        "(+)Z",  //z
		        "(%)P",  //{
		        "(%)Q",  // 
		        "(%)R",  //}
		        "(%)S",  //~
                "(%)T"//DEL (can also use %T, %X, %Y, %Z)
            };

        private bool m_useExtendedAlphabet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Code93Symbology"/> class.
        /// </summary>
        public Code93Symbology()
            : base(TrueSymbologyType.Code93)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Code93Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public Code93Symbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.Code93)
        {
        }

        /// <summary>
        /// Validates the value using Code 93 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            foreach (int c in value)
            {
                if (c > 127)
                    return false;
            }

            // value is valid, but do we need to use extended alphabet?
            m_useExtendedAlphabet = false;

            foreach (char c in value)
            {
                // if character not found within alphabet (and not one of our control characters "abcd"!)
                // then we need to use extended alphabet
                if (getCharPosition(c) == -1 || "abcd".IndexOf(c)>-1)
                    m_useExtendedAlphabet = true;
            }

            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public override string getValueRestrictions()
        {
            return "Code 93 symbology allows at most first 128 ASCII symbols to be encoded.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Code 93 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using Code 93 symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (Options.ShowStartStop || !forCaption)
                sb.Append("*");

            string valueAsSimpleChars = getValueAsSimpleChars();

            if (forCaption)
                sb.Append(Value);
            else
                sb.Append(valueAsSimpleChars);

            // two checksum chars always added to encoded value 
            // (and optionally to caption)

            if ((forCaption && AddChecksumToCaption) || (!forCaption))
            {
                // calculate C checksum 
                int checksumCharPosition = calculateCChecksum(valueAsSimpleChars);
                char cchecksumChar = m_alphabet[checksumCharPosition];
                sb.Append(cchecksumChar);

                // calculate K checksum (for value AND C checksum char)
                checksumCharPosition = calculateKChecksum(valueAsSimpleChars + cchecksumChar);
                char kchecksumChar = m_alphabet[checksumCharPosition];
                sb.Append(kchecksumChar);
            }

            if (Options.ShowStartStop || !forCaption)
                sb.Append("*");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given character.
        /// </returns>
        protected override string getCharPattern(char c)
        {
            switch (c)
            {
                case '0':
                    return "bsssbsbss";
                case '1':
                    return "bsbssbsss";
                case '2':
                    return "bsbsssbss";
                case '3':
                    return "bsbssssbs";
                case '4':
                    return "bssbsbsss";
                case '5':
                    return "bssbssbss";
                case '6':
                    return "bssbsssbs";
                case '7':
                    return "bsbsbssss";
                case '8':
                    return "bsssbssbs";
                case '9':
                    return "bssssbsbs";
                case 'A':
                    return "bbsbsbsss";
                case 'B':
                    return "bbsbssbss";
                case 'C':
                    return "bbsbsssbs";
                case 'D':
                    return "bbssbsbss";
                case 'E':
                    return "bbssbssbs";
                case 'F':
                    return "bbsssbsbs";
                case 'G':
                    return "bsbbsbsss";
                case 'H':
                    return "bsbbssbss";
                case 'I':
                    return "bsbbsssbs";
                case 'J':
                    return "bssbbsbss";
                case 'K':
                    return "bsssbbsbs";
                case 'L':
                    return "bsbsbbsss";
                case 'M':
                    return "bsbssbbss";
                case 'N':
                    return "bsbsssbbs";
                case 'O':
                    return "bssbsbbss";
                case 'P':
                    return "bsssbsbbs";
                case 'Q':
                    return "bbsbbsbss";
                case 'R':
                    return "bbsbbssbs";
                case 'S':
                    return "bbsbsbbss";
                case 'T':
                    return "bbsbssbbs";
                case 'U':
                    return "bbssbsbbs";
                case 'V':
                    return "bbssbbsbs";
                case 'W':
                    return "bsbbsbbss";
                case 'X':
                    return "bsbbssbbs";
                case 'Y':
                    return "bssbbsbbs";
                case 'Z':
                    return "bssbbbsbs";
                case '-':
                    return "bssbsbbbs";
                case '.':
                    return "bbbsbsbss";
                case ' ':
                    return "bbbsbssbs";
                case '$':
                    return "bbbssbsbs";
                case '/':
                    return "bsbbsbbbs";
                case '+':
                    return "bsbbbsbbs";
                case '%':
                    return "bbsbsbbbs";
                case '*':
                    return "bsbsbbbbs";
                case 'a':
                    return "bssbssbbs";
                case 'b':
                    return "bbbsbbsbs";
                case 'c':
                    return "bbbsbsbbs";
                case 'd':
                    return "bssbbssbs";
            }

            return "sssssssss";
        }

        /// <summary>
        /// Gets the value as simple chars (replaces extended chars with simple chars).
        /// </summary>
        /// <returns>The value as simple chars.</returns>
        public string getValueAsSimpleChars()
        {
            string value = Value;
            if (!m_useExtendedAlphabet)
                return value;

            // simple string is at most twice as long as extended
            // because each extended character encoded as two simple characters
            StringBuilder sb = new StringBuilder();
            
            foreach (char c in Value)
            {
                string extendedCharRepresentation = m_extendedAlphabet[(int)c];
                sb.Append(extendedCharRepresentation);
            }
            
            // now replace special shift commands like ($), (%) etc with a,b,c,d markers that we 
            // use in our main alphabet internally
            // so the bars parts will be properly assigned
                        
            //'a', //we use it instead of "($)"
            sb = sb.Replace("($)", "a");
            //'b', //we use it instead of "(%)"
            sb = sb.Replace("(%)", "b");
            //'c', //we use it instead of "(/)"
            sb = sb.Replace("(/)", "c");
            //'d', //we use it instead of "(+)"
            sb = sb.Replace("(+)", "d");


            return sb.ToString();
        }

        /// <summary>
        /// Gets the char position within the alphabet.
        /// </summary>
        /// <param name="c">The char to find.</param>
        /// <returns></returns>
        private static int getCharPosition(char c)
        {
            int position = -1;
            foreach (char s in m_alphabet)
            {
                position++;
                if (c == s)
                {
                    return position;
                }
            }
            return -1;
        }

        /// <summary>
        /// Calculates the C checksum.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The C checksum.</returns>
        private static int calculateCChecksum(string value)
        {
            int weight = 1;
            int total = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                total += getCharPosition(value[i]) * weight;
                weight++;

                if (weight > 20)
                    weight = 1;
            }

            return total % 47;
        }

        /// <summary>
        /// Calculates the K checksum.
        /// </summary>
        /// <param name="value">The value to calculate checksum for.</param>
        /// <returns>The K checksum.</returns>
        private static int calculateKChecksum(string value)
        {
            int total = 0;
            int weight = 1;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                total += getCharPosition(value[i]) * weight;
                weight++;

                if (weight > 15)
                    weight = 1;
            }

            return total % 47;
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);

            foreach (char c in value)
            {
                string pattern = getCharPattern(c);
                foreach (char patternChar in pattern)
                {
                    bool drawBar = (patternChar == 'b');

                    if (drawBar)
                        m_rects.Add(new Rectangle(x, y, NarrowBarWidth, BarHeight));

                    x += NarrowBarWidth;
                }
            }

            // draw termination bar
            m_rects.Add(new Rectangle(x, y, NarrowBarWidth, BarHeight));

            x += NarrowBarWidth;

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }
    }
}
