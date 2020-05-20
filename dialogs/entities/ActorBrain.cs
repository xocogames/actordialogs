using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorBrain
{
    private string actor;

    private ContextActors context;

    private ActorDialogs actorDialogs;

    private ActorMemoryDialogs actorMemory;

    public ActorBrain(ContextActors context, string actor)
    {
        this.actor = actor;
        this.context = context;

        actorDialogs = new ActorDialogs(actor);
        actorMemory = new ActorMemoryDialogs(actor);
    }

    public string GetActor()
    {
        return actor;
    }

    public ContextActors GetContext()
    {
        return context;
    }

    public ActorDialogs GetDialogs()
    {
        return actorDialogs;
    }

    public ActorMemoryDialogs GetMemory()
    {
        return actorMemory;
    }

    public void AddDialog(Dialog dialog)
    {
        actorDialogs.AddDialog(dialog);
    }

    public string GetVarValue(string name)
    {
        return GetVarValue(name, null);
    }

    public string GetFuncValue(string name)
    {
        Debug.Log("GAMETIME.GetFuncValue. name = "+ name);
        switch (name)
        {
            case "TimeHour":
                return context.GetTimeGameHour().ToString();

            case "TimeIsDay":
                return context.IsTimeDay().ToString();
            case "TimeIsNight":
                return context.IsTimeNight().ToString();
            case "TimeIsDawn":
                return context.IsTimeDawn().ToString();
            case "TimeIsTwilight":
                return context.IsTimeTwilight().ToString();

            case "TimeGameDays":
                return context.GetTimeGameDays().ToString();

            case "TimeDate":
                return context.GetTimeDate();
        }
        return null;
    }

    public string GetVarValue(string name, string def)
    {
        return actorMemory.GetVarValue(name, def);
    }

    public void ClearActorDialogs()
    {
        actorDialogs = new ActorDialogs(actor);
    }
}
