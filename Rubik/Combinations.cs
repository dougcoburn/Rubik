using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Coburn
{
    public static class Combinations
    {
        public static readonly ulong[] Factorial =
        {
        //  0  1  2  3  4   5    6    7     8      9       10       11        12         13          14           15             16              17              18                 19                  20
            1, 1, 2, 6, 24, 120, 720, 5040, 40320, 362880, 3628800, 39916800, 479001600, 6227020800, 87178291200, 1307674368000, 20922789888000, 355687428096000, 6402373705728000, 121645100408832000, 2432902008176640000
        };
        public static readonly ulong[] Pow2 =
        {
        //  0, 1, 2, 3, 4,  5,  6,  7,   8,   9,   10,   11,   12
            1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096
        };
        public static readonly ulong[] Pow3 =
        {
        //  0, 1, 2, 3,  4,  5,   6,   7,    8,    9,     10,    11,     12
            1, 3, 9, 27, 81, 243, 729, 2187, 6561, 19683, 59049, 177147, 531441
        };
        public static T[] ShallowClone<T>(this T[] array)
        {
            if (array == null) return null;
            T[] retval = new T[array.Length];
            Array.Copy(array, retval, array.Length);
            return retval;
        }
        public static string stringify<T>(T[] array)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            sb.Append(array[0]);
            for (int i = 1; i < array.Length; ++i)
            {
                sb.Append(',');
                sb.Append(array[i]);
            }
            sb.Append('}');
            return sb.ToString();
        }

        /// <summary>
        /// Give me a list of unique 'card IDs' between [0, deckSize) that represents an ordered permutation
        /// and I'll return an condensed unique identifier for the permutation between
        /// [0, deckSize!/(deckSize - ordereredList.Length)! )
        /// Knowing the returned identifier, deckSize, and orderedList.Length you can reconstruct orderedList
        /// with the PermutationFromId method.
        /// </summary>
        /// <param name="orderedList">the unique list of 'card IDs from the deck [0, deckSize).  Note side effect of changing values in orderedList.  Pass a copy with ShallowClone to avoid this.</param>
        /// <param name="deckSize">the number of unique 'card IDs' between [0, deckSize) one of which each element in orderedList can represent</param>
        /// <returns>a unique value representing this permutation as a number between [0, deckSize!/(deckSize - orderedList.Length)! )</returns>
        /// <remarks>orderedList will contain useless information after this method returns!</remarks>
        public static ulong PermutationId(byte[] orderedList, byte deckSize)
        {
            ulong retval = 0;
            for (uint i = 0; i < orderedList.Length; ++i)
            {
                retval = retval * (deckSize - i);
                retval += orderedList[i];
                for (uint j = i + 1; j < orderedList.Length; ++j)
                {
                    if (orderedList[j] > orderedList[i])
                    {
                        --orderedList[j];
                    }
                }
            }
            return retval;
        }
        /// <summary>
        /// Give me an id between [0, deckSize!/(deckSize - ordereredList.Length)! ), the number of elements in a deck and in a hand and I'll fill the
        /// hand with the permutation that yields the provided Id via the PermutationId method
        /// </summary>
        /// <param name="id">an ID between [0, deckSize!/(deckSize - ordereredList.Length)! ) representing and ordered list of orderedList.Length cards</param>
        /// <param name="deckSize">The number of elements in the deck (each having a unique id between [0, deckSize)</param>
        /// <param name="orderedList">an array to be modified to indicate the ordred elements that would yield the provided id</param>
        public static void PermutationFromId(ulong id, byte deckSize, byte[] orderedList)
        {
            // Get Perm
            for (int i = orderedList.Length - 1; i >= 0; i--)
            {
                orderedList[i] = (byte)(id % (byte)(deckSize - i));
                id /= (byte)(deckSize - i);
            }
            // Expand the perumutation
            for (int i = orderedList.Length - 2; i >= 0; i--)
            {
                //PrintSubGroup(retval);
                for (int j = orderedList.Length - 1; j > i; j--)
                {
                    if (orderedList[j] >= orderedList[i]) orderedList[j]++;
                }
            }
        }
        public static ulong NumPermutations(byte handSize, byte deckSize)
        {
            return Factorial[deckSize] / Factorial[deckSize - handSize];
        }
        public static ulong CombinationId(byte[] hand, byte deckSize)
        {
            int vHandSize = hand.Length;
            if (deckSize < 2) return 0;
            if (hand.Length == 0) return 0;
            ulong retval = 0;
            while (deckSize > 1 && vHandSize > 0)
            {
                ulong skip = 0;
                for (int i = 1; i < hand[hand.Length - vHandSize] + 1; ++i)
                {
                    skip += (Factorial[deckSize - i] / (Factorial[vHandSize - 1] * Factorial[deckSize - i - (vHandSize - 1)]));
                }
                retval += skip;
                deckSize -= (byte)(hand[hand.Length - vHandSize] + 1);
                --vHandSize;
                for (int i = hand.Length - vHandSize; i < hand.Length; ++i) hand[i] -= (byte)(hand[hand.Length - vHandSize - 1] + 1);
            }
            return retval;
        }
        public static void CombinationFromId(ulong id, byte deckSize, byte[] hand)
        {
            int vdeckSize = deckSize;
            int vhandSize = hand.Length;
            for (int pos = 0; pos < hand.Length; ++pos)
            {
                ulong skip = 0;
                int i;
                for (i = 1; vdeckSize - i - (vhandSize - 1) >= 0; ++i)
                {
                    ulong oldSkip = skip;
                    skip += (Factorial[vdeckSize - i] / (Factorial[vhandSize - 1] * Factorial[vdeckSize - i - (vhandSize - 1)]));
                    if (skip > id)
                    {
                        id -= oldSkip;
                        break;
                    }
                }
                if (0 == pos) { hand[0] = (byte)(i - 1); vdeckSize -= i; --vhandSize; }
                else { hand[pos] = (byte)(hand[pos - 1] + i); vdeckSize -= i; --vhandSize; }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hands">must be double sorted, each hand must be sorted smallest to largest and the hands must be sorted 
        /// by their lowest card, smallest to largest.</param>
        /// <param name="deckSize"></param>
        /// <returns></returns>
        public static ulong CombinationSetId(byte[][] hands, byte deckSize)
        {
            for (int i = hands.Length - 1; i > 0; --i)
            {
                for (int j = 0; j < i; ++j)
                {
                    for (int k = 0; k < hands[i].Length; ++k)
                    {
                        for (int l = 0; l < hands[j].Length; ++l)
                        {
                            if (hands[j][l] < hands[i][k]) --hands[i][k];
                        }
                    }
                }
            }
            ulong retval = 0;
            for (int i = 0; i < hands.Length; ++i)
            {
                retval *= NumCombinations((byte)hands[i].Length, deckSize);
                retval += CombinationId(hands[i], deckSize);
                deckSize -= (byte)hands[i].Length;
            }
            return retval;
        }
        public static void CombinationSetFromId(ulong id, byte deckSize, byte[][] hands)
        {
            // looking for a number between [0, deckSize!/(Prod(hands[i].Length!) * hands.Length! * (deckSize - Sum(hands[i].Length))!) }
            // for example a deck of 5 cards the number of 2 unordered combinations of 2 cards is : 5!/(2!*2!*2!*1!) = 5 * 4 * 3 * 2  = 5 * 3 = 15
            //                                                                                                           2 * 2   * 2
            // a sample deal could be (0,2)(1,4)[3]  -> each hand is sorted and then the set of hands are sorted by each hand's smallest id
            // a1. add the number of states that start with an id less than the one received : 0 => 0 states : results += 0  => results:0
            // a2. toss the first element and decrement remaining elements larger than the first element : (1)(0,3)[2]
            // a3. if more than one state remains: go to 1
            // b1. results += (num possible seconds hands if 0 was next element => 3!/(2!) => 3) => results:3
            // b2. (0,2)[1] => the zero element wasn't a black ball (in [] set) so don't drop it.
            // c1. results += 0 => results:3
            // c2. (1)[0]
            // d1. results += 1!/1! => 1 => results:4
            // d2. () ==> drop black ball as it was used to skip ahead 1

            // (1,4)(2,3)[0]
            // a1. results += {5!/(2!*2!*2!) - 4!/(2!*2!*2!) => 15 - 3 => 12} => results:12
            // a2. (2)(0,1) ==> drop the black ball 0 as it was used to skip ahead.
            // b1. results += 2*(2!/2!) => 2 => results:14
            // b2. (0,1)
            // c1. results += 0 => results:14
            // c2. (0)
            // T = Sum(i=1,hands.Length, hands[i].Length)
        }
        public static ulong NumCombinations(byte handSize, byte deckSize)
        {
            return Factorial[deckSize] / (Factorial[deckSize - handSize] * Factorial[handSize]);
        }
        public static ulong TwistId(byte[] orderedList, byte mod)
        {
            ulong retval = 0;
            for (int i = 0; i < orderedList.Length; ++i)
            {
                retval *= mod;
                retval += orderedList[i];
            }
            return retval;
        }
        public static void TwistFromId(ulong id, byte mod, byte[] orderedList)
        {
            for (int i = orderedList.Length - 1; i >= 0; --i)
            {
                orderedList[i] = Convert.ToByte(id % mod);
                id /= mod;
            }
        }
        public static byte[] permutate(byte[] lhs, byte[] rhs)
        {
            if (lhs.Length != rhs.Length) throw new ArgumentException("lhs.Length doesn't match rhs.Length");
            byte[] retval = new byte[lhs.Length];
            for (int i = 0; i < lhs.Length; ++i) retval[i] = lhs[rhs[i]];
            return retval;
        }
        public static byte[] twist(byte[] lhs, byte[] rhs, byte mod)
        {
            if (lhs.Length != rhs.Length) throw new ArgumentException("lhs.Length doesn't match rhs.Length");
            if (mod > 127) throw new Exception("mod must be less than 128 to avoid overflow!");
            byte[] retval = new byte[lhs.Length];
            for (int i = 0; i < lhs.Length; ++i)
            {
                retval[i] = Convert.ToByte((lhs[i] + rhs[i]) % mod);
            }
            return retval;
        }
        public static void Main()
        {
            byte[][] combinations = new byte[][]
            {
                new byte[] {0,1,2},
                new byte[] {0,1,3},
                new byte[] {0,1,4},
                new byte[] {0,2,3},
                new byte[] {0,2,4},
                new byte[] {0,3,4},
                new byte[] {1,2,3},
                new byte[] {1,2,4},
                new byte[] {1,3,4},
                new byte[] {2,3,4}
            };

            byte[][] permutations = new byte[][]
            {
                new byte[] {0,1,2},
                new byte[] {0,2,1},
                new byte[] {1,0,2},
                new byte[] {1,2,0},
                new byte[] {2,0,1},
                new byte[] {2,1,0},
                new byte[] {0,1,3},
                new byte[] {0,3,1},
                new byte[] {1,0,3},
                new byte[] {1,3,0},
                new byte[] {3,0,1},
                new byte[] {3,1,0},
                new byte[] {0,2,3},
                new byte[] {0,3,2},
                new byte[] {2,0,3},
                new byte[] {2,3,0},
                new byte[] {3,0,2},
                new byte[] {3,2,0},
                new byte[] {1,2,3},
                new byte[] {1,3,2},
                new byte[] {2,1,3},
                new byte[] {2,3,1},
                new byte[] {3,1,2},
                new byte[] {3,2,1}
            };
            for (int i = 0; i < combinations.Length; ++i)
            {
                ulong id = CombinationId(combinations[i], (byte)5);
                byte[] hand = new byte[3];
                System.Console.WriteLine("combination id of {0} out of a deck of 5 equals  {1}", stringify(combinations[i]), id);
                CombinationFromId(id, (byte)5, hand);
                System.Console.WriteLine("combination id is {0} out of a deck of 5 from id {1}", stringify(hand), id);
            }

            for (int i = 0; i < permutations.Length; ++i)
            {
                ulong id = PermutationId(permutations[i], 4);
                byte[] hand = new byte[3];
                System.Console.WriteLine("permutation id of {0} out of a deck of 4 equals  {1}", stringify(permutations[i]), id);
                PermutationFromId(id, (byte)4, hand);
                System.Console.WriteLine("permutation id is {0} out of a deck of 4 from id {1}", stringify(hand), id);
            }

            System.Console.Write("The Powers of 2 are [1");
            for (int i = 1; i < 10; ++i)
            {
                System.Console.Write(", {0}", Pow2[i]);
            }
            System.Console.WriteLine("]");
            System.Console.Write("The Powers of 3 are [1");
            for (int i = 1; i < 10; ++i)
            {
                System.Console.Write(", {0}", Pow3[i]);
            }
            System.Console.WriteLine("]");
        }
    }
}
