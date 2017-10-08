using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace Coburn
{
    public class Profiler
    {
        static Dictionary<string, Stopwatch> stopwatches = new Dictionary<string,Stopwatch>();
        static Dictionary<string, ulong> counters = new Dictionary<string,ulong>();
        public static Stopwatch getStopWatch(string name)
        {
            if (stopwatches.ContainsKey(name)) return stopwatches[name];
            else
            {
                Stopwatch retval = new Stopwatch();
                stopwatches[name] = retval;
                return retval;
            }
        }
        public static string showTimes()
        {
            StringBuilder retval = new StringBuilder();
            foreach (string name in stopwatches.Keys)
            {
                retval.AppendFormat("spent {0} milliseconds in section {1}\r\n", stopwatches[name].ElapsedMilliseconds, name);
            }
            return retval.ToString();
        }
        public static void incrementCounter(string name)
        {
            //if (counters.ContainsKey(name)) ++counters[name];
            //else
            //{
            //    counters[name] = 1;
            //}
        }
        public static string showCounters()
        {
            StringBuilder retval = new StringBuilder();
            foreach (string name in counters.Keys)
            {
                retval.AppendFormat("executed section {1} {0} times\r\n", counters[name], name);
            }
            return retval.ToString();
        }
        public class TimeSection : IDisposable
        {
            //private string sectionName;
            //Stopwatch watch;
            public TimeSection(string name)
            {
                //sectionName = name;
                //watch = getStopWatch(name);
                //watch.Start();
            }
            public void Dispose()
            {
                //watch.Stop();
            }
        }
    }
    public class DBBuilder
    {
        public static void NextMove(byte depth, byte maxDepth, byte[] db, RubikCube cube, UInt64[] foundCount)
        {
            UInt64 id = cube.CurrentCornerCubeId;
            if (depth != maxDepth) // If We're not looking for new cubes...
            {
                if (db[id] != depth) // If these are not equal, we're backtracking, stop this path now.
                {
                    return;
                }
            }
            else // If we are looking for new cubes
            {
                // If we have already seen this configuration, stop now.
                if (id == 0 || db[id] != 0)
                {
                    return;
                }
                db[id] = depth;
                foundCount[0]++;
                if (foundCount[0] % (int)(RubikCube.MaxCornerCubeId / 100) == 0)
                {
                    Console.WriteLine("{0}% finished", foundCount[0] / (int)(RubikCube.MaxCornerCubeId / 100));
                }
                foundCount[depth]++;
            }
            if (depth < maxDepth)
            {
                for (int i = 0; i < RubikCube.Moves_FTM.Length && foundCount[0] < RubikCube.MaxCornerCubeId; i++)
                {
                    NextMove((byte)(depth + 1), maxDepth, db, cube * RubikCube.Moves_FTM[i], foundCount);
                }
            }
        }
        static int count = 0;
        public static void worker(Object o)
        {
            ThreadParam info = (ThreadParam)o;
            for (int i = info.startID; i < info.endID && info.FoundCount[0] < info.DB.Length; i++)
            {
                if (info.endID == info.DB.Length)
                {
                    float percent = (float)(i - info.startID) / (float)(info.endID - info.startID);
                    count = (int)(percent * (float)info.DB.Length);
                }
                if (info.DB[i] == info.Depth - 1)
                {
                    RubikCube lastCube = info.Factory(i);
                    for (int j = 0; j < RubikCube.Moves_FTM.Length && info.FoundCount[0] < info.DB.Length; j++)
                    {
                        RubikCube newCube = lastCube * RubikCube.Moves_FTM[j];
                        ulong id = info.Generator(newCube);
                        if (id != 0 && info.DB[id] == 0)
                        {
                            lock (info.FoundCount)
                            {
                                if (info.DB[id] == 0)
                                {
                                    info.DB[id] = info.Depth;
                                    info.FoundCount[0]++;
                                    info.FoundCount[info.Depth]++;
                                }
                            }
                        }
                    }
                }
            }
        }
        public class ThreadParam
        {
            public ThreadParam(ThreadWorkerInfo info, int index, int cThreads)
            {
                this.info = info;
                int chunkSize = info.DB.Length / cThreads;
                this.startID = index * chunkSize;
                this.endID = (index + 1) * chunkSize;
                if (index == (cThreads - 1))
                {
                    this.endID = info.DB.Length;
                }
            }
            public byte Depth { get { return info.depth; } }
            public byte[] DB { get { return info.DB; } }
            public int[] FoundCount { get { return info.foundCount; } }
            public CubeFactory Factory { get { return info.factory; } }
            public CubeIdGenerator Generator { get { return info.generator; } }
            private ThreadWorkerInfo info;
            public int startID;
            public int endID;
        }
        public class ThreadWorkerInfo
        {
            public ThreadWorkerInfo(byte depth, byte[] db, int[] foundCount,
                                    CubeFactory factory, CubeIdGenerator generator)
            {
                this.depth = depth;
                this.DB = db;
                this.foundCount = foundCount;
                this.factory = factory;
                this.generator = generator;
            }
            public byte depth;
            public byte[] DB;
            public int[] foundCount;
            public CubeFactory factory;
            public CubeIdGenerator generator;

        }
        public delegate RubikCube CubeFactory(int id);
        public delegate ulong CubeIdGenerator(RubikCube cube);
        public delegate int LowerBound(RubikCube cube);

        public static ulong GenerateCornerDBId(RubikCube cube)
        {
            return cube.CurrentCornerCubeId;
        }
        public static ulong GeneratePhase1DBId(RubikCube cube)
        {
            return Convert.ToUInt64(cube.GeneratePhase1Id());
        }
        public static ulong GenerateEdgeDBId(RubikCube cube)
        {
            return cube.CurrentEdgeCubeId / RubikCube.MaxEdgeF;
        }
        public static ulong GenerateFlipDBId(RubikCube cube)
        {
            return (((cube.CurrentCornerCubeId % RubikCube.MaxCornerF) * RubikCube.MaxEdgeF) +
                           cube.CurrentEdgeCubeId % RubikCube.MaxEdgeF);
        }
        public static ulong GenerateCornerEdgeFlipDBId(RubikCube cube)
        {
            return (((cube.CurrentCornerCubeId / (ulong)RubikCube.MaxCornerF) * RubikCube.MaxEdgeF) +
                           cube.CurrentEdgeCubeId % (ulong)RubikCube.MaxEdgeF);
        }
        public static void Main(string[] args)
        {
            ThreadWorkerInfo info;
            int cThreads = 0;
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: Rubik.exe <DBToBuild> <NumThreads>");
                return;
            }
            try
            {
                cThreads = int.Parse(args[1]);
            }
            catch (System.FormatException)
            {
                Console.WriteLine("Usage: Rubik.exe <DBToBuild> <NumThreads>");
                return;
            }
            switch (args[0])
            {
                case "Phase1DB":
                    info = new ThreadWorkerInfo(2,
                                                new byte[2217093120],
                                                new int[17],
                                                RubikCube.BuildPhase1DBCube,
                                                GeneratePhase1DBId);
                    break;
                case "EdgeDB":
                    info = new ThreadWorkerInfo(2,
                                                new byte[RubikCube.Factorial[12]],
                                                new int[17],
                                                RubikCube.BuildEdgeDBCube,
                                                GenerateEdgeDBId);
                    break;
                case "CornerDB":
                    info = new ThreadWorkerInfo(2,
                                                new byte[RubikCube.MaxCornerCubeId],
                                                new int[17],
                                                RubikCube.BuildCornerDBCube,
                                                GenerateCornerDBId);
                    break;
                case "FlipDB":
                    info = new ThreadWorkerInfo(2,
                                                new byte[RubikCube.MaxCornerF * RubikCube.MaxEdgeF],
                                                new int[17],
                                                RubikCube.BuildFlipDBCube,
                                                GenerateFlipDBId);
                    break;
                case "Corner-EdgeFlipDB":
                    info = new ThreadWorkerInfo(2,
                                                new byte[RubikCube.Factorial[8] * RubikCube.MaxEdgeF],
                                                new int[17],
                                                RubikCube.BuildCornerEdgeFlipDBCube,
                                                GenerateCornerEdgeFlipDBId);
                    break;
                default:
                    Console.WriteLine("Usage: Rubik.exe <DBToBuild> <NumThreads>");
                    return;
            }

            // Prime the pump

            info.foundCount[0] = 1; // found the solved cube
            for (int i = 0; i < RubikCube.Moves_FTM.Length; i++)
            {
                info.DB[info.generator(RubikCube.Moves_FTM[i])] = 1;
                info.foundCount[0]++;
                info.foundCount[1]++;
            }

            for (byte depth = 2; depth < info.foundCount.Length && info.foundCount[0] < info.DB.Length; depth++)
            {
                Console.WriteLine("\r\nstarting IDA* at depth {0}", depth);
                info.depth = depth;

                System.Threading.Thread[] workerThreads = new System.Threading.Thread[cThreads];
                for (int i = 0; i < cThreads; i++)
                {
                    workerThreads[i] = new System.Threading.Thread(worker);
                    workerThreads[i].Start(new ThreadParam(info, i, cThreads));
                }

                for (int i = 0; i < cThreads; i++)
                {
                    while (!workerThreads[i].Join(500))
                    {
                        int f = info.foundCount[0];
                        Console.Write("\rFound {0} of {1} ids - {2}% done with depth {3}",
                                      f, info.DB.Length,
                                      (count / (info.DB.Length / 100)),
                                      info.depth);
                    }
                }

            }
            Console.WriteLine();

            for (int i = 0; i < info.foundCount.Length; i++)
            {
                Console.WriteLine("foundCount[{0}] = {1}", i, info.foundCount[i]);
            }
            System.IO.File.WriteAllBytes(string.Format("{0}.dat", args[0]), info.DB);

            /*
            System.Collections.ArrayList al = new System.Collections.ArrayList();
            Console.WriteLine("First Method");
            for (int i = 0; i < CornerDB.Length; i++)
            {
                if (CornerDB[i] == 2)
                {
                    al.Add(i);
                    if ((new RubikCube(RubikCube.MaxEdgeCubeId, (ulong)i)).CurrentCornerCubeId != (ulong)i)
                    {
                        System.Console.WriteLine("{0} is bad ", i);
                    }
                }
            }
            al.Sort();
            foreach (int i in al)
            {
                Console.WriteLine(i);
            }
            Console.WriteLine();
            
            try
            {
                CornerDB = new byte[RubikCube.MaxCornerCubeId];
                foundCount = new ulong[11];
                RubikCube cube = new RubikCube(0, 0);
                foundCount[0] = 1; // We found the solved cube.
                byte maxDepth = 0;
                for (maxDepth = 1; maxDepth < 3; maxDepth++) // After this point, it is faster to not do recursion
                {
                    Console.WriteLine("Starting IDA* with maxDepth = {0}", maxDepth);
                    for (int i = 0; i < RubikCube.Moves_FTM.Length && foundCount[0] < RubikCube.MaxCornerCubeId; i++)
                    {
                        NextMove(1, maxDepth, CornerDB, cube * RubikCube.Moves_FTM[i], foundCount);
                    }
                }
                /*
                while (foundCount[0] < RubikCube.MaxCornerCubeId)
                {
                    Console.WriteLine("Starting IDA* with maxDepth = {0}", maxDepth);
                    for (UInt64 i = 0; i < RubikCube.MaxCornerCubeId && foundCount[0] < RubikCube.MaxCornerCubeId; i++)
                    {
                        // Look for the new cubes found during the last iteration.
                        if (CornerDB[i] == maxDepth - 1)
                        {
                            RubikCube lastCube = new RubikCube(RubikCube.MaxEdgeCubeId, i);
                            for (int j = 0; j < RubikCube.Moves_FTM.Length && foundCount[0] < RubikCube.MaxCornerCubeId; j++)
                            {
                                RubikCube newCube = lastCube * RubikCube.Moves_FTM[j];
                                UInt64 id = newCube.CurrentCornerCubeId;
                                if (CornerDB[id] == 0)
                                {
                                    CornerDB[id] = maxDepth;
                                    foundCount[0]++;
                                    if (foundCount[0] % (int)(RubikCube.MaxCornerCubeId / 100) == 0)
                                    {
                                        Console.WriteLine("{0}% finished", foundCount[0] / (int)(RubikCube.MaxCornerCubeId / 100));
                                    }
                                    foundCount[maxDepth]++;
                                }
                            }
                        }
                    }
                    maxDepth++;
                }
                
                for (int i = 0; i < foundCount.Length; i++)
                {
                    Console.WriteLine("foundCount[{0}] = {1}", i, foundCount[i]);
                }
                //System.IO.File.WriteAllBytes("cornerdb.dat", CornerDB);

                Console.WriteLine("Second Method");
                al = new System.Collections.ArrayList();
                for (int i = 0; i < CornerDB.Length; i++)
                {
                    if (CornerDB[i] == 2)
                    {
                        al.Add(i);
                        if ((new RubikCube(RubikCube.MaxEdgeCubeId, (ulong)i)).CurrentCornerCubeId != (ulong)i)
                        {
                            System.Console.WriteLine("{0} is bad ", i);
                        }
                    }
                }
                al.Sort();
                foreach (int i in al)
                {
                    Console.WriteLine(i);
                }
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            */
            /*
            int[] foundCount = new int[11];
            byte[] db = System.IO.File.ReadAllBytes("cornerdb.dat");
            for (int i = 0; i < db.Length; i++)
            {
                foundCount[db[i]]++;
            }
            for (int i = 0; i < foundCount.Length; i++)
            {
                Console.WriteLine("foundCount[{0}] = {1}", i, foundCount[i]);
            }
            */
        }
    }

    //                              CORNERS                               EDGES
    //                                              UP FACE
    //                               /\ 
    //    LEFT FACE                 /0 \                               /\        /\       BACK SIDE
    //                              \U /                              /0 \  /\  /1 \
    //                               \/                               \U / /4 \ \U /
    //                          /\  /4 \  /\                           \/  \  /  \/ 
    //                         /3 \ \D / /1 \                      /\  /\   \/   /\  /\
    //                         \U /  /\  \U /                     /7  /3 \      /2 \  5\
    //                          \/  /2 \  \/                      \   \U /      \U /   /
    //                         /7 \ \U / /5 \                      \/  \/   /\   \/  \/
    //                         \D /  \/  \D /                           /\ /6 \   /\   
    //  FRONT FACE              \/  /6 \  \/                           /8 \\  /  /9 \      RIGHT SIDE
    //                              \D /                               \D / \/   \D /
    //                               \/                                 \/        \/
    //                                                                  /\        /\
    //                                                                 /11\      /10\
    //                                            DOWN FACE            \D /      \D /
    /// <summary>
    /// The diagram above shows the location of the slot at the specified array index.  The value of P found at that index is the id of the cube currently occupying that location.
    /// The solved Rubik Cube has the cubie with a P value of 0 at array index 0, 1 at array index 1 and so forth.
    /// The cubie F value reprents is twist.  A corner cubie can have a twist value of 0, 1, or 2.  An edge cubie can have a twist value of 0 or 1.  When a corner is twisted
    /// clockwise about its outside corner it has a twist value of 1, counter-clockwise gives it a twist value of 2.  An untwisted (with twist 0) cubie has it's up or down face
    /// facing either up or down.  If the cubie does not have either the up color or down color its front or back face must be facing front or back.
    /// </summary>
    public class RubikCube : IComparable<RubikCube>
    {
        static RubikCube()
        {
            Moves_FTM = new RubikCube[(int)Move.NumMoves];
            Moves_FTM[(int)Move.U] = new RubikCube(new Cubie[12] {
                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
                                new Cubie(4,0), new Cubie(5,0), new Cubie(6,0), new Cubie(7,0), 
                                new Cubie(8,0), new Cubie(9,0), new Cubie(10,0), new Cubie(11,0)},
                            new Cubie[8] { 
                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
                                new Cubie(4,0), new Cubie(5,0), new Cubie(6,0), new Cubie(7,0)});

            Moves_FTM[(int)Move.D] = new RubikCube(new Cubie[12] {
                                new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), new Cubie(3,0), 
                                new Cubie(4,0), new Cubie(5,0), new Cubie(6,0), new Cubie(7,0), 
                                new Cubie(9,0), new Cubie(10,0), new Cubie(11,0), new Cubie(8,0)},
                            new Cubie[8] { 
                                new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), new Cubie(3,0), 
                                new Cubie(5,0), new Cubie(6,0), new Cubie(7,0), new Cubie(4,0)});

            Moves_FTM[(int)Move.F] = new RubikCube(new Cubie[12] {
                                new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), new Cubie(7,1), 
                                new Cubie(4,0), new Cubie(5,0), new Cubie(3,1), new Cubie(11,1), 
                                new Cubie(8,0), new Cubie(9,0), new Cubie(10,0), new Cubie(6,1)},
                            new Cubie[8] { 
                                new Cubie(0,0), new Cubie(1,0), new Cubie(3,1), new Cubie(7,2), 
                                new Cubie(4,0), new Cubie(5,0), new Cubie(2,2), new Cubie(6,1)});

            Moves_FTM[(int)Move.B] = new RubikCube(new Cubie[12] {
                                new Cubie(0,0), new Cubie(5,1), new Cubie(2,0), new Cubie(3,0), 
                                new Cubie(1,1), new Cubie(9,1), new Cubie(6,0), new Cubie(7,0), 
                                new Cubie(8,0), new Cubie(4,1), new Cubie(10,0), new Cubie(11,0)},
                            new Cubie[8] { 
                                new Cubie(1,1), new Cubie(5,2), new Cubie(2,0), new Cubie(3,0), 
                                new Cubie(0,2), new Cubie(4,1), new Cubie(6,0), new Cubie(7,0)});

            Moves_FTM[(int)Move.R] = new RubikCube(new Cubie[12] {
                                new Cubie(0,0), new Cubie(1,0), new Cubie(6,0), new Cubie(3,0), 
                                new Cubie(4,0), new Cubie(2,0), new Cubie(10,0), new Cubie(7,0), 
                                new Cubie(8,0), new Cubie(9,0), new Cubie(5,0), new Cubie(11,0)},
                            new Cubie[8] { 
                                new Cubie(0,0), new Cubie(2,1), new Cubie(6,2), new Cubie(3,0), 
                                new Cubie(4,0), new Cubie(1,2), new Cubie(5,1), new Cubie(7,0)});


            Moves_FTM[(int)Move.L] = new RubikCube(new Cubie[12] {
                                new Cubie(4,0), new Cubie(1,0), new Cubie(2,0), new Cubie(3,0), 
                                new Cubie(8,0), new Cubie(5,0), new Cubie(6,0), new Cubie(0,0), 
                                new Cubie(7,0), new Cubie(9,0), new Cubie(10,0), new Cubie(11,0)},
                            new Cubie[8] { 
                                new Cubie(4,2), new Cubie(1,0), new Cubie(2,0), new Cubie(0,1), 
                                new Cubie(7,1), new Cubie(5,0), new Cubie(6,0), new Cubie(3,2)});

            Moves_FTM[(int)Move.U2] = Moves_FTM[(int)Move.U] * Moves_FTM[(int)Move.U];
            Moves_FTM[(int)Move.D2] = Moves_FTM[(int)Move.D] * Moves_FTM[(int)Move.D];
            Moves_FTM[(int)Move.L2] = Moves_FTM[(int)Move.L] * Moves_FTM[(int)Move.L];
            Moves_FTM[(int)Move.R2] = Moves_FTM[(int)Move.R] * Moves_FTM[(int)Move.R];
            Moves_FTM[(int)Move.F2] = Moves_FTM[(int)Move.F] * Moves_FTM[(int)Move.F];
            Moves_FTM[(int)Move.B2] = Moves_FTM[(int)Move.B] * Moves_FTM[(int)Move.B];

            Moves_FTM[(int)Move.U3] = -Moves_FTM[(int)Move.U];
            Moves_FTM[(int)Move.D3] = -Moves_FTM[(int)Move.D];
            Moves_FTM[(int)Move.L3] = -Moves_FTM[(int)Move.L];
            Moves_FTM[(int)Move.R3] = -Moves_FTM[(int)Move.R];
            Moves_FTM[(int)Move.F3] = -Moves_FTM[(int)Move.F];
            Moves_FTM[(int)Move.B3] = -Moves_FTM[(int)Move.B];

            Symetries = new RubikCube[(int)Symetry.NumSymetries];  // 24 symetries

            Symetries[(int)Symetry.F] = new RubikCube(new Cubie[12] {
                                new Cubie(8,1), new Cubie(4,1), new Cubie(0,1), new Cubie(7,1), 
                                new Cubie(9,1), new Cubie(1,1), new Cubie(3,1), new Cubie(11,1), 
                                new Cubie(10,1), new Cubie(5,1), new Cubie(2,1), new Cubie(6,1)},
                            new Cubie[8] { 
                                new Cubie(4,1), new Cubie(0,2), new Cubie(3,1), new Cubie(7,2), 
                                new Cubie(5,2), new Cubie(1,1), new Cubie(2,2), new Cubie(6,1)});

            Symetries[(int)Symetry.R] = new RubikCube(new Cubie[12] {
                                new Cubie(7,0), new Cubie(3,1), new Cubie(6,0), new Cubie(11,1), 
                                new Cubie(0,0), new Cubie(2,0), new Cubie(10,0), new Cubie(8,0), 
                                new Cubie(4,0), new Cubie(1,1), new Cubie(5,0), new Cubie(9,1)},
                            new Cubie[8] { 
                                new Cubie(3,2), new Cubie(2,1), new Cubie(6,2), new Cubie(7,1), 
                                new Cubie(0,1), new Cubie(1,2), new Cubie(5,1), new Cubie(4,2)});

            Symetries[(int)Symetry.U] = new RubikCube(new Cubie[12] {
                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
                                new Cubie(7,1), new Cubie(4,1), new Cubie(5,1), new Cubie(6,1), 
                                new Cubie(11,0), new Cubie(8,0), new Cubie(9,0), new Cubie(10,0)},
                            new Cubie[8] { 
                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
                                new Cubie(7,0), new Cubie(4,0), new Cubie(5,0), new Cubie(6,0)});

            Mirror = new RubikCube(new Cubie[12] {
                                new Cubie(2,0), new Cubie(1,0), new Cubie(0,0), new Cubie(3,0), 
                                new Cubie(5,0), new Cubie(4,0), new Cubie(7,0), new Cubie(6,0), 
                                new Cubie(10,0), new Cubie(9,0), new Cubie(8,0), new Cubie(11,0)},
                            new Cubie[8] { 
                                new Cubie(1,0), new Cubie(0,0), new Cubie(3,0), new Cubie(2,0), 
                                new Cubie(5,0), new Cubie(4,0), new Cubie(7,0), new Cubie(6,0)});


            Symetries[(int)Symetry.U2] = Symetries[(int)Symetry.U] * Symetries[(int)Symetry.U];
            Symetries[(int)Symetry.R2] = Symetries[(int)Symetry.R] * Symetries[(int)Symetry.R];
            Symetries[(int)Symetry.F2] = Symetries[(int)Symetry.F] * Symetries[(int)Symetry.F];

            Symetries[(int)Symetry.RF] = Symetries[(int)Symetry.R] * Symetries[(int)Symetry.F];
            Symetries[(int)Symetry.FU] = Symetries[(int)Symetry.F] * Symetries[(int)Symetry.U];
            Symetries[(int)Symetry.RU] = Symetries[(int)Symetry.R] * Symetries[(int)Symetry.U];
            Symetries[(int)Symetry.UR] = Symetries[(int)Symetry.U] * Symetries[(int)Symetry.R];

            Symetries[(int)Symetry.Ui] = -Symetries[(int)Symetry.U];
            Symetries[(int)Symetry.Ri] = -Symetries[(int)Symetry.R];
            Symetries[(int)Symetry.Fi] = -Symetries[(int)Symetry.F];

            Symetries[(int)Symetry.RFi] = -Symetries[(int)Symetry.RF];
            Symetries[(int)Symetry.FUi] = -Symetries[(int)Symetry.FU];
            Symetries[(int)Symetry.RUi] = -Symetries[(int)Symetry.RU];
            Symetries[(int)Symetry.URi] = -Symetries[(int)Symetry.UR];

            Symetries[(int)Symetry.RF2] = Symetries[(int)Symetry.R] * Symetries[(int)Symetry.F2];
            Symetries[(int)Symetry.RU2] = Symetries[(int)Symetry.R] * Symetries[(int)Symetry.U2];
            Symetries[(int)Symetry.FR2] = Symetries[(int)Symetry.F] * Symetries[(int)Symetry.R2];
            Symetries[(int)Symetry.FU2] = Symetries[(int)Symetry.F] * Symetries[(int)Symetry.U2];
            Symetries[(int)Symetry.UR2] = Symetries[(int)Symetry.U] * Symetries[(int)Symetry.R2];
            Symetries[(int)Symetry.UF2] = Symetries[(int)Symetry.U] * Symetries[(int)Symetry.F2];
            SymmetryMoveConversions = new SortedDictionary<RubikCube, Move[]>();
            //                                                                   Move.U, Move.U2, Move.U3, Move.D, Move.D2, Move.D3, Move.L, Move.L2, Move.L3, Move.R, Move.R2, Move.R3, Move.F, Move.F2, Move.F3, Move.B, Move.B2, Move.B3
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.F], new Move[]  { Move.R, Move.R2, Move.R3, Move.L, Move.L2, Move.L3, Move.U, Move.U2, Move.U3, Move.D, Move.D2, Move.D3, Move.F, Move.F2, Move.F3, Move.B, Move.B2, Move.B3 });
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.U], new Move[]  { Move.U, Move.U2, Move.U3, Move.D, Move.D2, Move.D3, Move.B, Move.B2, Move.B3, Move.F, Move.F2, Move.F3, Move.L, Move.L2, Move.L3, Move.R, Move.R2, Move.R3 });
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.R], new Move[]  { Move.B, Move.B2, Move.B3, Move.F, Move.F2, Move.F3, Move.L, Move.L2, Move.L3, Move.R, Move.R2, Move.R3, Move.U, Move.U2, Move.U3, Move.D, Move.D2, Move.D3 });

            SymmetryMoveConversions.Add(Symetries[(int)Symetry.F2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F]], SymmetryMoveConversions[Symetries[(int)Symetry.F]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.Fi], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F2]], SymmetryMoveConversions[Symetries[(int)Symetry.F]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.R2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.R]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.Ri], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R2]], SymmetryMoveConversions[Symetries[(int)Symetry.R]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.U2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U]], SymmetryMoveConversions[Symetries[(int)Symetry.U]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.Ui], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U2]], SymmetryMoveConversions[Symetries[(int)Symetry.U]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.FR2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F]], SymmetryMoveConversions[Symetries[(int)Symetry.R2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.UF2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U]], SymmetryMoveConversions[Symetries[(int)Symetry.F2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.FU], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F]], SymmetryMoveConversions[Symetries[(int)Symetry.U]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.FU2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F]], SymmetryMoveConversions[Symetries[(int)Symetry.U2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.FUi], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.F]], SymmetryMoveConversions[Symetries[(int)Symetry.Ui]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RF], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.F]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RF2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.F2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RFi], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.Fi]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RU], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.U]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RU2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.U2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.RUi], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.R]], SymmetryMoveConversions[Symetries[(int)Symetry.Ui]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.UR], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U]], SymmetryMoveConversions[Symetries[(int)Symetry.R]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.UR2], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U]], SymmetryMoveConversions[Symetries[(int)Symetry.R2]]));
            SymmetryMoveConversions.Add(Symetries[(int)Symetry.URi], AddMovesTranslations(SymmetryMoveConversions[Symetries[(int)Symetry.U]], SymmetryMoveConversions[Symetries[(int)Symetry.Ri]]));

        }

        #region Useful Cube constants
        public static readonly int[] Factorial = { 1, 1, 2, 6, 24, 120, 720, 5040, 40320, 362880, 3628800, 39916800, 479001600 };
        public const int MaxEdgeF = 2048;
        public const int MaxCornerF = 2187;
        public const UInt64 MaxEdgeCubeId = 980995276800;
        public const UInt64 MaxCornerCubeId = 88179840;
        public static readonly SortedDictionary<RubikCube, Move[]> SymmetryMoveConversions;
        #endregion

        #region Private Types
        public enum Move
        {
            U, U2, U3, D, D2, D3, L, L2, L3, R, R2, R3, F, F2, F3, B, B2, B3, NumMoves
        }
        public enum Symetry
        {
            F, R, U, RF, FU, RU, UR,
            Fi, Ri, Ui, RFi, FUi, RUi, URi,
            F2, R2, U2, RF2, RU2, FR2, FU2, UR2, UF2,
            NumSymetries
        }
        public struct Cubie
        {
            public Cubie(int P, int F)
            {
                this.P = P;
                this.F = F;
            }
            public int P;
            public int F;
        }
        #endregion

        #region ID to-from Cube utilities
        public static uint GenerateG1Id(RubikCube cube)
        {
            ulong edgeCubeId = cube.CurrentEdgeCubeId;
            ulong edgeP = edgeCubeId / MaxEdgeF;
            ulong edgeF = edgeCubeId % MaxEdgeF;
            ulong cornerId = Convert.ToUInt64(cube.Corners[0].P * cube.Corners[0].F);
            ulong retval = cornerId * (12 * 11 * 10 * 8) + (edgeP / Convert.ToUInt64(Factorial[9])) * 8 + (edgeF / 512ul);
            return Convert.ToUInt32(retval);
        }
        public static RubikCube BuildPhase1DBCube(int id)
        {
            int edgeTwist = id % 2048; id /= 2048;
            int cornerTwist = id % 2187; id /= 2187;
            int sliceCombination = id;
            int[] combination = new int[4];
            GenerateCombinationFromIdInternal(sliceCombination, 12, ref combination);
            int[] permutation = new int[12];
            for (int i = 0; i < permutation.Length; ++i) permutation[i] = 0;
            int next = 4;
            for (int i = 0; i < combination.Length; ++i) permutation[combination[i]] = next++;
            next = 0;
            for (int i = 0; i < permutation.Length; ++i) { if (permutation[i] == 0) permutation[i] = next++; if (next == 4) next = 8; }
            ulong edgeId = (ulong)GeneratePermutationIdInternal(permutation, 12);
            edgeId *= 2048;
            edgeId += (ulong)edgeTwist;
            return new RubikCube(edgeId, (ulong)cornerTwist);
        }
        public static RubikCube BuildCornerDBCube(int id)
        {
            return new RubikCube(RubikCube.MaxEdgeCubeId, (ulong)id);
        }
        public static RubikCube BuildEdgeDBCube(int id)
        {
            return new RubikCube((ulong)id * (ulong)RubikCube.MaxEdgeF, RubikCube.MaxCornerCubeId);
        }
        public static RubikCube BuildFlipDBCube(int id)
        {
            return new RubikCube((ulong)id % (ulong)RubikCube.MaxEdgeF, (ulong)id / (ulong)RubikCube.MaxEdgeF);
        }
        public static RubikCube BuildCornerEdgeFlipDBCube(int id)
        {
            ulong cornerP = (ulong)id / (ulong)MaxEdgeF;
            ulong edgeF = (ulong)id % (ulong)MaxEdgeF;
            Cubie[] Corners = CubieArrayFromId(cornerP * MaxCornerF, 3);
            Cubie[] Edges = CubieArrayFromId(edgeF, 2);
            return new RubikCube(Edges, Corners);
        }
        private static int GeneratePermutationIdInternal(int[] list, int deckSize)
        {
            int[] tmp = (int[])list.Clone();
            int retval = 0;


            for (int i = 0; i < tmp.Length; ++i)
            {
                retval += (tmp[i] * Factorial[deckSize - 1 - i]);
                for (int j = i + 1; j < tmp.Length; ++j)
                {
                    if (tmp[j] > tmp[i])
                    {
                        --tmp[j];
                    }
                }
            }
            return retval / Factorial[deckSize - list.Length];
        }
        /// <summary>
        /// Looks at Cubie.P.  Each of the cubies should have a unique value for P between 0 and cubies.Length-1.  
        /// </summary>
        /// <param name="cubies"></param>
        /// <param name="lookFor"></param>
        /// <param name="maxId"></param>
        /// <returns></returns>
        public static int GeneratePermutationId(int[] permutation, int[] lookFor, out int maxId)
        {
            maxId = Factorial[permutation.Length] / Factorial[permutation.Length - lookFor.Length];
            int solvedId = GeneratePermutationIdInternal(lookFor, permutation.Length);
            int[] actuals = new int[lookFor.Length];
            for (int i = 0; i < lookFor.Length; ++i)
            {
                actuals[i] = permutation[lookFor[i]];
            }
            int actualId = GeneratePermutationIdInternal(actuals, permutation.Length);
            return ((actualId + (maxId - solvedId)) % maxId);
        }
        public static Cubie[] ExpandPermutationId(int id, SortedSet<int> lookFor, int permutationGroupSize)
        {
            return new Cubie[12];
        }
        private static int[] GetSubCombinationSubProblem(int[] permutation, int[] combination, out int skip)
        {
            skip = 0;
            for (int i = 0; i < combination[0]; ++i)
            {
                skip += (Factorial[permutation.Length - i] / (Factorial[combination.Length - i] * Factorial[permutation.Length - combination.Length]));
            }
            int[] retval = new ArraySegment<int>(combination, 1, combination.Length - 1).Array;
            for (int i = 0; i < retval.Length; ++i) retval[i] -= combination[0];
            return retval;
        }
        /// <summary>
        /// Pass me a sorted hand and I'll tell you a unique identifier for that hand between 0 and total unique hands -1
        /// </summary>
        /// <param name="hand">a sorted array of unique ids between 0 and decksize-1</param>
        /// <param name="deckSize">The total number of possible values each element in hand could take</param>
        /// <returns>an ID for that hand</returns>
        public static int GenerateCombinationIdInternal(int[] hand, int deckSize)
        {
            if (deckSize < 2) return 0;
            if (hand.Length == 0) return 0;
            int skip = 0;
            for (int i = 1; i < hand[0]+1; ++i)
            {
                skip += (Factorial[deckSize - i] / (Factorial[hand.Length - 1] * Factorial[deckSize - i - (hand.Length - 1)]));
            }

            int[] subHand = new int[hand.Length - 1];
            Array.Copy(hand, 1, subHand, 0, hand.Length - 1);
            for (int i = 0; i < subHand.Length; ++i) subHand[i] -= (hand[0] + 1);
            return skip + GenerateCombinationIdInternal(subHand, deckSize - hand[0] - 1);
        }
        public static void GenerateCombinationFromIdInternal(int id, int deckSize, ref int[] hand)
        {
            int vdeckSize = deckSize;
            int vhandSize = hand.Length;
            for (int pos = 0; pos < hand.Length; ++pos)
            {
                int skip = 0;
                int i;
                for (i = 1; vdeckSize - i - (vhandSize - 1) >= 0; ++i)
                {
                    int oldSkip = skip;
                    skip += (Factorial[vdeckSize - i] / (Factorial[vhandSize - 1] * Factorial[vdeckSize - i - (vhandSize - 1)]));
                    if (skip > id)
                    {
                        id -= oldSkip;
                        break;
                    }
                }
                if (0 == pos) { hand[0] = i - 1; vdeckSize -= i; --vhandSize; }
                else { hand[pos] = hand[pos - 1] + i; vdeckSize -= i; --vhandSize; }
            }
        }
        public static int GenerateCombinationId(int[] permutation, int[] lookFor, out int maxId)
        {
            maxId = Factorial[permutation.Length] / (Factorial[lookFor.Length] * Factorial[permutation.Length - lookFor.Length]);
            Array.Sort<int>(lookFor);
            int[] actuals = new int[lookFor.Length];
            for (int i = 0; i < lookFor.Length; ++i)
            {
                actuals[i] = permutation[lookFor[i]];
            }
            Array.Sort<int>(actuals);

            int solvedId = GenerateCombinationIdInternal(lookFor, permutation.Length);
            int actualId = GenerateCombinationIdInternal(actuals, permutation.Length);
            return ((actualId + (maxId - solvedId)) % maxId);
        }
        private static int RebaseTo10(int[] digits, int mod)
        {
            int retval = 0;
            for (int i = digits.Length - 1; i >= 0; --i)
            {
                retval *= mod;
                retval += digits[i];
            }
            return retval;
        }
        public int GeneratePhase1Id()
        {
            int[] slice = new int[4];
            for (int i = 0; i < Edges.Length; ++i)
            {
                if (Edges[i].P > 3 && Edges[i].P < 8) slice[Edges[i].P - 4] = i;
            }
            Array.Sort<int>(slice);
            int cornerTwist = Convert.ToInt32(CurrentCornerCubeId % 2187);
            int edgeTwist = Convert.ToInt32(CurrentEdgeCubeId % 2048);
            int sliceCombination = GenerateCombinationIdInternal(slice, 12);
            return sliceCombination * (2187 * 2048) + cornerTwist * 2048 + edgeTwist;
        }
        public static Cubie[] ExpandCombinationId(int id, SortedSet<int> lookFor, int combinationGroupSize)
        {
            return new Cubie[12];
        }
        #endregion

        #region Constructors
        private RubikCube(Cubie[] Edges, Cubie[] Corners)
        {
            this.Edges = Edges;
            this.Corners = Corners;
            this.movesSoFar = new List<Move>();
        }
        public RubikCube(UInt64 EdgeCubeId, UInt64 CornerCubeId)
        {
            if (EdgeCubeId > MaxEdgeCubeId)
            {
                throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", MaxEdgeCubeId), "EdgeCubeId");
            }
            if (CornerCubeId > MaxCornerCubeId)
            {
                throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", MaxCornerCubeId), "CornerCubeId");
            }

            if (CornerCubeId < MaxCornerCubeId)
            {
                Corners = CubieArrayFromId(CornerCubeId, 3);
            }
            if (EdgeCubeId < MaxEdgeCubeId)
            {
                Edges = CubieArrayFromId(EdgeCubeId, 2);
            }

            if (Edges != null && Corners != null && !IsPermutationPossible())
            {
                throw new System.ArgumentException("The corner and edge cubies combine into an insolvable cube");
            }
            movesSoFar = new List<Move>();
        }
        public RubikCube(string ColorChart)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Private helper functions
        private static Cubie[] CubieArrayFromId(UInt64 id, int fMod)
        {
            int P = 0;
            int F = 0;
            UInt64 MaxF = 0;
            int FSum = 0;
            Cubie[] retval = null;
            if (fMod == 3)
            {
                retval = new Cubie[8];
                MaxF = (UInt64)MaxCornerF;
            }
            else if (fMod == 2)
            {
                retval = new Cubie[12];
                MaxF = (UInt64)MaxEdgeF;
            }
            else
            {
                throw new ArgumentException("Value must be 2 or 3", "fMod");
            }
            //Console.WriteLine();
            //Console.WriteLine("CubieArrayFromId");

            P = (int)(id / MaxF);
            F = (int)(id % MaxF);

            //Console.WriteLine("P = {0}", P);
            //Console.WriteLine("F = {0}", F);

            // Get Flip
            for (int i = retval.Length - 1; i > 0; i--)
            {
                retval[i].F = (int)(F % fMod);
                FSum += retval[i].F;
                F /= fMod;
            }
            retval[0].F = (fMod - (FSum % fMod)) % fMod;

            // Get Perm
            for (int i = retval.Length - 1; i >= 0; i--)
            {
                //PrintSubGroup(retval);
                if (i == (retval.Length - 1))
                {
                    retval[i].P = 0;
                }
                else
                {
                    retval[i].P = (int)(P % (retval.Length - i));
                    P /= (retval.Length - i);
                }
            }
            // Expand the perumutation
            for (int i = retval.Length - 2; i >= 0; i--)
            {
                //PrintSubGroup(retval);
                for (int j = retval.Length - 1; j > i; j--)
                {
                    if (retval[j].P >= retval[i].P) retval[j].P++;
                }
            }
            return retval;
        }
        static Move[] AddMovesTranslations(Move[] lhs, Move[] rhs)
        {
            if (lhs.Length != rhs.Length && lhs.Length != (int)Move.NumMoves) throw new Exception("AddMoveTranslations called with incorrect length move arrays");
            Move[] retval = new Move[(int)Move.NumMoves];
            for (Move move = (Move)0; move < Move.NumMoves; ++move)
            {
                retval[(int)move] = rhs[(int)lhs[(int)move]];
            }
            return retval;
        }
        private static bool CompareP(Cubie[] lhs, Cubie[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i].P != rhs[i].P)
                {
                    return false;
                }
            }
            return true;
        }
        private static bool CompareF(Cubie[] lhs, Cubie[] rhs)
        {
            if (lhs.Length != rhs.Length)
            {
                return false;
            }
            for (int i = 0; i < lhs.Length; i++)
            {
                if (lhs[i].F != rhs[i].F)
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsPermutationPossible()
        {
            if (Edges == null || Corners == null)
            {
                return true;
            }
            int sum = 0;
            bool[] Visited = null;
            Cubie[] subgroup = null;
            for (int k = 0; k < 2; k++)
            {
                if (k == 0) subgroup = Edges; else subgroup = Corners;
                Visited = new bool[subgroup.Length];

                for (int i = 0; i < subgroup.Length; i++)
                {
                    if (!Visited[i])
                    {
                        int count = 0;
                        int j = i;
                        while (subgroup[j].P != i)
                        {
                            count++;
                            Visited[j] = true;
                            j = subgroup[j].P;
                        }
                        Visited[j] = true;
                        sum += count;
                    }
                }
            }
            return (sum % 2 == 0);
        }
        private static void PrintSubGroup(Cubie[] subgroup)
        {
            Console.Write("i\t");
            for (int i = 0; i < subgroup.Length; i++)
            {
                Console.Write("{0}\t", i);
            }
            Console.Write("\r\nP\t");
            for (int i = 0; i < subgroup.Length; i++)
            {
                Console.Write("{0}\t", subgroup[i].P);
            }
            Console.Write("\r\nF\t");
            for (int i = 0; i < subgroup.Length; i++)
            {
                Console.Write("{0}\t", subgroup[i].F);
            }
            Console.WriteLine();
        }
        private UInt64 CubieArrayId(Cubie[] subgroup, int fMod)
        {
            if (subgroup == null)
            {
                throw new ArgumentException("The parameter can not be null");
            }
            //Console.WriteLine("Getting ID of array");
            UInt64 id = 0;
            Cubie[] temp = (Cubie[])subgroup.Clone();
            for (int i = 0; i < temp.Length; i++)
            {
                //                PrintSubGroup(temp);
                //                Console.WriteLine("id so far = {0}", id);
                id += (UInt64)(temp[i].P * Factorial[temp.Length - 1 - i]);
                for (int j = i + 1; j < temp.Length; j++)
                {
                    if (temp[j].P > temp[i].P)
                    {
                        temp[j].P--;
                    }
                }
            }
            for (int i = 1; i < temp.Length; i++)
            {
                //                Console.WriteLine("id so far = {0}", id);
                id *= (UInt64)fMod;
                id += (UInt64)temp[i].F;
            }
            return id;
        }
        #endregion

        #region Properties
        public UInt64 CurrentEdgeCubeId
        {
            get
            {
                if (Edges == null)
                {
                    return MaxEdgeCubeId;
                }
                return CubieArrayId(Edges, 2);
            }
        }
        public UInt64 CurrentCornerCubeId
        {
            get
            {
                if (Corners == null)
                {
                    return MaxCornerCubeId;
                }
                return CubieArrayId(Corners, 3);
            }
        }
        #endregion

        #region Operator Overloading
        // 
        /// <summary>
        ///Rubik's Cube multiplication is defined here lhs * rhs = product where 
        ///lhs is original physical cube and rhs represents the set of moves requred to
        ///permutate a solved cube to obtain the rhs cube state.  The product is the 
        ///result of applying those set of moves represented by rhs to the lhs cube.
        ///Example: rhs = U (result of turning the up face of a solved cube Right, 
        ///         lhs = R (result of turning the right face of a solved cube Right, 
        ///         product = UR (result of taking a solved cube and turning the up face Right then the Right face Right).  
        /// </summary>
        /// <param name="lhs">The current state of the Rubiks Cube</param>
        /// <param name="rhs">A rubiks cube whose state represents the set of moves to apply to the lhs cube.</param>
        /// <returns>The result of applying those moves represented by rhs to the lhs cube.</returns>
        public static RubikCube operator *(RubikCube lhs, RubikCube rhs)
        {
            Cubie[] retCorners = null;
            Cubie[] retEdges = null;
            if (lhs.Corners != null)
            {
                retCorners = new Cubie[lhs.Corners.Length];
            }
            if (lhs.Edges != null)
            {
                retEdges = new Cubie[lhs.Edges.Length];
            }
            Cubie[] retg = null;
            Cubie[] lhsg = null;
            Cubie[] rhsg = null;
            int FMod = 0;
            for (int k = 0; k < 2; k++)
            {
                if (k == 0)
                {
                    retg = retEdges;
                    lhsg = lhs.Edges;
                    rhsg = rhs.Edges;
                    FMod = 2;
                }
                else
                {
                    retg = retCorners;
                    lhsg = lhs.Corners;
                    rhsg = rhs.Corners;
                    FMod = 3;
                }
                if (retg != null && lhsg != null && rhsg != null)
                {
                    for (int i = 0; i < retg.Length; i++)
                    {
                        retg[i] = lhsg[rhsg[i].P];
                        retg[i].F += rhsg[i].F;
                        retg[i].F %= FMod;
                    }
                }
            }
            RubikCube retval = new RubikCube(retEdges, retCorners);
            retval.movesSoFar.AddRange(lhs.movesSoFar);
            retval.movesSoFar.AddRange(rhs.movesSoFar);
            return retval;
        }

        /// <summary>
        /// Creates the inverse of the represented cube.  For example -U = u and -(F) = (f).
        /// By definition X * -X = 1 where 1 represents the identity, doing nothing, or the solved cube.
        /// (ie: X * 1 = X).
        /// </summary>
        /// <param name="op">The cube whose inverse needs calculating</param>
        /// <returns></returns>
        public static RubikCube operator -(RubikCube op)
        {
            Cubie[] retCorners = null;
            Cubie[] retEdges = null;
            if (op.Corners != null)
            {
                retCorners = new Cubie[op.Corners.Length];
            }
            if (op.Edges != null)
            {
                retEdges = new Cubie[op.Edges.Length];
            }
            Cubie[] retg = null;
            Cubie[] opg = null;
            int FMod = 0;
            for (int k = 0; k < 2; k++)
            {
                if (k == 0)
                {
                    retg = retEdges;
                    opg = op.Edges;
                    FMod = 2;
                }
                else
                {
                    retg = retCorners;
                    opg = op.Corners;
                    FMod = 3;
                }

                if (retg != null && opg != null)
                {
                    for (int i = 0; i < retg.Length; i++)
                    {
                        retg[opg[i].P].P = i;
                        retg[opg[i].P].F = (FMod - opg[i].F) % FMod;
                    }
                }
            }
            RubikCube retval = new RubikCube(retEdges, retCorners);
            List<Move> newMovesSoFar = new List<Move>();
            for (int i = retval.movesSoFar.Count-1; i >= 0; --i)
            {
                switch (retval.movesSoFar[i])
                {
                    case Move.U: newMovesSoFar.Add(Move.U3); break;
                    case Move.U2: newMovesSoFar.Add(Move.U2); break;
                    case Move.U3: newMovesSoFar.Add(Move.U); break;
                    case Move.D: newMovesSoFar.Add(Move.D3); break;
                    case Move.D2: newMovesSoFar.Add(Move.D2); break;
                    case Move.D3: newMovesSoFar.Add(Move.D); break;
                    case Move.F: newMovesSoFar.Add(Move.F3); break;
                    case Move.F2: newMovesSoFar.Add(Move.F2); break;
                    case Move.F3: newMovesSoFar.Add(Move.F); break;
                    case Move.B: newMovesSoFar.Add(Move.B3); break;
                    case Move.B2: newMovesSoFar.Add(Move.B2); break;
                    case Move.B3: newMovesSoFar.Add(Move.B); break;
                    case Move.L: newMovesSoFar.Add(Move.L3); break;
                    case Move.L2: newMovesSoFar.Add(Move.L2); break;
                    case Move.L3: newMovesSoFar.Add(Move.L); break;
                    case Move.R: newMovesSoFar.Add(Move.R3); break;
                    case Move.R2: newMovesSoFar.Add(Move.R2); break;
                    case Move.R3: newMovesSoFar.Add(Move.R); break;
                }
            }
            retval.movesSoFar = newMovesSoFar;
            return retval;
        }
        /// <summary>
        ///Rubik's Cube addition is defined here lhs + rhs = sum where 
        ///lhs is original physical cube and rhs represents a cube translation.  
        ///The sum is the result of the lhs cube across the permutation represented by rhs.
        ///Sumation is most useful for rotating the entire cube to obtain a symetric cube with 
        ///different center cube colors.  There are 48 symmetric positions of a cube (6 face colors, 
        ///each with 4 possible up colors) x 2 for the cube in the mirror.
        ///Example: rhs = U (result of turning the up face of a solved cube Right, 
        ///         lhs = (F) hypothetical cube that would be the result of turning the entire cube right
        ///               about the face but leaving the center cubies unmoved.
        ///         new cube = R (if you take cube U and rotate the entire cube right about the face you end up with R.)  
        /// </summary>
        /// <param name="lhs">The current state of the Rubiks cube</param>
        /// <param name="rhs">The cube across which the current cube is translated and multiplied</param>
        /// <returns>The result of translating the current cube across the rhs permutation</returns>
        public static RubikCube operator +(RubikCube lhs, RubikCube rhs)
        {
            RubikCube retval = (-rhs * lhs) * rhs;
            Move[] translation = SymmetryMoveConversions[rhs];
            if (translation != null)
            {
                List<Move> newMovesSoFar = new List<Move>();
                for (int i = 0; i < retval.movesSoFar.Count; ++i)
                {
                    newMovesSoFar[i] = translation[(int)retval.movesSoFar[i]];
                }
                retval.movesSoFar = newMovesSoFar;
                retval.currentSymmetry = (lhs.currentSymmetry * rhs);
            }
            else
            {
                retval.movesSoFar.Clear();
                retval.isMirrored = false;
                retval.currentSymmetry = new RubikCube(0,0);
            }
            return retval;
        }

        public static RubikCube operator ~(RubikCube op)
        {
            RubikCube retVal = (-M * op) * M;
            // Invert parity on corners, Edges are their own inversion 
            // eg: (2 - 0)%2 == 0 && (2 - 1)%2 == 1 ==> (2 - x)%2 = x
            if (retVal.Corners != null)
            {
                for (int i = 0; i < retVal.Corners.Length; i++)
                {
                    retVal.Corners[i].F = (3 - retVal.Corners[i].F) % 3;
                }
            }
            List<Move> newMovesSoFar = new List<Move>();
            for (int i = 0; i < retVal.movesSoFar.Count; ++i)
            {
                switch (retVal.movesSoFar[i])
                {
                    case Move.U:  newMovesSoFar.Add(Move.U3); break;
                    case Move.U2: newMovesSoFar.Add(Move.U2); break;
                    case Move.U3: newMovesSoFar.Add(Move.U);  break;
                    case Move.D:  newMovesSoFar.Add(Move.D3); break;
                    case Move.D2: newMovesSoFar.Add(Move.D2); break;
                    case Move.D3: newMovesSoFar.Add(Move.D);  break;
                    case Move.F:  newMovesSoFar.Add(Move.F3); break;
                    case Move.F2: newMovesSoFar.Add(Move.F2); break;
                    case Move.F3: newMovesSoFar.Add(Move.F);  break;
                    case Move.B:  newMovesSoFar.Add(Move.B3); break;
                    case Move.B2: newMovesSoFar.Add(Move.B2); break;
                    case Move.B3: newMovesSoFar.Add(Move.B);  break;
                    case Move.L:  newMovesSoFar.Add(Move.R3); break;
                    case Move.L2: newMovesSoFar.Add(Move.R2); break;
                    case Move.L3: newMovesSoFar.Add(Move.R);  break;
                    case Move.R:  newMovesSoFar.Add(Move.L3); break;
                    case Move.R2: newMovesSoFar.Add(Move.L2); break;
                    case Move.R3: newMovesSoFar.Add(Move.L);  break;
                }
            }
            retVal.movesSoFar = newMovesSoFar;
            retVal.isMirrored = !retVal.isMirrored;
            return retVal;
        }

        public static bool operator ==(RubikCube lhs, RubikCube rhs)
        {
            Cubie[] lhsg = null;
            Cubie[] rhsg = null;
            for (int k = 0; k < 2; k++)
            {
                if (k == 0)
                {
                    lhsg = lhs.Edges;
                    rhsg = rhs.Edges;
                }
                else
                {
                    lhsg = lhs.Corners;
                    rhsg = rhs.Corners;
                }

                if (lhsg != null && rhsg != null)
                {
                    for (int i = 0; i < lhsg.Length; i++)
                    {
                        if (lhsg[i].P != rhsg[i].P ||
                            lhsg[i].F != rhsg[i].F)
                        {
                            return false;
                        }
                    }
                }
                else if (!(lhsg == null && rhsg == null))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool operator !=(RubikCube lhs, RubikCube rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof(RubikCube))
            {
                return false;
            }
            if ((RubikCube)o != this)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            UInt64 hash = CurrentCornerCubeId ^ CurrentEdgeCubeId;
            int retval = (int)hash ^ (int)(hash >> 32);
            return retval;
        }

        public static bool operator >(RubikCube lhs, RubikCube rhs)
        {
            ulong lhsEdgeId = lhs.CurrentEdgeCubeId;
            ulong rhsEdgeId = rhs.CurrentEdgeCubeId;
            if (lhsEdgeId > rhsEdgeId)
            {
                return true;
            }
            if (lhsEdgeId < rhsEdgeId)
            {
                return false;
            }
            if (lhs.CurrentCornerCubeId > rhs.CurrentCornerCubeId)
            {
                return true;
            }
            return false;
        }
        public static bool operator <(RubikCube lhs, RubikCube rhs)
        {
            ulong lhsEdgeId = lhs.CurrentEdgeCubeId;
            ulong rhsEdgeId = rhs.CurrentEdgeCubeId;
            if (lhsEdgeId < rhsEdgeId)
            {
                return true;
            }
            if (lhsEdgeId > rhsEdgeId)
            {
                return false;
            }
            if (lhs.CurrentCornerCubeId < rhs.CurrentCornerCubeId)
            {
                return true;
            }
            return false;
        }
        #endregion

        public void PrintTable()
        {
            if (Corners != null)
            {
                Console.WriteLine("Corner Table");
                System.Console.WriteLine("i  0    1    2    3    4    5    6    7");
                System.Console.WriteLine("P  {0}    {1}    {2}    {3}    {4}    {5}    {6}    {7}", Corners[0].P, Corners[1].P, Corners[2].P, Corners[3].P, Corners[4].P, Corners[5].P, Corners[6].P, Corners[7].P);
                System.Console.WriteLine("F  {0}    {1}    {2}    {3}    {4}    {5}    {6}    {7}", Corners[0].F, Corners[1].F, Corners[2].F, Corners[3].F, Corners[4].F, Corners[5].F, Corners[6].F, Corners[7].F);
            }
            if (Edges != null)
            {
                Console.WriteLine("Edge Table");
                System.Console.WriteLine("i  0    1    2    3    4    5    6    7    8    9    10   11");
                System.Console.WriteLine("P  {0}    {1}    {2}    {3}    {4}    {5}    {6}    {7}    {8}    {9}    {10}   {11}", Edges[0].P, Edges[1].P, Edges[2].P, Edges[3].P, Edges[4].P, Edges[5].P, Edges[6].P, Edges[7].P, Edges[8].P, Edges[9].P, Edges[10].P, Edges[11].P);
                System.Console.WriteLine("F  {0}    {1}    {2}    {3}    {4}    {5}    {6}    {7}    {8}    {9}    {10}   {11}", Edges[0].F, Edges[1].F, Edges[2].F, Edges[3].F, Edges[4].F, Edges[5].F, Edges[6].F, Edges[7].F, Edges[8].F, Edges[9].F, Edges[10].F, Edges[11].F);
            }
        }

        #region Primitive Cubes
        public static readonly RubikCube[] Moves_FTM;
        public static readonly RubikCube[] Symetries;
        private static RubikCube Mirror;
        public static RubikCube U { get { return Moves_FTM[(int)Move.U]; } }
        public static RubikCube u { get { return Moves_FTM[(int)Move.U3]; } }
        public static RubikCube U2 { get { return Moves_FTM[(int)Move.U2]; } }
        public static RubikCube R { get { return Moves_FTM[(int)Move.R]; } }
        public static RubikCube r { get { return Moves_FTM[(int)Move.R3]; } }
        public static RubikCube R2 { get { return Moves_FTM[(int)Move.R2]; } }
        public static RubikCube F { get { return Moves_FTM[(int)Move.F]; } }
        public static RubikCube f { get { return Moves_FTM[(int)Move.F3]; } }
        public static RubikCube F2 { get { return Moves_FTM[(int)Move.F2]; } }
        public static RubikCube D { get { return Moves_FTM[(int)Move.D]; } }
        public static RubikCube d { get { return Moves_FTM[(int)Move.D3]; } }
        public static RubikCube D2 { get { return Moves_FTM[(int)Move.D2]; } }
        public static RubikCube L { get { return Moves_FTM[(int)Move.L]; } }
        public static RubikCube l { get { return Moves_FTM[(int)Move.L3]; } }
        public static RubikCube L2 { get { return Moves_FTM[(int)Move.L2]; } }
        public static RubikCube B { get { return Moves_FTM[(int)Move.B]; } }
        public static RubikCube b { get { return Moves_FTM[(int)Move.B3]; } }
        public static RubikCube B2 { get { return Moves_FTM[(int)Move.B2]; } }

        public static RubikCube CF { get { return Symetries[(int)Symetry.F]; } }
        public static RubikCube CR { get { return Symetries[(int)Symetry.R]; } }
        public static RubikCube CU { get { return Symetries[(int)Symetry.U]; } }
        public static RubikCube CFi { get { return Symetries[(int)Symetry.Fi]; } }
        public static RubikCube CRi { get { return Symetries[(int)Symetry.Ri]; } }
        public static RubikCube CUi { get { return Symetries[(int)Symetry.Ui]; } }
        public static RubikCube M { get { return Mirror; } }
        #endregion

        #region Data
        private Cubie[] Corners;
        private Cubie[] Edges;
        private System.Collections.Generic.List<Move> movesSoFar;
        private RubikCube currentSymmetry;
        private bool isMirrored;
        #endregion

        // Unit tests
        public static void Main()
        {
            /*
            System.Collections.Hashtable last = new System.Collections.Hashtable();
            System.Collections.Hashtable now = new System.Collections.Hashtable();
            System.Collections.Hashtable[] cubesAtDepth = new System.Collections.Hashtable[7];
            cubesAtDepth[0] = new System.Collections.Hashtable();
            cubesAtDepth[1] = new System.Collections.Hashtable();
            cubesAtDepth[2] = new System.Collections.Hashtable();
            cubesAtDepth[3] = new System.Collections.Hashtable();
            cubesAtDepth[4] = new System.Collections.Hashtable(4000);
            cubesAtDepth[5] = new System.Collections.Hashtable(40000);
            cubesAtDepth[6] = new System.Collections.Hashtable(400000);

            RubikCube c = new RubikCube(0, 0);
            cubesAtDepth[0][c] = true;

            // Prime depth 1
            for (int i = 1; i < Moves_FTM.Length; i++)
            {
                RubikCube cubeSmallest = Moves_FTM[i];
                for (int j = 0; j < Symetries.Length; j++)
                {
                    RubikCube temp = Moves_FTM[i] + Symetries[j];
                    RubikCube tempMirror = (~Moves_FTM[i]) + Symetries[j];
                    if (temp != c)
                    {
                        if (temp < cubeSmallest)
                        {
                            cubeSmallest = temp;
                        }
                        if (tempMirror < cubeSmallest)
                        {
                            cubeSmallest = tempMirror;
                        }
                    }
                }
                cubesAtDepth[1][cubeSmallest] = true;
            }
            // Look deeper
            for (int depth = 2; depth < 7; depth++)
            {
                Console.WriteLine("Starting depth {0} with {1} values in depth {2}", depth, cubesAtDepth[depth - 1].Count, depth - 1);
                foreach (RubikCube cube in cubesAtDepth[depth - 1].Keys)
                {
                    if (cube.CurrentCornerCubeId == 0 || cube.CurrentEdgeCubeId == 0)
                    {
                        Console.WriteLine("FoundTranslator");
                        cube.PrintTable();
                    }
                    for (int i = 0; i < Moves_FTM.Length; i++)
                    {
                        RubikCube currentCube = cube * Moves_FTM[i];
                        RubikCube currentCubeMirror = ~currentCube;
                        RubikCube cubeSmallest = currentCube;
                        if (currentCubeMirror < cubeSmallest)
                        {
                            cubeSmallest = currentCubeMirror;
                        }
                        for (int j = 0; j < Symetries.Length; j++)
                        {
                            RubikCube temp = currentCube + Symetries[j];
                            RubikCube tempMirror = ~temp;
                            if (temp < cubeSmallest)
                            {
                                cubeSmallest = temp;
                            }
                            if (tempMirror < cubeSmallest)
                            {
                                cubeSmallest = tempMirror;
                            }
                        }
                        if (!cubesAtDepth[depth - 1].ContainsKey(cubeSmallest) &&
                            !cubesAtDepth[depth - 2].ContainsKey(cubeSmallest))
                        {
                            cubesAtDepth[depth][cubeSmallest] = true;
                        }
                    }
                }
            }
            Console.WriteLine("{0} values in depth 6", cubesAtDepth[6].Count);
            // RubikCube maxCube = null;
            foreach (RubikCube cn in cubesAtDepth[6].Keys)
            {
                if (cn.CurrentCornerCubeId == 0 || cn.CurrentEdgeCubeId == 0)
                {
                    Console.WriteLine("FoundTranslator");
                    cn.PrintTable();
                }
                // /*
                if (((object)maxCube) == null)
                {
                    maxCube = cn;
                }
                else if (cn > maxCube)
                {
                    maxCube = cn;
                }
                // * /
            }
            // Console.WriteLine("maxCube = ({0},{1})", maxCube.CurrentEdgeCubeId, maxCube.CurrentCornerCubeId);
            // maxCube.PrintTable();
            */


            RubikCube c = new RubikCube(0, 0);
            RubikCube cubeCornerOnly = new RubikCube(MaxEdgeCubeId, 0);
            for (int i = 0; i < Moves_FTM.Length; i++)
            {
                if (Moves_FTM[i] != c * Moves_FTM[i])
                {
                    Console.WriteLine("{0},{1} * {2},{3} != {2},{3}", c.CurrentEdgeCubeId, c.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                }
                if ((cubeCornerOnly * Moves_FTM[i]).CurrentCornerCubeId != Moves_FTM[i].CurrentCornerCubeId)
                {
                    Console.WriteLine("CornerOnly({0}) = {1}, {0} = {2}", (Move)i, (cubeCornerOnly * Moves_FTM[i]).CurrentCornerCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    Console.WriteLine("CornerOnly(I)");
                    cubeCornerOnly.PrintTable();
                    Console.WriteLine("I");
                    c.PrintTable();
                    Console.WriteLine("CornerOnly({0})", (Move)i);
                    (cubeCornerOnly * Moves_FTM[i]).PrintTable();
                    Console.WriteLine("\r\n{0}", (Move)i);
                    Moves_FTM[i].PrintTable();
                }
            }

            for (int i = 0; i < Moves_FTM.Length; i++)
            {
                for (int j = 0; j < Moves_FTM.Length; j++)
                {
                    if (Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] != c)
                    {
                        Console.WriteLine("Error");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] != cubeCornerOnly)
                    {
                        Console.WriteLine("Error2");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] == cubeCornerOnly)
                    {
                        Console.WriteLine("Error");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] == c)
                    {
                        Console.WriteLine("Error2");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if ((cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j]).CurrentCornerCubeId != (Moves_FTM[i] * Moves_FTM[j]).CurrentCornerCubeId)
                    {
                        Console.WriteLine("{}");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                }
            }


            System.Random rand = new Random(5);

            for (int i = 0; i < 100; i++)
            {
                ulong cornerid = (ulong)rand.Next((int)MaxCornerCubeId);
                ulong edgeid = (ulong)rand.Next(613122048) * (ulong)rand.Next(1600);

                try
                {
                    RubikCube cube = new RubikCube(edgeid, cornerid);
                    if (edgeid != cube.CurrentEdgeCubeId && cornerid != cube.CurrentCornerCubeId)
                    {
                        System.Console.WriteLine("edgeId {0}, cornerid {1}", edgeid, cornerid);
                        cube.PrintTable();
                        return;
                    }
                    RubikCube urTest = (cube + CU + CR + CU + CR + CU + CR);
                    RubikCube dfTest = (cube + -CU + CF + -CU + CF + -CU + CF);
                    RubikCube blTest = (cube + -CF + -CR + -CF + -CR + -CF + -CR);
                    if (edgeid != urTest.CurrentEdgeCubeId && cornerid != urTest.CurrentCornerCubeId)
                    {
                        System.Console.WriteLine("edgeId {0}, cornerid {1}", edgeid, cornerid);
                        urTest.PrintTable();
                        return;
                    }
                    if (edgeid != dfTest.CurrentEdgeCubeId && cornerid != dfTest.CurrentCornerCubeId)
                    {
                        System.Console.WriteLine("edgeId {0}, cornerid {1}", edgeid, cornerid);
                        dfTest.PrintTable();
                        return;
                    }
                    if (edgeid != blTest.CurrentEdgeCubeId && cornerid != blTest.CurrentCornerCubeId)
                    {
                        System.Console.WriteLine("edgeId {0}, cornerid {1}", edgeid, cornerid);
                        blTest.PrintTable();
                        return;
                    }
                }
                catch (Exception)
                {
                }
            }

            for (int i = 0; i < 100; i++)
            {
                ulong cornerid = (ulong)rand.Next((int)MaxCornerCubeId);
                RubikCube cornerCube = new RubikCube(MaxEdgeCubeId, (ulong)cornerid);

                if ((ulong)cornerid != cornerCube.CurrentCornerCubeId)
                {
                    System.Console.WriteLine("cornerid {0} != returned cornerid {1}", cornerid, cornerCube.CurrentCornerCubeId);
                    cornerCube.PrintTable();
                    return;
                }
                ulong corneridsmall = cornerid / MaxCornerF;
                corneridsmall *= MaxCornerF;
                if (!CompareP(cornerCube.Corners, (new RubikCube(MaxEdgeCubeId, corneridsmall)).Corners))
                {
                    System.Console.WriteLine("Not perm compare equal cornerid {0}, {2}, i={1}", cornerid, i, corneridsmall);
                    PrintSubGroup(cornerCube.Corners);
                    Console.WriteLine();
                    PrintSubGroup((new RubikCube(MaxEdgeCubeId, corneridsmall)).Corners);
                    return;
                }
            }


            for (int i = 0; i < 100; i++)
            {
                ulong edgeid = (ulong)rand.Next(613122048) * (ulong)rand.Next(1600);
                RubikCube edgeCube = new RubikCube((ulong)edgeid, MaxCornerCubeId);

                if ((ulong)edgeid != edgeCube.CurrentEdgeCubeId)
                {
                    System.Console.WriteLine("edgeId {0} != returned edgeId {1}", edgeid, edgeCube.CurrentEdgeCubeId);
                    edgeCube.PrintTable();
                    return;
                }
                ulong edgeidsmall = edgeid / MaxEdgeF;
                edgeidsmall *= MaxEdgeF;
                if (!CompareP(edgeCube.Edges, (new RubikCube(edgeidsmall, MaxCornerCubeId)).Edges))
                {
                    System.Console.WriteLine("Not perm compare equal edgeid {0}, {2}, i={1}", edgeid, i, edgeidsmall);
                    PrintSubGroup(edgeCube.Edges);
                    Console.WriteLine();
                    PrintSubGroup((new RubikCube(edgeidsmall, MaxCornerCubeId)).Edges);
                    return;
                }

            }

            {
                RubikCube cube1 = new RubikCube(0, 0);
                RubikCube cube2 = new RubikCube(0, 0);
                RubikCube cube3 = new RubikCube(0, 0);
                RubikCube cubeM = new RubikCube(0, 0);
                RubikCube cubeU2 = new RubikCube(0, 0);
                RubikCube[] cubeTurns = new RubikCube[]  { R, U, F, R, L2, D, B, U2, F2, U, L, R, D };
                RubikCube[] cubeUTurns = new RubikCube[] { F, U, L, F, B2, D, R, U2, L2, U, B, F, D };
                RubikCube[] cubeRTurns = new RubikCube[] { R, B, U, R, L2, F, D, B2, U2, B, L, R, F };
                RubikCube[] cubeMTurns = new RubikCube[] { l, u, f, l, R2, d, b, U2, F2, u, r, l, d };
                RubikCube[] cubeU2Turns = new RubikCube[]{ L, U, B, L, R2, D, F, U2, B2, U, R, L, D };
                for (int i = 0; i < 100; ++i)
                {
                    cube1 *= cubeTurns[i % cubeTurns.Length];
                    cube2 *= cubeUTurns[i % cubeUTurns.Length];
                    cube3 *= cubeRTurns[i % cubeRTurns.Length];
                    cubeM *= cubeMTurns[i % cubeMTurns.Length];
                    cubeU2 *= cubeU2Turns[i % cubeU2Turns.Length];
                    if (cube1 + CU != cube2)
                    {
                        System.Console.WriteLine("Test Failed!! after turn U {0}", i);
                    }
                    if (cube1 + CR != cube3)
                    {
                        System.Console.WriteLine("Test Failed!! after turn R {0}", i);
                    }
                    if (~cube1 != cubeM)
                    {
                        System.Console.WriteLine("Test Failed!! after turn R {0}", i);
                    }
                    if (cube1 + (CU * CU) != cubeU2 || cube1 + Symetries[(int)Symetry.U2] != cubeU2)
                    {
                        System.Console.WriteLine("Test Failed!! after turn U2 {0}", i);
                    }
                }
            }

            Console.WriteLine("Finished Tests!");

        }

        public int CompareTo(RubikCube other)
        {
            if (this < other) return -1;
            else if (this > other) return 1;
            return 0;
        }
    }

    //                              CORNERS                               EDGES
    //                                              UP FACE
    //                               /\ 
    //    LEFT FACE                 /0 \                               /\        /\       BACK SIDE
    //                              \U /                              /0 \  /\  /1 \
    //                               \/                               \U / /4 \ \U /
    //                          /\  /4 \  /\                           \/  \  /  \/ 
    //                         /3 \ \D / /1 \                      /\  /\   \/   /\  /\
    //                         \U /  /\  \U /                     /7  /3 \      /2 \  5\
    //                          \/  /2 \  \/                      \   \U /      \U /   /
    //                         /7 \ \U / /5 \                      \/  \/   /\   \/  \/
    //                         \D /  \/  \D /                           /\ /6 \   /\   
    //  FRONT FACE              \/  /6 \  \/                           /8 \\  /  /9 \      RIGHT SIDE
    //                              \D /                               \D / \/   \D /
    //                               \/                                 \/        \/
    //                                                                  /\        /\
    //                                                                 /11\      /10\
    //                                            DOWN FACE            \D /      \D /
    /// <summary>
    /// The diagram above shows the location of the slot at the specified array index.  The value of P found at that index is the id of the cube currently occupying that location.
    /// The solved Rubik Cube has the cubie with a P value of 0 at array index 0, 1 at array index 1 and so forth.
    /// The cubie F value reprents is twist.  A corner cubie can have a twist value of 0, 1, or 2.  An edge cubie can have a twist value of 0 or 1.  When a corner is twisted
    /// clockwise about its outside corner it has a twist value of 1, counter-clockwise gives it a twist value of 2.  An untwisted (with twist 0) cubie has it's up or down face
    /// facing either up or down.  If the cubie does not have either the up color or down color its front or back face must be facing front or back.
    /// </summary>
    public class RubikCube2 : IComparable<RubikCube2>
    {
        static RubikCube2()
        {
            Moves_FTM = new RubikCube2[(int)Move.NumMoves];
            Moves_FTM[(int)Move.U] = new RubikCube2(
                            new byte[8] { 3, 0, 1, 2, 4, 5, 6, 7 },
                            new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 },
                            new byte[12] { 3, 0, 1, 2, 4, 5, 6, 7, 8, 9, 10, 11 },
                            new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0 });

            Moves_FTM[(int)Move.D] = new RubikCube2(
                            new byte[8] { 0, 1, 2, 3, 5, 6, 7, 4 },
                            new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 },
                            new byte[12] { 0, 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 8 },
                            new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0,  0 });

            Moves_FTM[(int)Move.F] = new RubikCube2(
                            new byte[8] { 0, 1, 3, 7, 4, 5, 2, 6 },
                            new byte[8] { 0, 0, 1, 2, 0, 0, 2, 1 },
                            new byte[12] { 0, 1, 2, 7, 4, 5, 3, 11, 8, 9, 10, 6 },
                            new byte[12] { 0, 0, 0, 1, 0, 0, 1, 1,  0, 0, 0,  1 });

            Moves_FTM[(int)Move.B] = new RubikCube2(
                            new byte[8] { 1, 5, 2, 3, 0, 4, 6, 7 },
                            new byte[8] { 1, 2, 0, 0, 2, 1, 0, 0 },
                            new byte[12] { 0, 5, 2, 3, 1, 9, 6, 7, 8, 4, 10, 11 },
                            new byte[12] { 0, 1, 0, 0, 1, 1, 0, 0, 0, 1, 0,  0, });

            Moves_FTM[(int)Move.R] = new RubikCube2(
                            new byte[8] { 0, 2, 6, 3, 4, 1, 5, 7 },
                            new byte[8] { 0, 1, 2, 0, 0, 2, 1, 0 },
                            new byte[12] { 0, 1, 6, 3, 4, 2, 10, 7, 8, 9, 5, 11 },
                            new byte[12] { 0, 0, 0, 0, 0, 0, 0,  0, 0, 0, 0, 0 });


            Moves_FTM[(int)Move.L] = new RubikCube2(
                            new byte[8] { 4, 1, 2, 0, 7, 5, 6, 3 },
                            new byte[8] { 2, 0, 0, 1, 1, 0, 0, 2 },
                            new byte[12] { 4, 1, 2, 3, 8, 5, 6, 0, 7, 9, 10, 11 },
                            new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  0  });

            Moves_FTM[(int)Move.U2] = Moves_FTM[(int)Move.U] * Moves_FTM[(int)Move.U];
            Moves_FTM[(int)Move.D2] = Moves_FTM[(int)Move.D] * Moves_FTM[(int)Move.D];
            Moves_FTM[(int)Move.L2] = Moves_FTM[(int)Move.L] * Moves_FTM[(int)Move.L];
            Moves_FTM[(int)Move.R2] = Moves_FTM[(int)Move.R] * Moves_FTM[(int)Move.R];
            Moves_FTM[(int)Move.F2] = Moves_FTM[(int)Move.F] * Moves_FTM[(int)Move.F];
            Moves_FTM[(int)Move.B2] = Moves_FTM[(int)Move.B] * Moves_FTM[(int)Move.B];

            Moves_FTM[(int)Move.U3] = -Moves_FTM[(int)Move.U];
            Moves_FTM[(int)Move.D3] = -Moves_FTM[(int)Move.D];
            Moves_FTM[(int)Move.L3] = -Moves_FTM[(int)Move.L];
            Moves_FTM[(int)Move.R3] = -Moves_FTM[(int)Move.R];
            Moves_FTM[(int)Move.F3] = -Moves_FTM[(int)Move.F];
            Moves_FTM[(int)Move.B3] = -Moves_FTM[(int)Move.B];

            Moves_FTM[(int)Move.U].ultimateTwist = Face.U;
            Moves_FTM[(int)Move.U].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.D].ultimateTwist = Face.U;
            Moves_FTM[(int)Move.D].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.F].ultimateTwist = Face.F;
            Moves_FTM[(int)Move.F].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.B].ultimateTwist = Face.B;
            Moves_FTM[(int)Move.B].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.R].ultimateTwist = Face.R;
            Moves_FTM[(int)Move.R].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.L].ultimateTwist = Face.L;
            Moves_FTM[(int)Move.L].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.U2].ultimateTwist = Face.U;
            Moves_FTM[(int)Move.U2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.D2].ultimateTwist = Face.D;
            Moves_FTM[(int)Move.D2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.L2].ultimateTwist = Face.L;
            Moves_FTM[(int)Move.L2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.R2].ultimateTwist = Face.R;
            Moves_FTM[(int)Move.R2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.F2].ultimateTwist = Face.F;
            Moves_FTM[(int)Move.F2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.B2].ultimateTwist = Face.B;
            Moves_FTM[(int)Move.B2].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.U3].ultimateTwist = Face.U;
            Moves_FTM[(int)Move.U3].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.D3].ultimateTwist = Face.D;
            Moves_FTM[(int)Move.D3].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.L3].ultimateTwist = Face.L;
            Moves_FTM[(int)Move.L3].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.R3].ultimateTwist = Face.R;
            Moves_FTM[(int)Move.R3].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.F3].ultimateTwist = Face.F;
            Moves_FTM[(int)Move.F3].pentUltimateTwist = Face.NumFaces;
            Moves_FTM[(int)Move.B3].ultimateTwist = Face.B;
            Moves_FTM[(int)Move.B3].pentUltimateTwist = Face.NumFaces;

            
            noUD = new RubikCube2[] { Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3] };

            noU =  new RubikCube2[] { Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3],
                                      Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3] };

            noD = new RubikCube2[] {  Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3] };

            noLR = new RubikCube2[] { Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3] };
              
            noL = new RubikCube2[] {  Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3],
                                      Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3] };

            noR = new RubikCube2[] {  Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3], Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3],
                                      Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3] };

            noFB = new RubikCube2[] { Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3] };

            noF = new RubikCube2[] {  Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3],
                                      Moves_FTM[(int)Move.B], Moves_FTM[(int)Move.B2], Moves_FTM[(int)Move.B3] };

            noB = new RubikCube2[] {  Moves_FTM[(int)Move.L], Moves_FTM[(int)Move.L2], Moves_FTM[(int)Move.L3], Moves_FTM[(int)Move.R], Moves_FTM[(int)Move.R2], Moves_FTM[(int)Move.R3],
                                      Moves_FTM[(int)Move.U], Moves_FTM[(int)Move.U2], Moves_FTM[(int)Move.U3], Moves_FTM[(int)Move.D], Moves_FTM[(int)Move.D2], Moves_FTM[(int)Move.D3],
                                      Moves_FTM[(int)Move.F], Moves_FTM[(int)Move.F2], Moves_FTM[(int)Move.F3] };


            Symetries = new RubikCube2[(int)Symetry.NumSymetries][];  // 24 symetries
            for (int i = 0; i < (int)Symetry.NumSymetries; ++i) Symetries[i] = new RubikCube2[2];

            Symetries[(int)Symetry.F][0] = new RubikCube2(
                            new byte[8] { 4, 0, 3, 7, 5, 1, 2, 6 },
                            new byte[8] { 1, 2, 1, 2, 2, 1, 2, 1 },
                            new byte[12] { 8, 4, 0, 7, 9, 1, 3, 11, 10, 5, 2, 6 },
                            new byte[12] { 1, 1, 1, 1, 1, 1, 1, 1,  1,  1, 1, 1 });

            Symetries[(int)Symetry.R][0] = new RubikCube2(
                            new byte[8] { 3, 2, 6, 7, 0, 1, 5, 4 },
                            new byte[8] { 2, 1, 2, 1, 1, 2, 1, 2 },
                            new byte[12] { 7, 3, 6, 11, 0, 2, 10, 8, 4, 1, 5, 9 },
                            new byte[12] { 0, 1, 0, 1, 0, 0, 0, 0, 0, 1, 0, 1 });

