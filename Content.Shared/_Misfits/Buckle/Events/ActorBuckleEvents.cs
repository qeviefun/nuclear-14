// #Misfits Add: carry performer context across buckle state changes so server-side flavor systems can react.
using Content.Shared.Buckle.Components;

namespace Content.Shared._Misfits.Buckle.Events;

public sealed class ActorStrappedEvent : EntityEventArgs
{
	public Entity<StrapComponent> Strap { get; }
	public Entity<BuckleComponent> Buckle { get; }
	public EntityUid? User { get; }

	public ActorStrappedEvent(Entity<StrapComponent> strap, Entity<BuckleComponent> buckle, EntityUid? user)
	{
		Strap = strap;
		Buckle = buckle;
		User = user;
	}
}

public sealed class ActorUnstrappedEvent : EntityEventArgs
{
	public Entity<StrapComponent> Strap { get; }
	public Entity<BuckleComponent> Buckle { get; }
	public EntityUid? User { get; }

	public ActorUnstrappedEvent(Entity<StrapComponent> strap, Entity<BuckleComponent> buckle, EntityUid? user)
	{
		Strap = strap;
		Buckle = buckle;
		User = user;
	}
}