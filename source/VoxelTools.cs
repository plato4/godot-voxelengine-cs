using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VoxelEngine
{
    public static class VoxelTools
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatIndexFromXYZ(int xSize, int ySize, int x, int y, int z)
        {
            return GetFlatIndexFromXYZ(new Index(xSize, ySize, 0), new Index(x, y, z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatIndexFromXYZ(Index size, Index pos)
        {
            return pos.x + size.x * (pos.y + size.y * pos.z);
        }
    }
}
