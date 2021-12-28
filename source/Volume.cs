using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VoxelEngine
{
	public class Volume : Spatial
	{
		private IVoxelDataManager voxelDataManager;
		private Chunk[,,] chunks;
		private Area areaNode;
		public Vector3 voxelScale = Vector3.One;
		public int volumeXWidth = 16;
		public int volumeYHeight = 16;
		public int volumeZDepth = 16;
		public int chunkXWidth = 8;
		public int chunkYHeight = 8;
		public int chunkZDepth = 8;
		public enum MeshGenerationMethod{
			SingleThreaded = 0,
			MultiThreadedSafe = 1,
			MultiThreadedUnsafe = 2
		}

		public enum ColliderGenerationMethod {
			SingleThreaded = 0,
			MultiThreaded = 1
		}

		public bool fillVoxelState = true;
		public bool generateCollider = true;

		public MeshGenerationMethod meshGenerationMethod = MeshGenerationMethod.SingleThreaded;
		public ColliderGenerationMethod colliderGenerationMethod = ColliderGenerationMethod.SingleThreaded;
		public SpatialMaterial material = ResourceLoader.Load<SpatialMaterial>("res://VoxelEngine/material/dev_voxel_material.tres");
		private List<Index> dirtyChunkRegister = new List<Index>();

		public enum MeshingAlgorithm{
			Culled = 0
		}

		public MeshingAlgorithm meshingAlgorithm = MeshingAlgorithm.Culled;

		public override void _Ready()
		{
			this.voxelDataManager = new BasicVoxelDataManager(this.volumeXWidth, this.volumeYHeight, this.volumeZDepth);
			this.CreateChunks();
			this.FillVoxels(new Color(1f, 1f, 1f, 1f), this.fillVoxelState);
			this.GenerateAreaNode();

		}
		public override void _Process(float delta)
		{
			if (this.dirtyChunkRegister.Count > 0)
			{
				foreach (Index i in this.dirtyChunkRegister) this.UpdateChunk(i);
				this.dirtyChunkRegister.Clear();
			}
		}

		private Index GetChunkIndexAtVoxelIndex(Index index){
			return new Index((int)index.x/this.chunkXWidth, (int)index.y/this.chunkYHeight,(int)index.z/this.chunkZDepth);
		}
		public bool SetVoxelAtVoxelIndex(Index index, Voxel voxel){
			if (voxel == null || this.voxelDataManager == null || this.chunks == null) return false;

			if (!this.voxelDataManager.SetVoxel(index, voxel)) return false;

			Index i = this.GetChunkIndexAtVoxelIndex(index);

			if (!this.dirtyChunkRegister.Exists(v => v.x == i.x && v.y == i.y && v.z == i.z)){
				this.dirtyChunkRegister.Add(i);
			}
			return true;
		}

		public Voxel GetVoxelAtVoxelPosition(Index index){
			if (this.voxelDataManager == null) return null;
			return this.voxelDataManager.GetVoxel(index);
		}

		public Index GetVoxelIndexAtWorldPosition(Vector3 position){
			var positionOffset = position - this.Transform.origin;
			return new Index((int)(positionOffset.x / this.voxelScale.x),
							 (int)(positionOffset.y / this.voxelScale.y),
							 (int)(positionOffset.z / this.voxelScale.z));
		}

		public Vector3 GetWorldPositionAtVoxelIndex(Index index){
			return this.Transform.origin + new Vector3(index.x * this.voxelScale.x,
														index.y * this.voxelScale.y,
														index.z * this.voxelScale.z);
		}

		public Voxel GetVoxelAtWorldPosition(Vector3 position){
			return this.GetVoxelAtVoxelPosition(this.GetVoxelIndexAtWorldPosition(position));
		}

		public bool SetVoxelAtWorldPosition(Vector3 position, Voxel voxel){
			return this.SetVoxelAtVoxelIndex(this.GetVoxelIndexAtWorldPosition(position), voxel);
		}

		private void FillVoxels(Color color, bool state){
			Random r = new Random();
			for (int x = 0; x < this.volumeXWidth; x++)
			{
				for (int y = 0; y < this.volumeYHeight; y++)
				{
					for (int z = 0; z < this.volumeZDepth; z++)
					{
						this.SetVoxelAtVoxelIndex(new Index(x,y,z), new Voxel(color, state));
					}
				}
			}
		}

		private void CreateChunks()
		{
			// DESTROY EXISTING CHUNKS
			if (this.chunks != null)
			{
				for (int x = 0; x < this.chunks.GetLength(0); x++)
				{
					for (int y = 0; y < this.chunks.GetLength(1); y++)
					{
						for (int z = 0; z < this.chunks.GetLength(0); z++)
						{
							Chunk c = this.chunks[x, y, z];
							if (c != null)
							{
								c.ChunkDestroy();
								Free();
							}
						}
					}
				}
			}

			// CREATE NEW CHUNKS
			this.chunks = new Chunk[(int)Mathf.CeilToInt((float)this.volumeXWidth / this.chunkXWidth),
						(int)Mathf.CeilToInt((float)this.volumeYHeight / this.chunkYHeight),
						(int)Mathf.CeilToInt((float)this.volumeZDepth / this.chunkZDepth)];
			for (int x = 0; x < this.chunks.GetLength(0); x++)
			{
				for (int y = 0; y < this.chunks.GetLength(1); y++)
				{
					for (int z = 0; z < this.chunks.GetLength(0); z++)
					{
						Index i = new Index(x, y, z);
						this.CreateChunkAt(i);
					}
				}
			}
		}
		private void UpdateChunk(Index index)
		{
			if (this.voxelDataManager == null || this.chunks == null) return;
			if (index.x < 0 || index.y < 0 || index.z < 0 || 
			index.x >= this.chunks.GetLength(0) || index.y >= this.chunks.GetLength(1)  || index.z >= this.chunks.GetLength(2)) return;
			var c = this.chunks[index.x, index.y, index.z];
			if (c == null) return;
			c.Generate(this.GetChunkVoxels(index), this.voxelScale, this.material, this.meshGenerationMethod, this.colliderGenerationMethod, this.generateCollider);
		}



		private void CreateChunkAt(Index index)
		{
			Chunk c = new Chunk();
			MeshInstance m = new MeshInstance();
			c.AddChild(m);
			m.Name = "MeshInstance";
			c.index = index;
			c.Name = "Chunk x: " + index.x + " y: " + index.y + " z: " + index.z;
			this.AddChild(c);
			c.Translation = this.IndexToWorldPosition(index);
			this.chunks[index.x, index.y, index.z] = c;
		}

		private Vector3 IndexToWorldPosition(Index index)
		{
			Vector3 worldPos = new Vector3();
			worldPos.x = index.x * this.chunkXWidth * this.voxelScale.x;
			worldPos.y = index.y * this.chunkYHeight * this.voxelScale.y;
			worldPos.z = index.z * this.chunkZDepth * this.voxelScale.z;
			return worldPos;
		}

		public void GenerateAreaNode(){
			// Collider for lookups
			Area a = new Area();
			this.AddChild(a);
			CollisionShape s = new CollisionShape();
			a.AddChild(s);
			BoxShape b = new BoxShape();
			Vector3 colliderSize = new Vector3(this.volumeXWidth, this.volumeYHeight, this.volumeZDepth) * (this.voxelScale/2f);
			b.Extents = colliderSize;
			//s.Translation += colliderSize;
			a.Translation += colliderSize;
			s.Shape = b;
			this.areaNode = a;
			a.Name = "VolumeAreaNode";
		}

		private Voxel[,,] GetChunkVoxels(Index index)
		{
			Index indexStart = new Index(index.x*this.chunkXWidth,
										 index.y*this.chunkYHeight,
										 index.z*this.chunkZDepth);
			Index indexEnd = new Index(indexStart.x+this.chunkXWidth-1,
									   indexStart.y+this.chunkYHeight-1,
									   indexStart.z+this.chunkZDepth-1);

			Voxel[,,] voxels = new Voxel[(indexEnd.x-indexStart.x)+1, (indexEnd.y-indexStart.y)+1, (indexEnd.z-indexStart.z)+1];

			for (int x = indexStart.x; x <= indexEnd.x; x++){
				for (int y = indexStart.y; y <= indexEnd.y; y++){
					for (int z = indexStart.z; z <= indexEnd.z; z++){
						voxels[x-indexStart.x,y-indexStart.y,z-indexStart.z] = this.GetVoxelAtVoxelPosition(new Index(x, y, z));
					}
				}  
			}
			return voxels;
		}
	}
}
