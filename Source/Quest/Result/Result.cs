using Verse;

namespace Cities
{
	public abstract class Result : IExposable {
		public virtual string Label => null;

		public virtual void ExposeData() {
		}

		public virtual void OnResult(Quest quest) {
		}
	}
}