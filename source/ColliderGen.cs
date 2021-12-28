using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VoxelEngine
{
	public static class ColliderGen
	{
		public class CBox
		{
			public Index start;
			public Index size;
			public CBox(Index start, Index size){
				this.start = start;
				this.size = size;
			}
		}
		public static List<CBox> GenerateCollisionBoxes(Voxel[,,] voxels, Index dimensions)
		{

			List<CBox> boxes = new List<CBox>();
			bool[,,] tested = new bool[voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)];

			for (int x = 0; x < tested.GetLength(0); x++){
				for (int y = 0; y < tested.GetLength(1); y++){
					for (int z = 0; z < tested.GetLength(0); z++){
						if (!tested[x,y,z]){
							tested[x,y,z] = true;

							if (voxels[x,y,z].active)
							{
								Index boxStart = new Index(x, y, z);
								Index boxSize = new Index(1, 1, 1);
								bool canSpreadX = true;
								bool canSpreadY = true;
								bool canSpreadZ = true;

								while (canSpreadX || canSpreadY || canSpreadZ){
									canSpreadX = ColliderGen.TrySpreadX(canSpreadX, ref tested, boxStart, ref boxSize, voxels);
									canSpreadY = ColliderGen.TrySpreadY(canSpreadY, ref tested, boxStart, ref boxSize, voxels);
									canSpreadZ = ColliderGen.TrySpreadZ(canSpreadZ, ref tested, boxStart, ref boxSize, voxels);
								}
								boxes.Add(new CBox(boxStart, boxSize));
							}
						}
					}
				}
			}
			return boxes;
		}

		private static bool TrySpreadX(bool canSpreadX, ref bool[,,] tested, Index boxStart, ref Index boxSize, Voxel[,,] voxels){
			int yLimit = boxStart.y + boxSize.y;
			int zLimit = boxStart.z + boxSize.z;

			for (int y = boxStart.y; y < yLimit && canSpreadX; ++y)
			{
				for (int z = boxStart.z; z < zLimit; ++z){
					int newX = boxStart.x + boxSize.x;
					Index newIndex = new Index(newX, y, z);
					if (newX >= voxels.GetLength(0) ||
					 tested[newIndex.x, newIndex.y, newIndex.z] ||
					 !voxels[newIndex.x, newIndex.y, newIndex.z].active)
					 {
						 canSpreadX = false;
					 }
				}
			}

			if (canSpreadX){
				for (int y = boxStart.y; y < yLimit; ++y){
					for(int z = boxStart.z; z < zLimit; ++z){
						int newX = boxStart.x + boxSize.x;
						Index newIndex = new Index(newX, y, z);
						tested[newIndex.x, newIndex.y, newIndex.z] = true;
					}
				}
				++boxSize.x;
			}
			return canSpreadX;
		}

		private static bool TrySpreadY(bool canSpreadY, ref bool[,,] tested, Index boxStart, ref Index boxSize, Voxel[,,] voxels){
			int xLimit = boxStart.x + boxSize.x;
			int zLimit = boxStart.z + boxSize.z;

			for (int x = boxStart.x; x < xLimit && canSpreadY; ++x)
			{
				for (int z = boxStart.z; z < zLimit; ++z){
					int newY = boxStart.y + boxSize.y;
					Index newIndex = new Index(x, newY, z);
					if (newY >= voxels.GetLength(1) ||
					 tested[newIndex.x, newIndex.y, newIndex.z] ||
					 !voxels[newIndex.x, newIndex.y, newIndex.z].active)
					 {
						 canSpreadY = false;
					 }
				}
			}

			if (canSpreadY){
				for (int x = boxStart.x; x < xLimit; ++x){
					for(int z = boxStart.z; z < zLimit; ++z){
						int newY = boxStart.y + boxSize.y;
						Index newIndex = new Index(x, newY, z);
						tested[newIndex.x, newIndex.y, newIndex.z] = true;
					}
				}
				++boxSize.y;
			}
			return canSpreadY;
		}

		private static bool TrySpreadZ(bool canSpreadZ, ref bool[,,] tested, Index boxStart, ref Index boxSize, Voxel[,,] voxels){
			int xLimit = boxStart.x + boxSize.x;
			int yLimit = boxStart.y + boxSize.y;

			for (int x = boxStart.x; x < xLimit && canSpreadZ; ++x)
			{
				for (int y = boxStart.y; y < yLimit; ++y){
					int newZ = boxStart.z + boxSize.z;
					Index newIndex = new Index(x, y, newZ);
					if (newZ >= voxels.GetLength(2) ||
					 tested[newIndex.x, newIndex.y, newIndex.z] ||
					 !voxels[newIndex.x, newIndex.y, newIndex.z].active)
					 {
						 canSpreadZ = false;
					 }
				}
			}

			if (canSpreadZ){
				for (int x = boxStart.x; x < xLimit; ++x){
					for(int y = boxStart.y; y < yLimit; ++y){
						int newZ = boxStart.z + boxSize.z;
						Index newIndex = new Index(x, y, newZ);
						tested[newIndex.x, newIndex.y, newIndex.z] = true;
					}
				}
				++boxSize.z;
			}
			return canSpreadZ;
		}
	}
}
