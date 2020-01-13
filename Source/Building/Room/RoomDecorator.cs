using Verse.AI.Group;

namespace Cities {

	public abstract class RoomDecorator : Decorator {
		public bool roofed = true;
		public float lightChance = 0.9F;
		public int maxArea = 100;
	}
}
