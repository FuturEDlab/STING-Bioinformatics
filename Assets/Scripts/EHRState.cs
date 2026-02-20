using UnityEngine;

public abstract class EHRState
{
    protected readonly EHRContext ctx;

    protected EHRState(EHRContext context)
    {
        ctx = context;
    }

    public abstract EHRStateId Id { get; }

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }
}
