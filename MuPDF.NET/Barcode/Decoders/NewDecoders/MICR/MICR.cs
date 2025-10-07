using SkiaSharp;
using System;
using System.Drawing;

namespace BarcodeReader.Core.MICR
{
	internal class MICR
    {
        //if the matched char has an error <max => proceed
        protected float maxMatchCharError = 0.40F;

        BlackAndWhiteImage img;

        //Initialize borders gray level for each MICR symbol. This is used to adjust 
        //image borders before matching is done. For example, 0 has a left, right, top 
        //and bottom borders black. Then, when we call GetChar, we crop the image to fit
        //with border gray levels. This is usefull to remove noise around a symbol.
        public MICR()
        {
			if (borders == null)
            {
                borders = new Borders[chars.Length];
				lock (borders)
                for (int j = 0; j < chars.Length; j++)
                {
                    int[][] c = chars[j];
                    int l = 0, r = 0, t = 0, b = 0;
                    int cols = c[0].Length, cols1 = cols - 1;
                    int rows = c.Length, rows1 = rows - 1;
                    for (int i = 0; i < rows; i++) l += c[i][0];
                    for (int i = 0; i < rows; i++) r += c[i][cols1];
                    for (int i = 0; i < cols; i++) t += c[0][i];
                    for (int i = 0; i < cols; i++) b += c[rows1][i];
                    borders[j] = new Borders((float)l / (float)(2 * rows), (float)r / (float)(2 * rows),
                        (float)t / (float)(2 * cols), (float)b / (float)(2 * cols));
                }
            }
        }

        const float EPS = 0.2F;
        //To crop the image in the rectangle r, crop the 4 borders to fit the precalculated
        //level of gray.
        SKRect Crop(Borders b, SKRect r)
        {
            int h = (int)r.Height;
            int w2 = (int)Math.Floor(r.Width / 4f) ; if (w2 < 2) w2 = 2;
            int h2 = (int)Math.Floor(h / 4f); if (h2 < 2) h2 = 2;
            int xIn = MaxColFit((int)r.Left, (int)r.Top, (int)r.Height, 1, w2, b.left);
            int xEnd = MaxColFit((int)r.Right - 1, (int)r.Top, (int)r.Height, -1, w2, b.right);
            int w = xEnd - xIn + 1;
            int yIn = MaxRowFit(xIn, (int)r.Top, w, 1, h2, b.top);
            int yEnd = MaxRowFit(xIn, (int)r.Bottom - 1, w, -1, h2, b.bottom);
            return new SKRect(xIn, yIn, xEnd - xIn + 1, yEnd - yIn + 1);
        }

        //Moves a vertical boder until the best gray level fit.
        int MaxColFit(int x, int y, int h, int inc, int end, float b)
        {
            int min = x;
            float minD = 1.0F;
            float n = 0.0F;
            for (int i = 0; i < end; i++, x += inc)
            {
                float gray = GrayCol(x, y, h);
                float d = (float)Math.Abs(b - gray) + n;
                if ((minD - d) > EPSILON)
                {
                    minD = d; min = x;
                }
                n += 0.1F;
            }
            return min;
        }

        //Moves an horizontal boder until the best gray level fit.
        int MaxRowFit(int x, int y, int w, int inc, int end, float b)
        {
            int min = y;
            float minD = 1.0F;
            float n = 0.0F;
            for (int i = 0; i < end; i++, y += inc)
            {
                float gray = GrayRow(x, y, w);
                float d = (float)Math.Abs(b - gray) + n;
                if ((minD - d) > EPSILON)
                {
                    minD = d; min = y;
                }
                n += 0.1F;
            }
            return min;
        }

        //calculates the gray level of a given row.
        float GrayRow(int x, int y, int w)
        {
            if (y >= 0 && y < img.Height)
            {
                int c = 0;
                XBitArray row = img.GetRow(y);
                for (int i = 0; i < w; i++, x++) if (row[x]) c++;
                return (float)c / (float)w;
            }
            return 0.0F;
        }

        //calculates the gray level of a given column.
        float GrayCol(int x, int y, int h)
        {
            if (x >= 0 && x < img.Width)
            {
                int c = 0;
                XBitArray col = img.GetColumn(x);
                for (int i = 0; i < h; i++, y++) if (col[y]) c++;
                return (float)c / (float)h;
            }
            return 0.0F;
        }

