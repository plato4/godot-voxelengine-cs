using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace VoxelEngine
{

	public class MeshResults {
		public List<Color> colours = new List<Color>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Vector3[]> vertices = new List<Vector3[]>();
	}
	
	public class Chunk : Spatial
	{
		public Index index;

		private CancellationTokenSource meshTokenSource;
		private CancellationTokenSource colliderTokenSource;
		private List<CollisionShape> colliders = new List<CollisionShape>();

		public void ChunkDestroy()
		{
			if (meshTokenSource != null)
				meshTokenSource.Cancel();
			if (colliderTokenSource != null)
				colliderTokenSource.Cancel();
		}

		public void Generate(Voxel[,,] voxels, Vector3 voxelScale, Material material, Volume.MeshGenerationMethod meshGenerationMethod, Volume.ColliderGenerationMethod colliderGenerationMethod, bool generateCollider)
		{
			ArrayMesh mesh = new ArrayMesh();
			this.CancelExistingMeshingAndColliderJobToken();
			
			if (meshGenerationMethod == Volume.MeshGenerationMethod.SingleThreaded)
			{
				this.GenerateMesh(mesh, voxels, voxelScale, material);
			}
			else if (meshGenerationMethod == Volume.MeshGenerationMethod.MultiThreadedSafe)
			{
				this.GenerateMeshAsyncSafe(mesh, voxels, voxelScale, material);
			}
			else if (meshGenerationMethod == Volume.MeshGenerationMethod.MultiThreadedUnsafe)
			{
				this.GenerateMeshAsyncUnsafe(mesh, voxels, voxelScale, material);
			}

			if (generateCollider)
			{
				if (colliderGenerationMethod == Volume.ColliderGenerationMethod.MultiThreaded)
					this.GenerateCollidersAsync(voxels, voxelScale);
				else if (colliderGenerationMethod == Volume.ColliderGenerationMethod.SingleThreaded) 
					this.GenerateColliders(voxels, voxelScale);
			}
			else
			{
				this.DestroyExistingColliders();
			}

		}

		private void GenerateMesh(Mesh mesh, Voxel[,,] voxels, Vector3 voxelScale, Material material)
		{
			MeshResults mr = null;
			ArrayMesh m = new ArrayMesh();
			mr = Meshing.GenerateMesh(voxels, voxelScale);
			m = Meshing.CompileMesh(m, mr, material, true);
			this.ApplyMesh(m);
		}

		private void GenerateColliders(Voxel[,,] voxels, Vector3 voxelScale){
			var boxes = ColliderGen.GenerateCollisionBoxes(voxels, new Index(voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2)));
			this.UpdateCollider(boxes, voxelScale);
		}

		private async void GenerateCollidersAsync(Voxel[,,] voxels, Vector3 voxelScale)
		{
			CancellationTokenSource localTokenSource = this.colliderTokenSource = new CancellationTokenSource();
			CancellationToken token = this.colliderTokenSource.Token;
			List<ColliderGen.CBox> boxes;
			try
			{
				boxes = await Task.Run(() => ColliderGen.GenerateCollisionBoxes(voxels, new Index(voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2))), token);
			}
			catch (Exception OperationCanceledException)
			{
				boxes = null;
			}
			finally
			{
				localTokenSource.Dispose();
				if (localTokenSource == this.colliderTokenSource){
					this.colliderTokenSource = null;
				}
			}
			if (boxes != null)
				this.UpdateCollider(boxes, voxelScale);
		}

		private void CancelExistingMeshingAndColliderJobToken()
		{
			if (this.meshTokenSource != null && !this.meshTokenSource.IsCancellationRequested){
				try{
				this.meshTokenSource.Cancel();
				this.meshTokenSource = null;
				}
				catch{GD.Print("Failed to cancel meshToken");}

			}
			if (this.colliderTokenSource != null && !this.colliderTokenSource.IsCancellationRequested){
				try{
				this.colliderTokenSource.Cancel();
				this.colliderTokenSource = null;
				}
				catch{GD.Print("Failed to cancel colliderToken");}
			}
		}

		private async void GenerateMeshAsyncSafe(ArrayMesh mesh, Voxel[,,] voxels, Vector3 voxelScale, Material material)
		{
			CancellationTokenSource localTokenSource = this.meshTokenSource = new CancellationTokenSource();
			CancellationToken token = this.meshTokenSource.Token;
			ArrayMesh m = null;
			MeshResults mr = null;
			try
			{
				mr = await Task.Run(() => Meshing.GenerateMesh(voxels, voxelScale), token);
			}
			catch (Exception OperationCanceledException)
			{
				mr = null;
			}
			finally
			{
				localTokenSource.Dispose();
				if (localTokenSource == this.meshTokenSource){
					this.meshTokenSource = null;
				}
			}
			//For thread safety put task that involves surface tool onto main thread
			if (mr != null)
			{
				m = Meshing.CompileMesh(mesh, mr, material, true);
				this.ApplyMesh(m);
			}
		}

		// Unsafe due to code for surfacetool being run under a thread - doesn't seem to safe.
		// attempted to work around it by instantiating an arraymesh on main thread and passing in surfacetool.commit(arraymesh) which
		// prevents surfacetool from trying to instantiate a new arraymesh in the thread.
		private async void GenerateMeshAsyncUnsafe(ArrayMesh mesh, Voxel[,,] voxels, Vector3 voxelScale, Material material)
		{
			CancellationTokenSource localTokenSource = this.meshTokenSource = new CancellationTokenSource();
			CancellationToken token = this.meshTokenSource.Token;
			ArrayMesh m = new ArrayMesh();
			try
			{
				MeshResults mr = await Task.Run(() => Meshing.GenerateMesh(voxels, voxelScale), token);
				m = await Task.Run(() => Meshing.CompileMesh(mesh, mr, material, true), token);
			}
			catch (Exception OperationCanceledException)
			{
				m = null;
			}
			finally
			{
				localTokenSource.Dispose();
				if (localTokenSource == this.meshTokenSource){
					this.meshTokenSource = null;
				}
			}
			if (m != null)
				this.ApplyMesh(m);
		}

		public void ApplyMesh(ArrayMesh m)
		{
			if (m == null) return;
			((MeshInstance)GetNode("MeshInstance")).Mesh = m;
		}

		private void DestroyExistingColliders(){
			foreach (CollisionShape c in this.colliders){
				// TODO Store the StaticBody instead and destroy that and child collisionshapes
				var p = c.GetParent();
				c.Free();
				p.Free();
			}
			this.colliders.Clear();
		}

		private void UpdateCollider(List<ColliderGen.CBox> boxes, Vector3 voxelScale){
			this.DestroyExistingColliders();
			
			foreach (ColliderGen.CBox cbox in boxes)
			{
				StaticBody body = new StaticBody();
				this.AddChild(body);
				CollisionShape col = new CollisionShape();
				body.AddChild(col);
				BoxShape box = new BoxShape();
				box.Extents = new Vector3((cbox.size.x * voxelScale.x)/2,
										  (cbox.size.y * voxelScale.y)/2,
										  (cbox.size.z * voxelScale.z)/2);
				body.Translation = new Vector3((cbox.start.x * voxelScale.x)+box.Extents.x,
											   (cbox.start.y * voxelScale.y)+box.Extents.y,
											   (cbox.start.z * voxelScale.z)+box.Extents.z);			   
				col.Shape = box;
				this.colliders.Add(col);	
			}
		}
	}
}
