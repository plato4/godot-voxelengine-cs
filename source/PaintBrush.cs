using Godot;
using System;

namespace VoxelEngine
{
	public class PaintBrush : Spatial
	{
		// Declare member variables here. Examples:
		// private int a = 2;
		// private string b = "text";
		public float radius = 5f;
		public Area tipArea;

		public DateTime nextrun;
		
		public enum BrushOperation{
			Add = 0,
			Sub = 1
		}

		public enum BrushShape{
			Circle = 0
		}

		public BrushOperation brushOperation = BrushOperation.Sub; 
		public BrushShape brushShape = BrushShape.Circle;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			this.tipArea = this.FindNode("TipArea") as Area;
		}

		public override void _Process(float delta){

			if (nextrun < DateTime.Now)
			{
				this.Paint();
				GD.Print("Painting");
				nextrun = DateTime.Now.AddSeconds(0.5);
			}
			
		}

		public void Paint(){
			if (brushShape == BrushShape.Circle){
				this.PaintCircle(this.Transform.origin, this.radius, this.brushOperation);
			}
		}

		public void PaintCircle(Vector3 worldPosition, float radius, BrushOperation brushOperation){
			var areasOverlapped = this.tipArea.GetOverlappingAreas();

			foreach (Area a in areasOverlapped){
				Volume v = this.FindAreasVolume(a);
				if (v == null) continue;
				for (int x = 0; x < v.volumeXWidth; x++){
					for (int y = 0; y < v.volumeYHeight; y++){
						for(int z = 0; z < v.volumeZDepth; z++){
							var distanceFromBrushCentre = worldPosition.DistanceTo(v.GetWorldPositionAtVoxelIndex(new Index(x, y, z)));

							if (distanceFromBrushCentre < radius){
								var vox = v.GetVoxelAtVoxelPosition(new Index(x,y,z));
								vox.active = false;
								v.SetVoxelAtVoxelIndex(new Index(x,y,z),vox);
							}
						}
					}
				}
			}
		}

		public Volume FindAreasVolume(Area area){
			return area.GetParent<Volume>();
		}
	}
}
