using System;
using System.Collections;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
    /*public class Test
    {
        public void Run()
        {
            //ECC 0
            //var img  = "011111100000110100001100001010100000001001011101111100000000000000000000000000000";
            //ECC 50
            //var img = "011100000000011100000000000110011100101101000011011000111000011011100011100000000";
            //ECC 50 AB12-X
            var img = "0111000000000111000000010101011111110101010101000000100001101101000010100011000000011101010100110101001100001001010000000";

            var bits = img.ToList().Select(c => c == '1').ToArray();
            var input = new BitArray(bits);

            var decoder = new ConvolutionDecoder(input);
            decoder.Correct();
            Console.WriteLine(decoder.CorrectionSucceeded);
        }
    }*/

    // values are in line with the ones found in the actual bitstream
    enum ErrorCorrectionLevel
    {
        ECC000 = -1, ECC050 = 0, ECC080 = 1, ECC100 = 2, ECC140 = 3
    }

    // Convolution code decoder & corrector, based on sequential decoding.
    // http://en.wikipedia.org/wiki/Sequential_decoding
	internal class ConvolutionDecoder
    {
        #region ECC prefixes
        // prefixes used to identify the correction level in place
        private static readonly BitArray ECC000Prefix =
            new BitArray(new bool[] {false, true, true, true, true, true, true});

        private static readonly BitArray NonECC000Prefix =
            new BitArray(new bool[] {false, true, true, true, false, false, false});

        private static readonly BitArray ECC050Postfix =
            new BitArray(new bool[] {false, false, false, false, false, false, true, true, true, false, false, false});

        private static readonly BitArray ECC080Postfix =
            new BitArray(new bool[] {false, false, false, true, true, true, false, false, false, true, true, true});

        private static readonly BitArray ECC100Postfix =
            new BitArray(new bool[] {false, false, false, true, true, true, true, true, true, true, true, true});

        private static readonly BitArray ECC140Postfix =
            new BitArray(new bool[] {true, true, true, false, false, false, true, true, true, true, true, true});

        private static readonly BitArray[] ProtectionModes = new BitArray[]
                                                        {ECC050Postfix, ECC080Postfix, ECC100Postfix, ECC140Postfix};

        #endregion

        #region Generator Matrices
        // generator matrices - assembled them from the DM specification
		private static readonly bool[][][] GeneratorMatrixECC050 = new bool[][][]
                                                               {
                                                                   new bool[][]
                                                                       {
                                                                           new bool[] {true, false, false}, new bool[] {false, true, false},
                                                                           new bool[] {false, false, true}, new bool[] {true, true, true}
                                                                       },
                                                                   new bool[][]
                                                                       {
                                                                           new bool[] {false, false, true}, new bool[] {false, true, false},
                                                                           new bool[] {true, true, true}, new bool[] {true, true, true}
                                                                       },
                                                                   new bool[][]
                                                                       {
                                                                           new bool[] {false, false, true}, new bool[] {true, false, false},
                                                                           new bool[] {true, false, false}, new bool[] {false, true, false}
                                                                       },
                                                                   new bool[][]
                                                                       {
                                                                           new bool[] {false, true, true}, new bool[] {true, true, false},
                                                                           new bool[] {true, false, false}, new bool[] {false, false, true}
                                                                       }
                                                               };

		private static readonly bool[][][] GeneratorMatrixECC080 = new bool[][][]
                                                                      {
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, false}, new bool[] {false, true},
                                                                                  new bool[] {true, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, false}, new bool[] {true, false},
                                                                                 new bool[]  {false, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {false, false}, new bool[] {false, false},
                                                                                 new bool[]  {false, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, true}, new bool[] {false, true},
                                                                                  new bool[] {false, false}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {false, false}, new bool[] {true, false},
                                                                                  new bool[] {false, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, false}, new bool[] {true, false},
                                                                                  new bool[] {true, false}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, false}, new bool[] {false, true},
                                                                                  new bool[] {true, false}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, true}, new bool[] {false, false},
                                                                                  new bool[] {true, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {false, true}, new bool[] {true, true},
                                                                                  new bool[] {false, false}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {false, false}, new bool[] {true, true},
                                                                                  new bool[] {false, true}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {true, false}, new bool[] {true, false},
                                                                                  new bool[] {false, false}
                                                                              },
                                                                          new bool[][]
                                                                              {
                                                                                  new bool[] {false, true}, new bool[] {false, false},
                                                                                  new bool[] {false, true}
                                                                              }
                                                                      };

    	private static readonly bool[][][] GeneratorMatrixECC100 = new bool[][][]
    	                                                           	{
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { false } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { false }, new bool[] { true } },
    	                                                           		new bool[][] { new bool[] { true }, new bool[] { true } }
    	                                                           	};

		private static readonly bool[][][] GeneratorMatrixECC140 = new bool[][][]
                                                                      {
                                                                          new bool[][] { new bool[] {true}, new bool[] {true}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {false}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {false}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {true}, new bool[] {false}, new bool[] {false}},
                                                                          new bool[][] { new bool[] {true}, new bool[] {true}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {false}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {false}, new bool[] {false}, new bool[] {false}},
                                                                          new bool[][] { new bool[] {true}, new bool[] {true}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {true}, new bool[] {false}, new bool[] {false}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {true}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {true}, new bool[] {true}, new bool[] {false}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {false}, new bool[] {true}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {true}, new bool[] {false}, new bool[] {true}, new bool[] {true}},
                                                                          new bool[][] { new bool[] {true}, new bool[] {true}, new bool[] {true}, new bool[] {true}}
                                                                      };


		private static readonly bool[][][][] GeneratorMatrices = new bool[][][][]
                                                                    {
                                                                        GeneratorMatrixECC050, GeneratorMatrixECC080,
                                                                        GeneratorMatrixECC100, GeneratorMatrixECC140
                                                                    };

        #endregion

        private readonly BitArray inputData;
        private bool correctionSuceeded;
        private BitArray correctedData;
        private readonly ErrorCorrectionLevel correctionLevel;
		private readonly bool[][][] generatorMatrix;
        private readonly int k;
        private readonly int n;

        public ConvolutionDecoder(BitArray array)
        {
            inputData = array;
            int ecc000Diff = 0;
            int nonEcc000Diff = 0;
            for (int i = 0; i < 7; ++i)
            {
                if (ECC000Prefix[i] != inputData[i])
                {
                    ecc000Diff++;
                }

                if (NonECC000Prefix[i] != inputData[i])
                {
                    nonEcc000Diff++;
                }
            }
            
            if (ecc000Diff < nonEcc000Diff)
            {
                correctionLevel = ErrorCorrectionLevel.ECC000;
                inputData = Common.Utils.BitArrayPart(inputData, 7, inputData.Length - 7);
                return;
            }

            // we skip some of the cases for multiple winners
            if (nonEcc000Diff < ecc000Diff)
            {
                int[] indices = new int[4];
                int[] errors = new int[4];
                for (int i = 0; i < 4; ++i)
                {
                    indices[i] = i;
                    errors[i] = 0;
                    for (int b = 0; b < 12; ++b)
                    {
                        if (inputData[b + 7] != ProtectionModes[i][b])
                        {
                            errors[i]++;
                        }
                    }
                }

                // we skip some of the cases for multiple winners
                Array.Sort(errors, indices);
                correctionLevel = (ErrorCorrectionLevel) indices[0];
                generatorMatrix = GeneratorMatrices[indices[0]];
                n = generatorMatrix[0].Length;
				k = generatorMatrix[0][0].Length;
				inputData = Common.Utils.BitArrayPart(inputData, 19, inputData.Length - 19);

                //Debug.WriteLine("Unrandomised Bit Stream (w/o header): " + Utils.BitArrayToString(inputData));

                return;
            }
        }

        public bool CorrectionSucceeded
        {
            get {
                return correctionSuceeded;
            }
        }

        public BitArray CorrectedData
        {
            get {
                return correctedData;
            }
        }

        public ErrorCorrectionLevel CorrectionLevel
        {
            get
            {
                return correctionLevel;
            }
        }

        // Sequential decoding algorithm. Try the most appealing input, and fall back if too much error accumulates.
        public void Correct()
        {
            if (correctionLevel == ErrorCorrectionLevel.ECC000)
            {
                correctedData = inputData;
                correctionSuceeded = true;
                return;
            }

            int blockCount = inputData.Length/n;
            int dataLength = blockCount*k;
            int bitIndex = 0;
            var encoder = new ConvolutionEncoder(generatorMatrix);
            Node currentNode = new Node(encoder, 0, null);
            while (currentNode != null && currentNode.InputCandidate.Count < dataLength)
            {
                // too many error - fall back
                if (currentNode.Distance > 5)
                {
                    currentNode = currentNode.LastNode;
                    continue;
                }

                // haven't passed this node yet, create the possible routes
                if (currentNode.NextNodes == null)
                {
                    bool[] chunk = new bool[n];
                    for (int b = 0; b < n; ++b, ++bitIndex)
                    {
                        if (bitIndex >= inputData.Length)
                        {
                            correctionSuceeded = false;
                            return;
                        }
                        chunk[b] = inputData[bitIndex];
                    }
                    currentNode.ApplyOutput(chunk);
                }

                // atthis point we _know_ that NextNodes is not null, so let's proceed with the next route
                if (currentNode.NextNodes.Count == 0)
                {
                    // no more ways, go back
                    currentNode = currentNode.LastNode;
                }
                else
                {
                    Node nextNode = (Node)currentNode.NextNodes[0];
                    currentNode.NextNodes.RemoveAt(0);
                    currentNode = nextNode;   
                }
            }

            // we got back to the start - decoding failed
            if (currentNode == null)
            {
                correctionSuceeded = false;
                return;
            }

            // yay - we got the input right
            correctedData = new BitArray(currentNode.InputCandidate.Count);
            for (int i = 0; i < currentNode.InputCandidate.Count; ++i)
            {
                correctedData[i] = (bool) currentNode.InputCandidate[i];
            }
            correctionSuceeded = true;
        }

        // represents one node in the decode chain
        private class Node
        {
            private readonly ConvolutionEncoder convolutionEncoder;

            public readonly Node LastNode;

            public ArrayList NextNodes;

            public ArrayList InputCandidate;

            public readonly int Distance;

            public Node(ConvolutionEncoder encoder, int dist, Node lastNode)
            {
                convolutionEncoder = encoder;
                Distance = dist;
                LastNode = lastNode;
                InputCandidate = new ArrayList();
            }

            // generate possibilities for proceeding, based on the given output block
            public void ApplyOutput(bool[] outputBlock)
            {
                int[] distances;
                NextNodes = new ArrayList();
                bool[][] inputCandidates = GenerateInputCandidates(outputBlock, out distances);
                for (int i = 0; i < inputCandidates.Length; ++i)
                {
                    ConvolutionEncoder newCe = (ConvolutionEncoder) convolutionEncoder.Clone();
                    newCe.EncodeInput(inputCandidates[i]);
                    Node newNode = new Node(newCe, distances[i] + Distance, this);
                    newNode.InputCandidate = new ArrayList(InputCandidate);
                    newNode.InputCandidate.AddRange(inputCandidates[i]);
                    NextNodes.Add(newNode);
                }
            }

            private bool[][] GenerateInputCandidates(bool[] outputBlock, out int[] distances)
            {
                // get all the possible variations for the given input length
                int inputCount = (1 << convolutionEncoder.K);
                int[] dists = new int[inputCount];
                bool[][] inputVariations = new bool[inputCount][];
                for (int i = 0; i < inputCount; ++i)
                {
                    inputVariations[i] = new bool[convolutionEncoder.K];
                    for (int b = 0; b < convolutionEncoder.K; ++b)
                    {
                        inputVariations[i][b] = (i & (1 << b)) != 0;
                    }

                    bool[] output = convolutionEncoder.SimulateInput(inputVariations[i]);
                    dists[i] = HammingDistance(outputBlock, output);
                }

                const int maxResultCount = 3;
                int resultCount = Math.Min(maxResultCount, inputVariations.Length);
                Array.Sort(dists, inputVariations);
                distances = new int[resultCount];
                bool[][] result = new bool[resultCount][];
                Array.Copy(inputVariations, result, resultCount);
                Array.Copy(dists, distances, resultCount);
                return result;
            }

            private static int HammingDistance(bool[] a, bool[] b)
            {
                int dist = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                    {
                        dist++;
                    }
                }

                return dist;
            }
        }
    }

    // Convolution encoder. Used to get the possible outputs for a given input.
    // http://en.wikipedia.org/wiki/Convolutional_code
	internal class ConvolutionEncoder : ICloneable
    {
		private readonly bool[][][] generatorMatrices;

        public readonly int N;

        public readonly int K;

        // in fact, the real M is m-1, but we include the inputs as T=0 storage time registers
        public readonly int M;

		private bool[][] registers;

		public ConvolutionEncoder(bool[][][] gms)
        {
            generatorMatrices = gms;
            M = generatorMatrices.Length;
            if (M == 0)
            {
                throw new Exception("Memory register width is 0 for Convolution Encoder");
            }

            N = generatorMatrices[0].Length;
			K = generatorMatrices[0][0].Length;
			
			if (K == 0)
            {
                throw new Exception("Input width is 0 for Convolution Encoder");
            }
            if (N == 0)
            {
                throw new Exception("Output width is 0 for Convolution Encoder");
            }

            registers = new bool[M][];
            for (int i = 0; i < M; ++i)
            {
				registers[i] = new bool[K];

                for (int j = 0; j < K; ++j)
                {
					registers[i][j] = false;
                }
            }
        }

        public object Clone()
        {
            ConvolutionEncoder clone = new ConvolutionEncoder(generatorMatrices);
            clone.registers = Utils.CloneJaggedArray(registers);

            return clone;
        }

        public bool[] SimulateInput(bool[] input)
        {
            ValidateInput(input);
            bool[][] localRegs = Utils.CloneJaggedArray(registers);
            ShiftRegisters(localRegs);
            ApplyInput(localRegs, input);
            return CalculateOutput(localRegs);
        }

        public void ApplyInput(bool[] input)
        {
            ValidateInput(input);
            ApplyInput(registers, input);
        }

        public bool[] EncodeInput(bool[] input)
        {
            bool[] result = SimulateInput(input);
            ShiftRegisters(registers);
            ApplyInput(registers, input);
            return result;
        }

        private void ValidateInput(bool[] input)
        {
            if (input.Length != K)
            {
                throw new Exception("Invalid input length");
            }
        }

        // shift the memory registers to accomodate the next input
		private static void ShiftRegisters(bool[][] r)
        {
            for (int i = r.Length - 1; i > 0; --i)
            {
                for (int j = 0; j < r[0].Length; ++j)
                {
					r[i][j] = r[i - 1][j];
                }
            }
        }

        // apply the input to the first register
		private static void ApplyInput(bool[][] r, bool[] input)
        {
            for (int j = 0; j < r[0].Length; ++j)
            {
				r[0][j] = input[j];
            }
        }

        // get output values based on the passed register contents
		private bool[] CalculateOutput(bool[][] localRegs)
        {
            bool[] outputs = new bool[N];
            // for each register, calculate the output contribution
            for (int r = 0; r < M; ++r)
            {
                for (int o = 0; o < N; ++o)
                {
                    for (int i = 0; i < K; ++i)
                    {
						if (generatorMatrices[r][o][i])
                        {
							outputs[o] ^= localRegs[r][i];
                        }
                    }
                }
            }

            return outputs;
        }

    }
}
