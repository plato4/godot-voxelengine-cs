namespace VoxelEngine
{
    public interface IVoxelDataManager
    {
        Voxel GetVoxel(Index index);
        bool SetVoxel(Index index, Voxel voxel);
        bool ContainsIndex(Index index);
    }
}