//            Symetries[(int)Symetry.U] = new RubikCube(new Cubie[12] {
//                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
//                                new Cubie(7,1), new Cubie(4,1), new Cubie(5,1), new Cubie(6,1), 
//                                new Cubie(11,0), new Cubie(8,0), new Cubie(9,0), new Cubie(10,0)},
//                            new Cubie[8] { 
//                                new Cubie(3,0), new Cubie(0,0), new Cubie(1,0), new Cubie(2,0), 
//                                new Cubie(7,0), new Cubie(4,0), new Cubie(5,0), new Cubie(6,0)});

            Mirror = new RubikCube2(
                            new byte[8] { 1, 0, 3, 2, 5, 4, 7, 6 },
                            new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 },
                            new byte[12] { 2, 1, 0, 3, 5, 4, 7, 6, 10, 9, 8, 11 },
                            new byte[12] { 0, 0, 0, 0, 0, 0, 0, 0, 0,  0, 0, 0  });

            Symetries[(int)Symetry.R2][0] = Symetries[(int)Symetry.R][0] * Symetries[(int)Symetry.R][0];
            Symetries[(int)Symetry.F2][0] = Symetries[(int)Symetry.F][0] * Symetries[(int)Symetry.F][0];
            Symetries[(int)Symetry.Ri][0] = -Symetries[(int)Symetry.R][0];
            Symetries[(int)Symetry.Fi][0] = -Symetries[(int)Symetry.F][0];

            Symetries[(int)Symetry.U][0] = Symetries[(int)Symetry.Fi][0] * Symetries[(int)Symetry.Ri][0] * Symetries[(int)Symetry.F][0];
            Symetries[(int)Symetry.U2][0] = Symetries[(int)Symetry.U][0] * Symetries[(int)Symetry.U][0];
            Symetries[(int)Symetry.Ui][0] = -Symetries[(int)Symetry.U][0];
            Symetries[(int)Symetry.RF][0] = Symetries[(int)Symetry.R][0] * Symetries[(int)Symetry.F][0];
            Symetries[(int)Symetry.FU][0] = Symetries[(int)Symetry.F][0] * Symetries[(int)Symetry.U][0];
            Symetries[(int)Symetry.RU][0] = Symetries[(int)Symetry.R][0] * Symetries[(int)Symetry.U][0];
            Symetries[(int)Symetry.UR][0] = Symetries[(int)Symetry.U][0] * Symetries[(int)Symetry.R][0];

            Symetries[(int)Symetry.RFi][0] = -Symetries[(int)Symetry.RF][0];
            Symetries[(int)Symetry.FUi][0] = -Symetries[(int)Symetry.FU][0];
            Symetries[(int)Symetry.RUi][0] = -Symetries[(int)Symetry.RU][0];
            Symetries[(int)Symetry.URi][0] = -Symetries[(int)Symetry.UR][0];

            Symetries[(int)Symetry.RF2][0] = Symetries[(int)Symetry.R][0] * Symetries[(int)Symetry.F2][0];
            Symetries[(int)Symetry.RU2][0] = Symetries[(int)Symetry.R][0] * Symetries[(int)Symetry.U2][0];
            Symetries[(int)Symetry.FR2][0] = Symetries[(int)Symetry.F][0] * Symetries[(int)Symetry.R2][0];
            Symetries[(int)Symetry.FU2][0] = Symetries[(int)Symetry.F][0] * Symetries[(int)Symetry.U2][0];
            Symetries[(int)Symetry.UR2][0] = Symetries[(int)Symetry.U][0] * Symetries[(int)Symetry.R2][0];
            Symetries[(int)Symetry.UF2][0] = Symetries[(int)Symetry.U][0] * Symetries[(int)Symetry.F2][0];
            for (int i = 0; i < (int)Symetry.NumSymetries; ++i) Symetries[i][1] = -Symetries[i][0];
        }

        #region Private Types
        public enum Move
        {
            U, U2, U3, D, D2, D3, L, L2, L3, R, R2, R3, F, F2, F3, B, B2, B3, NumMoves
        }
        public enum Symetry
        {
            F, R, U, RF, FU, RU, UR,
            Fi, Ri, Ui, RFi, FUi, RUi, URi,
            F2, R2, U2, RF2, RU2, FR2, FU2, UR2, UF2,
            NumSymetries
        }
        public enum Face
        {
            U, D, L, R, F, B, NumFaces
        }
        #endregion

        #region Constructors
        private RubikCube2(byte[] cornerPerm, byte[] cornerTwist, byte[] edgePerm, byte[] edgeTwist, bool fast = false)
        {
            if (fast)
            {
                using (Profiler.TimeSection timer = new Profiler.TimeSection("RubikCube2(arrays) fast"))
                {
                    this.cornerPerm = cornerPerm;
                    this.cornerTwist = cornerTwist;
                    this.edgePerm = edgePerm;
                    this.edgeTwist = edgeTwist;
                }
            }
            else
            {
                using (Profiler.TimeSection timer = new Profiler.TimeSection("RubikCube2(arrays) slow"))
                {
                    this.cornerPerm = Coburn.Combinations.ShallowClone<byte>(cornerPerm);
                    this.cornerTwist = Coburn.Combinations.ShallowClone<byte>(cornerTwist);
                    this.edgePerm = Coburn.Combinations.ShallowClone<byte>(edgePerm);
                    this.edgeTwist = Coburn.Combinations.ShallowClone<byte>(edgeTwist);
                }
            }
        }
        public RubikCube2()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("RubikCube2()"))
            {
                cornerPerm = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                cornerTwist = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                edgePerm = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
                edgeTwist = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            }
        }
        public RubikCube2(ulong cornerPerm, ulong cornerTwist, ulong edgePerm, ulong edgeTwist)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("RubikCube2(ulongs)"))
            {
                if (cornerPerm > Coburn.Combinations.Factorial[8])
                    throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", Coburn.Combinations.Factorial[8]), "cornerPerm");
                if (cornerTwist > Coburn.Combinations.Pow3[8])
                    throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", Coburn.Combinations.Pow3[8]), "cornerTwist");
                if (edgePerm > Coburn.Combinations.Factorial[12])
                    throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", Coburn.Combinations.Factorial[8]), "edgePerm");
                if (edgeTwist > Coburn.Combinations.Pow2[12])
                    throw new System.ArgumentException(String.Format("The maximum value for this parameter is {0}", Coburn.Combinations.Pow2[8]), "edgeTwist");

                if (cornerPerm < Coburn.Combinations.Factorial[8])
                {
                    this.cornerPerm = new byte[8];
                    Coburn.Combinations.PermutationFromId(cornerPerm, 8, this.cornerPerm);
                }
                if (cornerTwist < Coburn.Combinations.Pow3[8])
                {
                    this.cornerTwist = new byte[8];
                    Coburn.Combinations.TwistFromId(cornerPerm, 3, this.cornerTwist);
                }
                if (edgePerm < Coburn.Combinations.Factorial[12])
                {
                    this.edgePerm = new byte[12];
                    Coburn.Combinations.PermutationFromId(edgePerm, 12, this.edgePerm);
                }
                if (edgeTwist < Coburn.Combinations.Pow2[12])
                {
                    this.edgeTwist = new byte[12];
                    Coburn.Combinations.PermutationFromId(edgeTwist, 2, this.edgeTwist);
                }
            }
        }
        #endregion

        #region Private helper functions
        static Move[] AddMovesTranslations(Move[] lhs, Move[] rhs)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("AddMoveTranslations()"))
            {
                if (lhs.Length != rhs.Length && lhs.Length != (int)Move.NumMoves) throw new Exception("AddMoveTranslations called with incorrect length move arrays");
                Move[] retval = new Move[(int)Move.NumMoves];
                for (Move move = (Move)0; move < Move.NumMoves; ++move)
                {
                    retval[(int)move] = rhs[(int)lhs[(int)move]];
                }
                return retval;
            }
        }
        private static bool IsPermutationEven(byte[] cornerPerm, byte[] edgePerm)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("IsPermutationEven()"))
            {
                int sum = 0;
                bool[] Visited = null;
                byte[] subgroup = null;
                for (int k = 0; k < 2; k++)
                {
                    if (k == 0) subgroup = cornerPerm; else subgroup = edgePerm;
                    Visited = new bool[subgroup.Length];

                    for (int i = 0; i < subgroup.Length; i++)
                    {
                        if (!Visited[i])
                        {
                            int count = 0;
                            int j = i;
                            while (subgroup[j] != i)
                            {
                                count++;
                                Visited[j] = true;
                                j = subgroup[j];
                            }
                            Visited[j] = true;
                            sum += count;
                        }
                    }
                }
                return (sum % 2 == 0);
            }
        }
        #endregion

        public ulong getCornerPermutationId()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getCornerPermutationId"))
            {
                return Coburn.Combinations.PermutationId(Coburn.Combinations.ShallowClone<byte>(cornerPerm), 8);
            }
        }
        public ulong getCornerTwistId()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getCornerTwistId"))
            {
                return Coburn.Combinations.TwistId(Coburn.Combinations.ShallowClone<byte>(cornerTwist), 3);
            }
        }
        public ulong getEdgePermutationId()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getEdgePermutationId"))
            {
                return Coburn.Combinations.PermutationId(Coburn.Combinations.ShallowClone<byte>(edgePerm), 12);
            }
        }
        public ulong getEdgeTwistId()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getEdgeTwistId"))
            {
                return Coburn.Combinations.TwistId(Coburn.Combinations.ShallowClone<byte>(edgeTwist), 2);
            }
        }

        public byte[] getCornerPermutation()
        {
            return Coburn.Combinations.ShallowClone<byte>(cornerPerm);
        }
        public byte[] getCornerTwist()
        {
            return Coburn.Combinations.ShallowClone<byte>(cornerTwist);
        }
        public byte[] getEdgePermutation()
        {
            return Coburn.Combinations.ShallowClone<byte>(edgePerm);
        }
        public byte[] getEdgeTwist()
        {
            return Coburn.Combinations.ShallowClone<byte>(edgeTwist);
        }

        public byte[] getCornerPositions(byte[] cornerIds)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getCornerPositions"))
            {
                byte[] retval = new byte[cornerIds.Length];
                int k = 0;
                for (int i = 0; i < cornerIds.Length; ++i)
                {
                    retval[k++] = (byte)Array.IndexOf<byte>(cornerPerm, cornerIds[i]);
                }
                return retval;
            }
        }
        public byte[] getEdgePositions(byte[] edgeIds)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getEdgePositions"))
            {
                byte[] retval = new byte[edgeIds.Length];
                int k = 0;
                for (int i = 0; i < edgeIds.Length; ++i)
                {
                    retval[k++] = (byte)Array.IndexOf<byte>(edgePerm, edgeIds[i]);
                }
                return retval;
            }
        }
        public RubikCube2[] getFruitfulMoves()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("getFruitfulMoves"))
            {
                switch (ultimateTwist)
                {
                    case Face.U:
                        if (pentUltimateTwist == Face.D)
                        {
                            return noUD;
                        }
                        else
                        {
                            return noU;
                        }
                    case Face.D:
                        if (pentUltimateTwist == Face.U)
                        {
                            return noUD;
                        }
                        else
                        {
                            return noD;
                        }
                    case Face.L:
                        if (pentUltimateTwist == Face.R)
                        {
                            return noLR;
                        }
                        else
                        {
                            return noL;
                        }
                    case Face.R:
                        if (pentUltimateTwist == Face.L)
                        {
                            return noLR;
                        }
                        else
                        {
                            return noR;
                        }
                    case Face.F:
                        if (pentUltimateTwist == Face.B)
                        {
                            return noFB;
                        }
                        else
                        {
                            return noF;
                        }
                    case Face.B:
                        if (pentUltimateTwist == Face.F)
                        {
                            return noFB;
                        }
                        else
                        {
                            return noB;
                        }
                    default:
                        return Moves_FTM;
                }
            }
        }

        #region Operator Overloading
        // 
        /// <summary>
        ///Rubik's Cube multiplication is defined here lhs * rhs = product where 
        ///lhs is original physical cube and rhs represents the set of moves requred to
        ///permutate a solved cube to obtain the rhs cube state.  The product is the 
        ///result of applying those set of moves represented by rhs to the lhs cube.
        ///Example: rhs = U (result of turning the up face of a solved cube Right, 
        ///         lhs = R (result of turning the right face of a solved cube Right, 
        ///         product = UR (result of taking a solved cube and turning the up face Right then the Right face Right).  
        /// </summary>
        /// <param name="lhs">The current state of the Rubiks Cube</param>
        /// <param name="rhs">A rubiks cube whose state represents the set of moves to apply to the lhs cube.</param>
        /// <returns>The result of applying those moves represented by rhs to the lhs cube.</returns>
        public static RubikCube2 operator *(RubikCube2 lhs, RubikCube2 rhs)
        {
            Profiler.incrementCounter("operator*()");
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator*()"))
            {
                byte[] retvalCornerPerm = null;
                byte[] retvalCornerTwist = null;
                byte[] retvalEdgePerm = null;
                byte[] retvalEdgeTwist = null;
                if (lhs.cornerPerm != null && rhs.cornerPerm != null)
                {
                    retvalCornerPerm = Coburn.Combinations.permutate(lhs.cornerPerm, rhs.cornerPerm);
                    if (lhs.cornerTwist != null && rhs.cornerTwist != null)
                    {
                        retvalCornerTwist = Coburn.Combinations.permutate(lhs.cornerTwist, rhs.cornerPerm);
                        retvalCornerTwist = Coburn.Combinations.twist(retvalCornerTwist, rhs.cornerTwist, 3);
                    }
                }
                if (lhs.edgePerm != null && rhs.edgePerm != null)
                {
                    retvalEdgePerm = Coburn.Combinations.permutate(lhs.edgePerm, rhs.edgePerm);
                    if (lhs.edgeTwist != null && rhs.edgeTwist != null)
                    {
                        retvalEdgeTwist = Coburn.Combinations.permutate(lhs.edgeTwist, rhs.edgePerm);
                        retvalEdgeTwist = Coburn.Combinations.twist(retvalEdgeTwist, rhs.edgeTwist, 2);
                    }
                }
                RubikCube2 retval = new RubikCube2(retvalCornerPerm, retvalCornerTwist, retvalEdgePerm, retvalEdgeTwist, true);
                retval.pentUltimateTwist = lhs.ultimateTwist;
                retval.ultimateTwist = rhs.ultimateTwist;
                return retval;
            }
        }

        /// <summary>
        /// Creates the inverse of the represented cube.  For example -U = u and -(F) = (f).
        /// By definition X * -X = 1 where 1 represents the identity, doing nothing, or the solved cube.
        /// (ie: X * 1 = X).
        /// </summary>
        /// <param name="op">The cube whose inverse needs calculating</param>
        /// <returns></returns>
        public static RubikCube2 operator -(RubikCube2 op)
        {
            Profiler.incrementCounter("operator-()");
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator-()"))
            {
                byte[] retCornerPerm = null;
                byte[] retEdgePerm = null;
                byte[] retCornerTwist = null;
                byte[] retEdgeTwist = null;
                if (op.cornerPerm != null) retCornerPerm = new byte[8];
                if (op.cornerTwist != null) retCornerTwist = new byte[8];
                if (op.edgePerm != null) retEdgePerm = new byte[12];
                if (op.edgeTwist != null) retEdgeTwist = new byte[12];
                byte FMod = 0;
                for (int k = 0; k < 2; k++)
                {
                    byte[] retPerm;
                    byte[] retTwist;
                    byte[] opPerm;
                    byte[] opTwist;
                    if (k == 0)
                    {
                        retPerm = retEdgePerm;
                        retTwist = retEdgeTwist;
                        opPerm = op.edgePerm;
                        opTwist = op.edgeTwist;
                        FMod = 2;
                    }
                    else
                    {
                        retPerm = retCornerPerm;
                        retTwist = retCornerTwist;
                        opPerm = op.cornerPerm;
                        opTwist = op.cornerTwist;
                        FMod = 3;
                    }
                    if (retPerm != null && retTwist != null && opPerm != null && opTwist != null)
                    {
                        for (byte i = 0; i < retPerm.Length; ++i)
                        {
                            retPerm[opPerm[i]] = i;
                            retTwist[opPerm[i]] = (byte)((FMod - opTwist[i]) % FMod);
                        }
                    }
                }
                return new RubikCube2(retCornerPerm, retCornerTwist, retEdgePerm, retEdgeTwist, true);
            }
        }
        /// <summary>
        ///Rubik's Cube addition is defined here lhs + rhs = sum where 
        ///lhs is original physical cube and rhs represents a cube translation.  
        ///The sum is the result of the lhs cube across the permutation represented by rhs.
        ///Sumation is most useful for rotating the entire cube to obtain a symetric cube with 
        ///different center cube colors.  There are 48 symmetric positions of a cube (6 face colors, 
        ///each with 4 possible up colors) x 2 for the cube in the mirror.
        ///Example: rhs = U (result of turning the up face of a solved cube Right, 
        ///         lhs = (F) hypothetical cube that would be the result of turning the entire cube right
        ///               about the face but leaving the center cubies unmoved.
        ///         new cube = R (if you take cube U and rotate the entire cube right about the face you end up with R.)  
        /// </summary>
        /// <param name="lhs">The current state of the Rubiks cube</param>
        /// <param name="rhs">The cube across which the current cube is translated and multiplied</param>
        /// <returns>The result of translating the current cube across the rhs permutation</returns>
        public static RubikCube2 operator +(RubikCube2 lhs, RubikCube2 rhs)
        {
            Profiler.incrementCounter("operator+()");
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator+()"))
            {
                RubikCube2 retval = ((-rhs) * lhs) * rhs;
                return retval;
            }
        }

        public static RubikCube2 FastRotate(RubikCube2 cube, Symetry sym)
        {
            Profiler.incrementCounter("FastRotate");
            using (Profiler.TimeSection timer = new Profiler.TimeSection("FastRotate"))
            {
                return (Symetries[(int)sym][1] * cube) * Symetries[(int)sym][0];
            }
        }

        public static RubikCube2 operator ~(RubikCube2 op)
        {
            Profiler.incrementCounter("operator ~()");
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator ~()"))
            {
                RubikCube2 retVal = (-M * op) * M;
                // Invert parity on corners, Edges are their own inversion 
                // eg: (2 - 0)%2 == 0 && (2 - 1)%2 == 1 ==> (2 - x)%2 = x
                if (retVal.cornerTwist != null)
                {
                    for (int i = 0; i < retVal.cornerTwist.Length; i++)
                    {
                        retVal.cornerTwist[i] = (byte)((3 - retVal.cornerTwist[i]) % 3);
                    }
                }
                return retVal;
            }
        }

        public static int CompareArray(byte[] lhs, byte[] rhs)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("Compare(byte[], byte[])"))
            {
                if (lhs == null)
                {
                    if (rhs == null) return 0;
                    else return -1;
                }
                else
                {
                    if (rhs == null) return 1;
                }
                if (lhs.Length < rhs.Length) return -1;
                if (lhs.Length > rhs.Length) return 1;
                for (int i = 0; i < lhs.Length; ++i)
                {
                    if (lhs[i] < rhs[i]) return -1;
                    if (lhs[i] > rhs[i]) return 1;
                }
                return 0;
            }
        }

        public static bool operator ==(RubikCube2 lhs, RubikCube2 rhs)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator==()"))
            {
                int retval = 0;
                for (int i = 0; i < 4 && 0 == retval; ++i)
                {
                    switch (i)
                    {
                        case 0: retval = CompareArray(lhs.cornerPerm, rhs.cornerPerm); break;
                        case 1: retval = CompareArray(lhs.cornerTwist, rhs.cornerTwist); break;
                        case 2: retval = CompareArray(lhs.edgePerm, rhs.edgePerm); break;
                        case 3: retval = CompareArray(lhs.edgeTwist, rhs.edgeTwist); break;
                        default: break;
                    }
                }
                return (0 == retval);
            }
        }
        public static bool operator !=(RubikCube2 lhs, RubikCube2 rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(Object o)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("Equals()"))
            {
                if (o.GetType() != typeof(RubikCube2))
                {
                    return false;
                }
                if ((RubikCube2)o != this)
                {
                    return false;
                }
                return true;
            }
        }

        public override int GetHashCode()
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("GetHashCode()"))
            {
                UInt64 hash = getCornerPermutationId() << 32;
                hash |= getEdgePermutationId();
                UInt64 hash2 = getCornerTwistId() << 32;
                hash |= getEdgeTwistId();
                hash ^= hash2;
                return (int)hash ^ (int)(hash >> 32);
            }
        }

        public static bool operator >(RubikCube2 lhs, RubikCube2 rhs)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("operator>()"))
            {
                int retval = 0;
                for (int i = 0; i < 4 && 0 == retval; ++i)
                {
                    switch (i)
                    {
                        case 0: retval = CompareArray(lhs.cornerPerm, rhs.cornerPerm); break;
                        case 1: retval = CompareArray(lhs.cornerTwist, rhs.cornerTwist); break;
                        case 2: retval = CompareArray(lhs.edgePerm, rhs.edgePerm); break;
                        case 3: retval = CompareArray(lhs.edgeTwist, rhs.edgeTwist); break;
                        default: break;
                    }
                }
                return (retval > 0);
            }
        }
        public static bool operator <(RubikCube2 lhs, RubikCube2 rhs)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("Operator<()"))
            {
                int retval = 0;
                for (int i = 0; i < 4 && 0 == retval; ++i)
                {
                    switch (i)
                    {
                        case 0: retval = CompareArray(lhs.cornerPerm, rhs.cornerPerm); break;
                        case 1: retval = CompareArray(lhs.cornerTwist, rhs.cornerTwist); break;
                        case 2: retval = CompareArray(lhs.edgePerm, rhs.edgePerm); break;
                        case 3: retval = CompareArray(lhs.edgeTwist, rhs.edgeTwist); break;
                        default: break;
                    }
                }
                return (retval < 0);
            }
        }
        #endregion

        #region Primitive Cubes
        public static readonly RubikCube2[] Moves_FTM;
        public static readonly RubikCube2[][] Symetries;
        public static readonly RubikCube2[] noU;
        public static readonly RubikCube2[] noD;
        public static readonly RubikCube2[] noUD;
        public static readonly RubikCube2[] noF;
        public static readonly RubikCube2[] noB;
        public static readonly RubikCube2[] noFB;
        public static readonly RubikCube2[] noR;
        public static readonly RubikCube2[] noL;
        public static readonly RubikCube2[] noLR;
        private static RubikCube2 Mirror;
        public static RubikCube2 U { get { return Moves_FTM[(int)Move.U]; } }
        public static RubikCube2 u { get { return Moves_FTM[(int)Move.U3]; } }
        public static RubikCube2 U2 { get { return Moves_FTM[(int)Move.U2]; } }
        public static RubikCube2 R { get { return Moves_FTM[(int)Move.R]; } }
        public static RubikCube2 r { get { return Moves_FTM[(int)Move.R3]; } }
        public static RubikCube2 R2 { get { return Moves_FTM[(int)Move.R2]; } }
        public static RubikCube2 F { get { return Moves_FTM[(int)Move.F]; } }
        public static RubikCube2 f { get { return Moves_FTM[(int)Move.F3]; } }
        public static RubikCube2 F2 { get { return Moves_FTM[(int)Move.F2]; } }
        public static RubikCube2 D { get { return Moves_FTM[(int)Move.D]; } }
        public static RubikCube2 d { get { return Moves_FTM[(int)Move.D3]; } }
        public static RubikCube2 D2 { get { return Moves_FTM[(int)Move.D2]; } }
        public static RubikCube2 L { get { return Moves_FTM[(int)Move.L]; } }
        public static RubikCube2 l { get { return Moves_FTM[(int)Move.L3]; } }
        public static RubikCube2 L2 { get { return Moves_FTM[(int)Move.L2]; } }
        public static RubikCube2 B { get { return Moves_FTM[(int)Move.B]; } }
        public static RubikCube2 b { get { return Moves_FTM[(int)Move.B3]; } }
        public static RubikCube2 B2 { get { return Moves_FTM[(int)Move.B2]; } }

        public static RubikCube2 CF { get { return Symetries[(int)Symetry.F][0]; } }
        public static RubikCube2 CR { get { return Symetries[(int)Symetry.R][0]; } }
        public static RubikCube2 CU { get { return Symetries[(int)Symetry.U][0]; } }
        public static RubikCube2 CFi { get { return Symetries[(int)Symetry.Fi][0]; } }
        public static RubikCube2 CRi { get { return Symetries[(int)Symetry.Ri][0]; } }
        public static RubikCube2 CUi { get { return Symetries[(int)Symetry.Ui][0]; } }
        public static RubikCube2 M { get { return Mirror; } }
        #endregion

        #region Data
        private byte[] cornerPerm;
        private byte[] cornerTwist;
        private byte[] edgePerm;
        private byte[] edgeTwist;
        private Face ultimateTwist;
        private Face pentUltimateTwist;
        #endregion

        public int CompareTo(RubikCube2 other)
        {
            using (Profiler.TimeSection timer = new Profiler.TimeSection("CompareTo()"))
            {
                int retval = 0;
                for (int i = 0; i < 4 && 0 == retval; ++i)
                {
                    switch (i)
                    {
                        case 0: retval = CompareArray(this.cornerPerm, other.cornerPerm); break;
                        case 1: retval = CompareArray(this.cornerTwist, other.cornerTwist); break;
                        case 2: retval = CompareArray(this.edgePerm, other.edgePerm); break;
                        case 3: retval = CompareArray(this.edgeTwist, other.edgeTwist); break;
                        default: break;
                    }
                }
                return retval;
            }
        }

        public string Table
        {
            get
            {
                return String.Format("{0}\n{1}\n{2}\n{3}", Coburn.Combinations.stringify<byte>(cornerPerm),
                                                           Coburn.Combinations.stringify<byte>(cornerTwist),
                                                           Coburn.Combinations.stringify<byte>(edgePerm),
                                                           Coburn.Combinations.stringify<byte>(edgeTwist));
            }
        }

        public static void Main()
        {
            // Unit Tests

            RubikCube2 c = new RubikCube2();
            RubikCube2 cubeCornerOnly = new RubikCube2(0, 0, Coburn.Combinations.Factorial[12], Coburn.Combinations.Pow2[12]);
            for (int i = 0; i < Moves_FTM.Length; i++)
            {
                if (Moves_FTM[i] != c * Moves_FTM[i])
                {
                    Console.WriteLine("Identity test failed {0}", i);
                }
                if (Moves_FTM[i] != Moves_FTM[i] * c)
                {
                    Console.WriteLine("Identity test failed {0}", i);
                }
                if ((cubeCornerOnly * Moves_FTM[i]).getCornerPermutationId() != Moves_FTM[i].getCornerPermutationId())
                {
                    Console.WriteLine("Half Identity perm test failed {0}", i);
                }
                if ((cubeCornerOnly * Moves_FTM[i]).getCornerTwistId() != Moves_FTM[i].getCornerTwistId())
                {
                    Console.WriteLine("Half Identity twist test failed {0}", i);
                }
            }

            for (int i = 0; i < Moves_FTM.Length; i++)
            {
                for (int j = 0; j < Moves_FTM.Length; j++)
                {
                    if (Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] != c)
                    {
                        Console.WriteLine("Error");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] != cubeCornerOnly)
                    {
                        Console.WriteLine("Error2");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] == cubeCornerOnly)
                    {
                        Console.WriteLine("Error");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if (cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j] * -Moves_FTM[j] * -Moves_FTM[i] == c)
                    {
                        Console.WriteLine("Error2");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                    if ((cubeCornerOnly * Moves_FTM[i] * Moves_FTM[j]).getCornerPermutationId() != (Moves_FTM[i] * Moves_FTM[j]).getCornerPermutationId())
                    {
                        Console.WriteLine("{}");//, cube.CurrentEdgeCubeId, cube.CurrentCornerCubeId, Moves_FTM[i].CurrentEdgeCubeId, Moves_FTM[i].CurrentCornerCubeId);
                    }
                }
            }


            System.Random rand = new Random(5);

            for (int i = 0; i < 100; i++)
            {
                ulong cornerPermId = (ulong)rand.Next((int)Coburn.Combinations.Factorial[8]);
                ulong edgePermId = (ulong)rand.Next((int)Coburn.Combinations.Factorial[12]);
                ulong cornerTwistId = (ulong)rand.Next((int)Coburn.Combinations.Pow3[8]);
                ulong edgeTwistId = (ulong)rand.Next((int)Coburn.Combinations.Pow2[12]);

                try
                {
                    RubikCube2 cube = new RubikCube2(cornerPermId, cornerTwistId, edgePermId, edgeTwistId);
                    if (cornerPermId != cube.getCornerPermutationId() || cornerTwistId != cube.getCornerTwistId()
                        || edgePermId != cube.getEdgePermutationId() || edgeTwistId != cube.getEdgeTwistId())
                    {
                        System.Console.WriteLine("round trip test failed for {0}, {1}, {2}, {3}", cornerPermId, cornerTwistId, edgePermId, edgeTwistId);
                        return;
                    }
                    RubikCube2 urTest = (cube + CU + CR + CU + CR + CU + CR);
                    RubikCube2 dfTest = (cube + -CU + CF + -CU + CF + -CU + CF);
                    RubikCube2 blTest = (cube + -CF + -CR + -CF + -CR + -CF + -CR);
                    if (urTest != cube)
                    {
                        System.Console.WriteLine("failed urTest");
                        return;
                    }
                    if (dfTest != cube)
                    {
                        System.Console.WriteLine("failed dfTest");
                        return;
                    }
                    if (blTest != cube)
                    {
                        System.Console.WriteLine("failed blTest");
                        return;
                    }
                }
                catch (Exception)
                {
                }
            }

            {
                if ((U*F) + CU != U*L)
                {
                    RubikCube2 lhs = (U * F) + CU;
                    RubikCube2 rhs = U * L;
                    Console.WriteLine();
                }
                RubikCube2 cube1 = new RubikCube2();
                RubikCube2 cube2 = new RubikCube2();
                RubikCube2 cube3 = new RubikCube2();
                RubikCube2 cubeM = new RubikCube2();
                RubikCube2 cubeU2 = new RubikCube2();
                RubikCube2[] cubeTurns = new RubikCube2[] { R, U, F, R, L2, D, B, U2, F2, U, L, R, D };
                RubikCube2[] cubeUTurns = new RubikCube2[] { F, U, L, F, B2, D, R, U2, L2, U, B, F, D };
                RubikCube2[] cubeRTurns = new RubikCube2[] { R, B, U, R, L2, F, D, B2, U2, B, L, R, F };
                RubikCube2[] cubeMTurns = new RubikCube2[] { l, u, f, l, R2, d, b, U2, F2, u, r, l, d };
                RubikCube2[] cubeU2Turns = new RubikCube2[] { L, U, B, L, R2, D, F, U2, B2, U, R, L, D };
                for (int i = 0; i < 100; ++i)
                {
                    cube1 *= cubeTurns[i % cubeTurns.Length];
                    cube2 *= cubeUTurns[i % cubeUTurns.Length];
                    cube3 *= cubeRTurns[i % cubeRTurns.Length];
                    cubeM *= cubeMTurns[i % cubeMTurns.Length];
                    cubeU2 *= cubeU2Turns[i % cubeU2Turns.Length];
                    if (cube1 + CU != cube2)
                    {
                        System.Console.WriteLine("Test Failed!! after turn U {0}", i);
                    }
                    if (cube1 + CR != cube3)
                    {
                        System.Console.WriteLine("Test Failed!! after turn R {0}", i);
                    }
                    if (~cube1 != cubeM)
                    {
                        System.Console.WriteLine("Test Failed!! after turn R {0}", i);
                    }
                    if (cube1 + (CU * CU) != cubeU2 || cube1 + Symetries[(int)Symetry.U2][0] != cubeU2)
                    {
                        System.Console.WriteLine("Test Failed!! after turn U2 {0}", i);
                    }
                }
            }
            Console.WriteLine("Finished Tests!");
        }
    }

