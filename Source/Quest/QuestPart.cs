namespace Cities
{
	public abstract class QuestPart {
		public virtual void OnChoose(Quest quest) {
		}
		public virtual bool IsValid(Quest quest) {
			return true;
		}
		public virtual void OnStart(Quest quest) {
		}
		public virtual void OnComplete(Quest quest) {
		}
		public virtual void OnCancel(Quest quest) {
		}
		public virtual void OnExpire(Quest quest) {
		}
		public virtual void OnMapGenerated(Quest quest) {
		}
		public virtual void OnMapRemoved(Quest quest) {
		}
	}
}