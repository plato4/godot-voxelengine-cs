namespace VoxelEngine
{
    public class BasicVoxelDataManager : IVoxelDataManager
    {
        private Voxel[] voxels;
        public int width, height, depth;

        public BasicVoxelDataManager(int width, int height, int depth){
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.voxels = new Voxel[this.width * this.height * this.depth];
        }
        public Voxel GetVoxel(Index index)
        {
			var flatIndex = VoxelTools.GetFlatIndexFromXYZ(this.width, this.height, index.x, index.y, index.z);
			if (flatIndex < 0 || flatIndex > this.voxels.Length-1) return null;
			return this.voxels[flatIndex];
        }
        public bool SetVoxel(Index index, Voxel voxel)
        {
			var flatIndex = VoxelTools.GetFlatIndexFromXYZ(this.width, this.height, index.x, index.y, index.z);
			if (flatIndex < 0 || flatIndex > this.voxels.Length-1) return false;
            this.voxels[flatIndex] = voxel;
            return true;
        }

        public bool ContainsIndex(Index index){
            var flatIndex = VoxelTools.GetFlatIndexFromXYZ(this.width, this.height, index.x, index.y, index.z);
			if (flatIndex < 0 || flatIndex > this.voxels.Length-1) return false;
            return true;
        }
    }
}