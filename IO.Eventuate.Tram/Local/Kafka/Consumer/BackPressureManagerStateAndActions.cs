namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerStateAndActions
{
	internal readonly BackPressureActions Actions;
	internal readonly IBackPressureManagerState State;

	public BackPressureManagerStateAndActions(BackPressureActions actions, IBackPressureManagerState state) {
		Actions = actions;
		State = state;
	}

	public BackPressureManagerStateAndActions(IBackPressureManagerState state)
	{
		Actions = BackPressureActions.None;
		State = state;
	}
}