        //Main method to find the matching MICR symbol. Find the matching symbol
        //for a region r of the image img.
        public string GetChar(BlackAndWhiteImage img, ref SKRect r)
        {
            this.img = img;
            float min = float.MaxValue;
            string minChar = "-";
            SKRect minRect = r;
            for (int i = 0; i < chars.Length; i++)
            {
                SKRect rect = Crop(borders[i], r);
                float e = GetError(rect, chars[i]);
                if (e < min)
                {
                    min = e;
                    minChar = Convert.ToString(Chars[i]);
                    minRect = rect;
                }
            }
            if (min < maxMatchCharError) //if the best fit is under 0.4 we have found the matching symbol.
            {
                r = minRect;
                return minChar; // +"  E:" + min;
            }
            return null;
        }

        const float EPSILON = 5e-4F;
        //Key method to calculate the difference between a region of the image and a pattern.
        //The region can be bigger or smaller than the pattern, so a few maths are involved.
        float GetError(SKRect rect, int[][] c)
        {
            int rows = c.Length;
            int cols = c[0].Length;
            float[][] count = new float[rows][];
            for (int i = 0; i < rows; i++) count[i] = new float[cols];

            float w = (float)cols / (float)rect.Width;
            float h = (float)rows / (float)rect.Height;

            float Y = 0.0F;
            for (int j = 0; j < rect.Height; j++, Y += h)
            {
                float X = 0.0F;
                XBitArray row = img.GetRow((int)(rect.Top + j));
                for (int i = 0; i < rect.Width; i++, X += w)
                {
                    bool isBlack = row[(int)rect.Left + i];
                    if (isBlack)
                    {
                        float y = Y;
                        float pendH = h, currentH = 0.0F;
                        int nY = (int)Math.Floor(y + h);
                        while (pendH > EPSILON)
                        {
                            int iY = (int)Math.Floor(y);
                            if (iY == nY) currentH = pendH;
                            else currentH = (float)Math.Floor(y + 1F) - y;

                            float x = X;
                            float pendW = w, currentW = 0.0F;
                            int nX = (int)Math.Floor(x + w);
                            while (pendW > EPSILON)
                            {
                                int iX = (int)Math.Floor(x);
                                if (iX == nX) currentW = pendW;
                                else currentW = (float)Math.Floor(x + 1F) - x;

                                add(count, iY, iX, currentH * currentW);

                                pendW -= currentW;
                                x += currentW;
                            }
                            pendH -= currentH;
                            y += currentH;
                        }
                    }
                }
            }

            float error = 0.0F;
            for (int j = 0; j < rows; j++)
                for (int i = 0; i < cols; i++)
                {
                    float e = ((float)c[j][i]) / 2F - count[j][i];
                    if (e < 0.0f) e = -e;
                    error += e * e;
                }
            return (float)Math.Sqrt(error / (float)(rows * cols));
        }

        void add(float[][] count, int row, int col, float inc)
        {
            if (row < count.Length && col < count[0].Length)
                count[row][col] += inc;
        }

