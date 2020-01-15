namespace Cities
{
	public abstract class Decorator {
		public float weight = 1;

		public abstract void Decorate(Stencil s);
	}
}