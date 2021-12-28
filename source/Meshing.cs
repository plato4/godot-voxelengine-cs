using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace VoxelEngine
{
	public static class Meshing
	{
		public static readonly Vector3[] vertList = {
				new Vector3(0, 0, 0), new Vector3(1, 0, 0), 
				new Vector3(1, 0, 1), new Vector3(0, 0, 1), 
				new Vector3(0, 1, 0), new Vector3(1, 1, 0),
				new Vector3(1, 1, 1), new Vector3(0, 1, 1) 
			};


		// TODO does not handle threading well, need to pass in existing Mesh  into s.commit to prevent commit creating an instance which isnt multithreaded
		public static ArrayMesh CompileMesh(ArrayMesh mesh, MeshResults mr, Material mat=null, bool regenerateNormals = true){
			if (mr == null) return mesh;
			SurfaceTool s = new SurfaceTool();
			s.Begin(Mesh.PrimitiveType.Triangles);
			for (int i = 0; i < mr.colours.Count; i++)
			{
				s.AddColor(mr.colours[i]);
				s.AddNormal(mr.normals[i]);
				foreach(var v in mr.vertices[i]){
					s.AddVertex(v);
				}
			}
			if (regenerateNormals)
				s.GenerateNormals();
			s.Index();
			mesh = s.Commit(mesh);

			if(mat != null)
				mesh.SurfaceSetMaterial(0, mat);

			return mesh;
		}

		public static MeshResults GenerateMesh(Voxel[,,] voxels, Vector3 voxelScale)
		{
			MeshResults meshResults = new MeshResults();

			for (int x = 0; x < voxels.GetLength(0); x++)
			{
				for (int y = 0; y < voxels.GetLength(1); y++)
				{
					for (int z = 0; z < voxels.GetLength(2); z++)
					{
						if (!voxels[x,y,z].active) continue;
						bool left = false;
						if (x > 0) left = voxels[x-1,y,z].active;

						bool right = false;
						if (x < voxels.GetLength(0)-1) right = voxels[x+1,y,z].active;

						bool back = false;
						if (z > 0) back = voxels[x,y,z-1].active;

						bool front = false;
						if (z < voxels.GetLength(2)-1) front = voxels[x,y,z+1].active;

						bool top = false;
						if (y < voxels.GetLength(1)-1) top = voxels[x,y+1,z].active;

						bool bottom = false;
						if (y > 0) bottom = voxels[x,y-1,z].active;

						if (left && right && top && bottom && front && back) continue;

						Vector3 vOffset = new Vector3(x, y, z);
						if (!top){
							meshResults.normals.Add(new Vector3(0, 1, 0));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[4] + vOffset)*voxelScale;
							tempV[1] = (vertList[5] + vOffset)*voxelScale;
							tempV[2] = (vertList[7] + vOffset)*voxelScale;
							tempV[3] = (vertList[5] + vOffset)*voxelScale;
							tempV[4] = (vertList[6] + vOffset)*voxelScale;
							tempV[5] = (vertList[7] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
							
						}

						if (!right){
							meshResults.normals.Add(new Vector3(-1, 0, 0));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[2] + vOffset)*voxelScale;
							tempV[1] = (vertList[5] + vOffset)*voxelScale;
							tempV[2] = (vertList[1] + vOffset)*voxelScale;
							tempV[3] = (vertList[2] + vOffset)*voxelScale;
							tempV[4] = (vertList[6] + vOffset)*voxelScale;
							tempV[5] = (vertList[5] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
						}

						if (!left){
							meshResults.normals.Add(new Vector3(1, 0, 0));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[0] + vOffset)*voxelScale;
							tempV[1] = (vertList[7] + vOffset)*voxelScale;
							tempV[2] = (vertList[3] + vOffset)*voxelScale;
							tempV[3] = (vertList[0] + vOffset)*voxelScale;
							tempV[4] = (vertList[4] + vOffset)*voxelScale;
							tempV[5] = (vertList[7] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
						}

						if (!front){
							meshResults.normals.Add(new Vector3(0, 0, -1));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[3] + vOffset)*voxelScale;
							tempV[1] = (vertList[6] + vOffset)*voxelScale;
							tempV[2] = (vertList[2] + vOffset)*voxelScale;
							tempV[3] = (vertList[3] + vOffset)*voxelScale;
							tempV[4] = (vertList[7] + vOffset)*voxelScale;
							tempV[5] = (vertList[6] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
						}

						if (!back){
							meshResults.normals.Add(new Vector3(0, 0, 1));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[0] + vOffset)*voxelScale;
							tempV[1] = (vertList[1] + vOffset)*voxelScale;
							tempV[2] = (vertList[5] + vOffset)*voxelScale;
							tempV[3] = (vertList[5] + vOffset)*voxelScale;
							tempV[4] = (vertList[4] + vOffset)*voxelScale;
							tempV[5] = (vertList[0] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
						}

						if (!bottom){
							meshResults.normals.Add(new Vector3(0, -1, 0));
							meshResults.colours.Add(voxels[x,y,z].color);
							Vector3[] tempV = new Vector3[6];
							tempV[0] = (vertList[1] + vOffset)*voxelScale;
							tempV[1] = (vertList[3] + vOffset)*voxelScale;
							tempV[2] = (vertList[2] + vOffset)*voxelScale;
							tempV[3] = (vertList[1] + vOffset)*voxelScale;
							tempV[4] = (vertList[0] + vOffset)*voxelScale;
							tempV[5] = (vertList[3] + vOffset)*voxelScale;
							meshResults.vertices.Add(tempV);
						}
					}
				}
			}
			return meshResults;
		}
	}
}
