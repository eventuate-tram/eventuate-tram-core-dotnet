namespace IO.Eventuate.Tram.Local.Kafka.Consumer;

public class BackPressureManagerStateAndActions
{
	public readonly BackPressureActions Actions;
	public readonly BackPressureManagerState State;

	public BackPressureManagerStateAndActions(BackPressureActions actions, BackPressureManagerState state) {
		Actions = actions;
		State = state;
	}

	public BackPressureManagerStateAndActions(BackPressureManagerState state)
	{
		Actions = BackPressureActions.NONE;
		State = state;
	}
}