        //MICR patterns: 0(white), 1(gray), 2(black)
        static readonly int[][][] chars = new int[][][] { 
            new int[][]{ //0
                new int[]{1,2,2,2,2,2,1},
                new int[]{2,1,0,0,0,1,2},
                new int[]{2,0,0,0,0,0,2},
                new int[]{2,0,0,0,0,0,2},
                new int[]{2,0,0,0,0,0,2},
                new int[]{2,0,0,0,0,0,2},
                new int[]{2,0,0,0,0,0,2},
                new int[]{2,1,0,0,0,1,2},
                new int[]{1,2,2,2,2,2,1}
            },
           new int[][]{ //1 --> 4x9
                new int[]{2,2,0,0},
                new int[]{2,2,0,0},
                new int[]{0,2,0,0},
                new int[]{0,2,0,0},
                new int[]{0,2,0,0},
                new int[]{2,2,2,2},
                new int[]{2,2,2,2},
                new int[]{2,2,2,2},
                new int[]{2,2,2,2}
            },
            new int[][]{ //2 --> 4x9
                new int[]{2,2,2,2},
                new int[]{0,0,0,2},
                new int[]{0,0,0,2},
                new int[]{0,0,0,2},
                new int[]{2,2,2,2},
                new int[]{2,0,0,0},
                new int[]{2,0,0,0},
                new int[]{2,0,0,0},
                new int[]{2,2,2,2}
            },
           new int[][]{ //3 --> 5x9
                new int[]{2,2,2,2,0},
                new int[]{0,0,0,2,0},
                new int[]{0,0,0,2,0},
                new int[]{0,0,0,2,0},
                new int[]{2,2,2,2,1},
                new int[]{0,0,0,2,2},
                new int[]{0,0,0,2,2},
                new int[]{0,0,0,2,2},
                new int[]{2,2,2,2,2}
            },        
           new int[][]{ //4 --> 6x9
                new int[]{2,2,0,0,0,0},
                new int[]{2,2,0,0,0,0},
                new int[]{2,2,0,0,0,0},
                new int[]{2,2,0,0,0,0},
                new int[]{2,2,0,0,0,0},
                new int[]{2,2,0,0,2,2},
                new int[]{2,2,2,2,2,2},
                new int[]{0,0,0,0,2,2},
                new int[]{0,0,0,0,2,2}
            },        
           new int[][]{ //5 --> 5x9
                new int[]{2,2,2,2,2},
                new int[]{2,0,0,0,0},
                new int[]{2,0,0,0,0},
                new int[]{2,0,0,0,0},
                new int[]{2,2,2,2,2},
                new int[]{0,0,0,0,2},
                new int[]{0,0,0,0,2},
                new int[]{0,0,0,0,2},
                new int[]{2,2,2,2,2}
            },        
           new int[][]{ //6 --> 6x9
                new int[]{2,2,2,2,0,0},
                new int[]{2,0,0,2,0,0},
                new int[]{2,0,0,1,0,0},
                new int[]{2,0,0,0,0,0},
                new int[]{2,0,0,0,0,0},
                new int[]{2,2,2,2,2,2},
                new int[]{2,0,0,0,0,2},
                new int[]{2,0,0,0,0,2},
                new int[]{2,2,2,2,2,2}
            }, 
           new int[][]{ //7 --> 5x9
                new int[]{2,2,2,2,2},
                new int[]{2,0,0,0,2},
                new int[]{2,0,0,0,2},
                new int[]{0,0,0,1,2},
                new int[]{0,0,2,1,0},
                new int[]{0,0,2,0,0},
                new int[]{0,0,2,0,0},
                new int[]{0,0,2,0,0},
                new int[]{0,0,2,0,0}
            },
           new int[][]{ //8 --> 7x9
                new int[]{0,2,2,2,2,2,0},
                new int[]{0,2,0,0,0,2,0},
                new int[]{0,2,0,0,0,2,0},
                new int[]{0,2,0,0,0,2,0},
                new int[]{1,2,2,2,2,2,1},
                new int[]{2,2,0,0,0,2,2},
                new int[]{2,2,0,0,0,2,2},
                new int[]{2,2,0,0,0,2,2},
                new int[]{2,2,2,2,2,2,2}
            },         
           new int[][]{ //9 --> 6x9
                new int[]{2,2,2,2,2,2},
                new int[]{2,0,0,0,0,2},
                new int[]{2,0,0,0,0,2},
                new int[]{2,0,0,0,0,2},
                new int[]{2,2,2,2,2,2},
                new int[]{0,0,0,0,2,2},
                new int[]{0,0,0,0,2,2},
                new int[]{0,0,0,0,2,2},
                new int[]{0,0,0,0,2,2}
            }, 
           new int[][]{ //100 --> 7x9
                new int[]{0,0,0,0,2,2,2},
                new int[]{0,0,0,0,2,2,2},
                new int[]{2,2,0,0,2,2,2},
                new int[]{2,2,0,0,0,0,0},
                new int[]{2,2,0,0,0,0,0},
                new int[]{2,2,0,0,0,0,0},
                new int[]{2,2,0,0,2,2,2},
                new int[]{0,0,0,0,2,2,2},
                new int[]{0,0,0,0,2,2,2}
            },   
           new int[][]{ //110 --> 7x7
                new int[]{0,0,0,0,2,2,2},
                new int[]{2,0,2,0,2,2,2},
                new int[]{2,0,2,0,2,2,2},
                new int[]{2,0,2,0,2,2,2},
                new int[]{2,0,2,0,0,0,0},
                new int[]{2,0,2,0,0,0,0},
                new int[]{2,0,2,0,0,0,0}
            },
           new int[][]{ //010 --> 7x9
                new int[]{0,0,0,0,0,2,2},
                new int[]{0,0,0,0,0,2,2},
                new int[]{0,0,0,1,0,2,2},
                new int[]{0,0,0,2,0,2,2},
                new int[]{0,0,0,2,0,0,0},
                new int[]{2,2,0,2,0,0,0},
                new int[]{2,2,0,1,0,0,0},
                new int[]{2,2,0,0,0,0,0},
                new int[]{2,2,0,0,0,0,0}
            },   
           new int[][]{ //001 --> 7x4
                new int[]{2,2,0,2,2,0,2},
                new int[]{2,2,0,2,2,0,2},
                new int[]{2,2,0,2,2,0,2},
                new int[]{2,2,0,2,2,0,2}
            }
        };
        class Borders { public float left, right, top, bottom; public Borders(float l, float r, float t, float b) { left = l; right = r; top = t; bottom = b; } }
        static readonly string Chars = "0123456789abcd";
        static Borders[] borders;
    }
}