//    public delegate bool PruneFunc(T state);
//    public delegate void NextStatesFunc(T currentState, out T[] nextStates, out string[] edgesToNextStates);
//    public delegate bool EqualFunc(T lhs, T rhs);
    public class Solver
    {
        public static RubikCube2[] LegalSymmetries = new RubikCube2[] { RubikCube2.CU,
                                                                        RubikCube2.CU*RubikCube2.CU,
                                                                        RubikCube2.CUi,
                                                                        RubikCube2.CR*RubikCube2.CR,
                                                                        RubikCube2.CR*RubikCube2.CR*RubikCube2.CU,
                                                                        RubikCube2.CR*RubikCube2.CR*RubikCube2.CU*RubikCube2.CU,
                                                                        RubikCube2.CR*RubikCube2.CR*RubikCube2.CUi };

        public static RubikCube2.Symetry[] LegalSymetryIds = new RubikCube2.Symetry[] { RubikCube2.Symetry.U,
                                                                                        RubikCube2.Symetry.U2,
                                                                                        RubikCube2.Symetry.Ui,
                                                                                        RubikCube2.Symetry.F2,
                                                                                        RubikCube2.Symetry.UF2,
                                                                                        RubikCube2.Symetry.UR2,
                                                                                        RubikCube2.Symetry.R2 };

        public class Phase1Coordinate : IComparable<Phase1Coordinate>
        {
            static Phase1Coordinate()
            {
                s_udslice = new byte[] { 4, 5, 6, 7 };
            }
            private static byte[] s_udslice;
            //public Phase1Coordinate(ulong cornerTwist, ulong edgeTwist, ulong combination)
            //{
            //    Profiler.incrementCounter("Phase1Coordinate()");
            //    using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate()"))
            //    {
            //        cornerTwistID = cornerTwist;
            //        edgeTwistID = edgeTwist;
            //        udSliceCombination = combination;
            //    }
            //}
            public Phase1Coordinate(RubikCube2 cube)
            {
                this.reference = cube;
            }

            // Looking for twist to all be zero and UD slice edges to be in UD slice.
            // So we need to know the twist of all corners and edges and location of
            // the UD slice edges regardless of order.  When zero, we can move to Phase2
            //public ulong cornerTwistID; // between [0, 3^7) 2187
            //public ulong edgeTwistID; // between [0, 2^11) 2048
            //public ulong udSliceCombination; // [0, 495) [(12*11*10*9)/(4*3*2)]
            public RubikCube2 reference;
            public UInt32 ID
            {
                get
                {
                    Profiler.incrementCounter("Phase1Coordinate::ID");
                    using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Corrdinate::ID()"))
                    {
                        byte[] udslice = reference.getEdgePositions(s_udslice);
                        Array.Sort<byte>(udslice);
                        ulong rawUDSliceID = Combinations.CombinationId(udslice, (byte)12);
                        //return new Phase1Coordinate(cube.getCornerTwistId()/3, cube.getEdgeTwistId()/2, (rawUDSliceID + (495 - UDSliceZero))%495);
                        return Convert.ToUInt32((reference.getCornerTwistId() / 3) * 2048 * 495) + Convert.ToUInt32((reference.getEdgeTwistId() / 2) * 495) + Convert.ToUInt32((rawUDSliceID + (495 - UDSliceZero)) % 495);
                    }
                }
            }
            private static ulong udSliceZeroId = 0;
            public static ulong UDSliceZero
            {
                get
                {
                    Profiler.incrementCounter("UDSliceZero");
                    using (Profiler.TimeSection timer = new Profiler.TimeSection("UDSliceZero()"))
                    {
                        if (udSliceZeroId == 0)
                        {
                            udSliceZeroId = Combinations.CombinationId(new byte[] { 4, 5, 6, 7 }, 12);
                        }
                    }
                    return udSliceZeroId;
                }
            }
            public static bool operator < (Phase1Coordinate lhs, Phase1Coordinate rhs)
            {
                Profiler.incrementCounter("Phase1Coordinate::operator<()");
                using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate::operator<()"))
                {
                    int ctwist = RubikCube2.CompareArray(lhs.reference.getCornerTwist(), rhs.reference.getCornerTwist());
                    if (ctwist < 0) return true;
                    if (ctwist > 0) return false;
                    int etwist = RubikCube2.CompareArray(lhs.reference.getEdgeTwist(), rhs.reference.getEdgeTwist());
                    if (etwist < 0) return true;
                    if (etwist > 0) return false;

                    byte[] lhsUdSlice = lhs.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(lhsUdSlice);
                    byte[] rhsUdSlice = rhs.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(rhsUdSlice);
                    if (RubikCube2.CompareArray(lhsUdSlice, rhsUdSlice) < 0) return true;
                    return false;

//                    if (lhs.cornerTwistID < rhs.cornerTwistID) return true;
//                    else if (rhs.cornerTwistID < lhs.cornerTwistID) return false;
//                    else if (lhs.edgeTwistID < rhs.edgeTwistID) return true;
//                    else if (rhs.edgeTwistID < lhs.edgeTwistID) return false;
//                    else if (lhs.udSliceCombination < rhs.udSliceCombination) return true;
//                    return false;
                }
            }
            public static bool operator >(Phase1Coordinate lhs, Phase1Coordinate rhs)
            {
                Profiler.incrementCounter("Phase1Coordinate::operator>()");
                using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate::operator>()"))
                {
                    int ctwist = RubikCube2.CompareArray(lhs.reference.getCornerTwist(), rhs.reference.getCornerTwist());
                    if (ctwist > 0) return true;
                    if (ctwist < 0) return false;
                    int etwist = RubikCube2.CompareArray(lhs.reference.getEdgeTwist(), rhs.reference.getEdgeTwist());
                    if (etwist > 0) return true;
                    if (etwist < 0) return false;

                    byte[] lhsUdSlice = lhs.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(lhsUdSlice);
                    byte[] rhsUdSlice = rhs.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(rhsUdSlice);
                    if (RubikCube2.CompareArray(lhsUdSlice, rhsUdSlice) > 0) return true;
                    return false;

//                    if (lhs.cornerTwistID > rhs.cornerTwistID) return true;
//                    else if (rhs.cornerTwistID > lhs.cornerTwistID) return false;
//                    else if (lhs.edgeTwistID > rhs.edgeTwistID) return true;
//                    else if (rhs.edgeTwistID > lhs.edgeTwistID) return false;
//                    else if (lhs.udSliceCombination > rhs.udSliceCombination) return true;
//                    return false;
                }
            }
            public static Phase1Coordinate FromCube(RubikCube2 cube)
            {
                Profiler.incrementCounter("Phase1Coordinate::FromCube");
                using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate::FromCube()"))
                {
                    return new Phase1Coordinate(cube);
//                    byte[] udslice = cube.getEdgePositions(s_udslice);
//                    Array.Sort<byte>(udslice);
//                    ulong rawUDSliceID = Combinations.CombinationId(udslice, (byte)12);
//                    return new Phase1Coordinate(cube.getCornerTwistId()/3, cube.getEdgeTwistId()/2, (rawUDSliceID + (495 - UDSliceZero))%495);
                }
            }
//            public static Phase1Coordinate FromID(UInt32 id)
//            {
//                Profiler.incrementCounter("Phase1Coordinate::FromID");
//                using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate::FromID()"))
//                {
//                    ulong ct, et, uds;
//                    uds = id % 495; id /= 495;
//                    et = id % 2048; id /= 2048;
//                    ct = id;
//                    return new Phase1Coordinate(ct, et, uds);
//                }
//            }
            public static ulong[] SymetriesFromCube(RubikCube2 cube)
            {
                Profiler.incrementCounter("Phase1Coordinate::SymetriesFromCube");
                using (Profiler.TimeSection timer = new Profiler.TimeSection("Phase1Coordinate::SymetriesFromCube()"))
                {
                    RubikCube2 cubem = ~cube;
                    SortedSet<Phase1Coordinate> results = new SortedSet<Phase1Coordinate>();
                    results.Add(FromCube(cube));
                    results.Add(FromCube(cubem));
                    for (int i = 0; i < Solver.LegalSymetryIds.Length; ++i)
                    {
                        //RubikCube2 sym = cube+Solver.LegalSymmetries[i];
                        RubikCube2 sym = RubikCube2.FastRotate(cube, LegalSymetryIds[i]);
                        RubikCube2 symm = RubikCube2.FastRotate(cubem, LegalSymetryIds[i]);
                        results.Add(FromCube(sym));
                        results.Add(FromCube(symm));
                    }
                    ulong[] retval = new ulong[results.Count];
                    int j = 0;
                    foreach (Phase1Coordinate c in results)
                    {
                        retval[j++] = c.ID;
                    }
                    Array.Sort(retval);
                    return retval;
                }
            }

            public int CompareTo(Phase1Coordinate other)
            {
                
                    int ctwist = RubikCube2.CompareArray(this.reference.getCornerTwist(), other.reference.getCornerTwist());
                    if (ctwist < 0) return -1;
                    if (ctwist > 0) return 1;
                    int etwist = RubikCube2.CompareArray(this.reference.getEdgeTwist(), other.reference.getEdgeTwist());
                    if (etwist < 0) return -1;
                    if (etwist > 0) return 1;

                    byte[] lhsUdSlice = this.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(lhsUdSlice);
                    byte[] rhsUdSlice = other.reference.getEdgePositions(s_udslice);
                    Array.Sort<byte>(rhsUdSlice);
                    int uds = RubikCube2.CompareArray(lhsUdSlice, rhsUdSlice);
                    if (uds < 0) return -1;
                    if (uds > 0) return 1;
                    return 0;
            }
        }
        public static bool ShouldPrune(RubikCube2 cube)
        {
            return false;
        }
        public static void GetNextStates(RubikCube2 currentCube, out RubikCube2[] nextCubes, out string[] twistNames)
        {
            nextCubes = new RubikCube2[0];
            twistNames = new string[0];
        }
        public static bool EqualFunc(RubikCube2 lhs, RubikCube2 rhs)
        {
            return false;
        }

        class Foo : IComparable<Foo>
        {
            public int f;
            public Foo(int f) { this.f = f; }
            public static bool operator < (Foo lhs, Foo rhs) { return (lhs.f/2) < (rhs.f/2); }
            public static bool operator >(Foo lhs, Foo rhs) { return (lhs.f / 2) > (rhs.f / 2); }

            public int CompareTo(Foo other)
            {
                if (this < other) return -1;
                if (this > other) return 1;
                return 0;
            }
            public static void Main()
            {
                SortedSet<Foo> fooSet = new SortedSet<Foo>();
                for (int i = 0; i < 10; ++i) fooSet.Add(new Foo(i));
                // prints 0 2 4 6 8
                foreach (Foo f in fooSet) System.Console.WriteLine("{0}", f.f);
                return;
            }
        }

        public static void Main()
        {
            /*
            RubikCube2 result = new RubikCube2();
            DateTime now = DateTime.Now;
            for (int i = 0; i < 1000000; ++i)
            {
                result = result * RubikCube2.U;
            }
            System.Console.WriteLine("Million permutations in {0} milliseconds", (DateTime.Now - now).TotalMilliseconds);
            System.Console.WriteLine(Profiler.showTimes());

            System.Console.WriteLine(Profiler.showCounters());
            return;
            */

            byte[] masks = new byte[] { 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80 };
            byte[] phase1 = new byte[495UL * 2187UL * 2048UL / 8];
            SortedDictionary<ulong, byte> cubeDepths = new SortedDictionary<ulong, byte>();
            cubeDepths[0] = 0;
            ulong count = 0;
            List<RubikCube2> currentDepthCubes = new List<RubikCube2>();
            for (int i = 0; i < RubikCube2.Moves_FTM.Length; ++i)
            {
                ulong tmp = Phase1Coordinate.FromCube(RubikCube2.Moves_FTM[i]).ID;
                if ((phase1[tmp / 8] & masks[tmp % 8]) == 0) ++count;
                phase1[tmp / 8] |= masks[tmp % 8];
                ulong[] ids = Phase1Coordinate.SymetriesFromCube(RubikCube2.Moves_FTM[i]);
                if (!cubeDepths.ContainsKey(ids[0]))
                {
                    cubeDepths[ids[0]] = 1;
                    currentDepthCubes.Add(RubikCube2.Moves_FTM[i]);
                }
            }
            List<RubikCube2> nextDepthCubes = new List<RubikCube2>();
            for (byte currentDepth = 2; currentDepth < 9; ++currentDepth)
            {
                int deadEnds = 0;
                while (currentDepthCubes.Count > 0)
                {
                    bool found = false;
                    RubikCube2 rcc = currentDepthCubes[0];
                    currentDepthCubes.RemoveAt(0);
                    //if (currentDepthCubes.Count > 0)
                    //{
                    //    RubikCube2 rcc2 = currentDepthCubes[0];
                    //    currentDepthCubes.RemoveAt(0);
                    //    System.Threading.ThreadPool.QueueUserWorkItem();
                    //}
                    RubikCube2[] moves = rcc.getFruitfulMoves();
                    for (int i = 0; i < moves.Length; ++i)
                    {
                        RubikCube2 rcn = rcc * moves[i];
                        Phase1Coordinate coord = Phase1Coordinate.FromCube(rcn);
                        ulong tmp = coord.ID;
                        if (tmp == 0) continue;
                        if ((phase1[tmp / 8] & masks[tmp % 8]) == 0)
                        {
                            ulong[] ids = Phase1Coordinate.SymetriesFromCube(rcn);
                            for (int j = 0; j < ids.Length; ++j)
                            {
                                if ((phase1[ids[j] / 8] & masks[ids[j] % 8]) == 0)
                                {
                                    ++count;
                                    phase1[ids[j] / 8] |= masks[ids[j] % 8];
                                }
                            }
                            cubeDepths[ids[0]] = currentDepth;
                            nextDepthCubes.Add(rcn);
                            found = true;
                        }
                    }
                    if (!found) { ++deadEnds; }
                }
                currentDepthCubes = nextDepthCubes;
                nextDepthCubes = new List<RubikCube2>();
                Console.WriteLine("Found {0} cubes out of {1} and {4} at depth {2} with {5} dead ends, {3}%  Press Y<enter> to continue",
                                  count,
                                  2187L * 2048L * 495L,
                                  currentDepth,
                                  Convert.ToSingle(count) / Convert.ToSingle(2187L * 2048L * 495L) * 100.0,
                                  currentDepthCubes.Count,
                                  deadEnds);
                //if (Console.ReadLine() == "Y") continue;
                //break;
            }
            System.Console.WriteLine(Profiler.showTimes());

            System.Console.WriteLine(Profiler.showCounters());
        }
    }

}
