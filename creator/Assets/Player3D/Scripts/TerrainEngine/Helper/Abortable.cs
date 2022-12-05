using System;
using System.Threading;
using System.Collections.Generic;

namespace TerrainEngine
{
    //
    //  class Abortable
    //
    //  The Abortable class enables the following functionality:
    //
    //  1. Offers a single global point of control for aborting a distributed
    //     and/or multithreaded processing sequence (member Abort()).
    //  2. Communicates to abortable scopes whether to abort processing. (shouldAbort property)
    //  3. Keeps track of scopes that are still executing, allowing a coroutine to
    //     wait until all execution completes before taking further action. (count property)
    //  4. Offers the diagnostic ability to inspect in the debugger which scopes are
    //     still executing (pause the process and view static member s_instances in the
    //     watch window)
    //
    //  To use:
    //
    //  Create an instance of the Abortable class using operator new at the beginning
    //  of a abortable function or scope.
    //  

    public class Abortable
    {
        const long WAIT_TIMEOUT = 5000; // milliseconds

        //  Instance state
        private string name;
        private float  startTime = 0.0f;

        //  Global state
        private static long  s_abortTime = 0;
        private static long  s_resetTime = 0;
        private static bool  s_shouldAbort = false;
        private static int   s_instanceCount = 0;
        private static List<string> s_instances = new List<string>();

        //  Diagnostics
        static Trace.Config traceConfig = null; // new Trace.Config();

        public static int count
        {
            get { return s_instanceCount; }
        }

        public bool shouldAbort
        {
            get 
            {
                if (s_shouldAbort)
                {
                    return true;
                }
                else if (
                    s_abortTime !=0 && 
                    startTime < s_abortTime && 
                    s_resetTime > s_abortTime)
                {
                    return true;
                }
                return false;
            }
        }

        public static bool ShouldAbortRoutine()
        {
            //  Use this only in the special circumstance of a coroutine
            //  in which you cannot rely on the Abortable destructor
            return s_shouldAbort;
        }

        public static void Abort()
        {
            lock (s_instances)
            {
                s_abortTime = DateTime.Now.Ticks;
                s_shouldAbort = true;
            }
        }

        public static bool WaitForAbortables(long milliseconds = WAIT_TIMEOUT)
        {
            if (s_instanceCount == 0)
            {
                return false;
            }

            if ((DateTime.Now.Ticks - s_abortTime) > (milliseconds * TimeSpan.TicksPerMillisecond))
            {
                Reset();
                return false;
            }
            return true;
        }

        public static void Reset()
        {
            lock (s_instances)
            {
                s_instanceCount = 0;
                s_resetTime = DateTime.Now.Ticks;
                s_shouldAbort = false;
                s_instances.Clear();
            }
        }

        public Abortable(string name)
        {
            Trace.Log(traceConfig, "Abortable created: '{0}'", name);

            this.name = name;
            lock (s_instances)
            {
                s_instances.Add(name);
            }
            Interlocked.Increment(ref s_instanceCount);
            this.startTime = DateTime.Now.Ticks;
        }

        ~Abortable()
        {
            Trace.Log(traceConfig, "Abortable destroyed: '{0}'", this.name);

            lock (s_instances)
            {
                s_instances.Remove(this.name);
            }

            if (Interlocked.Decrement(ref s_instanceCount) == 0)
            {
                Reset();
            }
        }
    }
}