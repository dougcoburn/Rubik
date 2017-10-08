using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Coburn
{
    public class IDAStar<T>
    {
        public delegate bool PruneFunc(T state);
        public delegate void NextStatesFunc(T currentState, out T[] nextStates, out string[] edgesToNextStates);
        public delegate bool EqualFunc(T lhs, T rhs);
        public static bool Search(T startState, T goalState, int maxHops, PruneFunc Prune, NextStatesFunc NextStates, EqualFunc Equal, System.Collections.Generic.Stack<string> edgesToGoal)
        {
            if (Equal(startState, goalState)) return true;
            if (maxHops == 0) return false;
            if (Prune(startState)) return false;
            T[] nextStates;
            string[] edges;
            NextStates(startState, out nextStates, out edges);
            for (int i = 0; i < nextStates.Length; ++i)
            {
                if (Search(nextStates[i], goalState, maxHops-1, Prune, NextStates, Equal, edgesToGoal))
                {
                    edgesToGoal.Push(edges[i]);
                    return true;
                }
            }
            return false;
        }
    }
}
