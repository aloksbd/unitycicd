using System;
using UnityEngine;

namespace TerrainEngine
{
    //-------------------------------------------------------------------------------
    //  Diagnostic trace configuration for the Terrain Engine
    //
    //  To modify trace behavior, edit the TRACE_FLAGS constant and/or the 
    //  the inline Trace.Config constructor below.

    public class TerrainTrace : MonoBehaviour
    {
        public enum Flag
        {
            State = 0x00000001,
            HeightMaps = 0x00000002,
            Imagery = 0x00000004,
            RunCoroutine = 0x00000008,
            WebRequests = 0x00000010,
            WorldGen = 0x00000020,
        }

        private const UInt32 TRACE_FLAGS = (UInt32)(Flag.State | Flag.WorldGen);

        private static Trace.Config s_traceConfig = new Trace.Config()
        {
            enabled = true,
            logFileNameNoExtension = "TerrainTrace",
            includeTimeStamp = false
        };

        public static Trace.Config Config(Flag flag)
        {
            return (TRACE_FLAGS & (UInt32)flag) != 0 ?
                s_traceConfig : null;
        }
    }
}