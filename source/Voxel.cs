using Godot;
using System;

namespace VoxelEngine
{
    public class Voxel
    {
        public bool active;
        public Color color;

        public Voxel(Color color, bool active)
        {
            this.color = color;
            this.active = active;
        }
    }
}