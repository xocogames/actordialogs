using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class DialogsManager
{
    private ContextActors context;

    private Dictionary<string, ActorBrain> dictActorBrain;

    private DialogsLoader dialogsLoader;

    static private Regex regexValidInput = new Regex("[^a-zA-Z0-9_\\s]");

    public DialogsManager()
    {
        context = new ContextActors();
        // dictActorDialogs = new Dictionary<string, ActorDialogs>();
        dictActorBrain = new Dictionary<string, ActorBrain>();
    }

    public void SetDialogsLoader(DialogsLoader dialogsLoader)
    {
        this.dialogsLoader = dialogsLoader;
    }

    public int LoadActorDialogs(string actor, string path)
    {
        return dialogsLoader.LoadActorDialogs(actor, path);
    }

    public ContextActors GetContext()
    {
        return context;
    }

    public void SetContext(ContextActors context)
    {
        this.context = context;
    }

    public void CreateActor(string actor)
    {
        dictActorBrain.Add(actor, new ActorBrain(context, actor));
    }

    public void AddActorDialog(string actor, Dialog dialog)
    {
        ActorDialogs actorDialogs = GetActorDialogs(actor);
        actorDialogs.AddDialog(dialog);
    }

    public DialogResponse DialogRequest(string actor, string text)
    {
        DialogResponse dialogResponse = null;

        ActorDialogs actorDialogs = GetActorDialogs(actor);
        ActorMemoryDialogs actorMemory = GetActorMemory(actor);
        if (actorDialogs != null && actorMemory != null)
        {
            text = NormalizeText(text);
            Debug.Log("NormalizeText: "+ text);
            dialogResponse = actorDialogs.DialogRequest(actorMemory, text);
            actorMemory.AddDialogResponse(dialogResponse);
        }
        return dialogResponse;
    }

    public ActorBrain GetActorBrain(string actor)
    {
        ActorBrain actorBrain;

        dictActorBrain.TryGetValue(actor, out actorBrain);
        return actorBrain;
    }

    public ActorDialogs GetActorDialogs(string actor)
    {
        ActorBrain actorBrain;

        dictActorBrain.TryGetValue(actor, out actorBrain);
        if (actorBrain != null) return actorBrain.GetDialogs();

        return null;
    }

    public ActorMemoryDialogs GetActorMemory(string actor)
    {
        ActorBrain actorBrain;

        dictActorBrain.TryGetValue(actor, out actorBrain);
        if (actorBrain != null) return actorBrain.GetMemory();

        return null;
    }

    public void ClearActorMemory(string actor)
    {
        ActorMemoryDialogs actorMemory = GetActorMemory(actor);
        if (actorMemory != null)
        {
            actorMemory.ClearAll();
        }
    }

    public void ClearActorDialogs(string actor)
    {
        ActorBrain actorBrain = GetActorBrain(actor);
        if (actorBrain != null)
        {
            actorBrain.ClearActorDialogs();
        }
    }

    public void ReloadActorDialogs(string actor)
    {
        ActorMemoryDialogs actorMemory = GetActorMemory(actor);
        if (actorMemory != null)
        {
            actorMemory.ClearLastActorDialogResponse();

            ClearActorDialogs(actor);
            dialogsLoader.LoadActorDialogs(actor, "");
        }
    }

    static public string NormalizeText(string text)
    {
        string textNormalized = text;

        char c0 = '\0';
        if (text.Length > 0 && text[0] == ':')
        {
            c0 = text[0];
            textNormalized = textNormalized.Substring(1);
        }
        textNormalized = textNormalized.Normalize(NormalizationForm.FormD);
        textNormalized = regexValidInput.Replace(textNormalized, "").ToLower();

        if (c0 != '\0')
        {
            textNormalized = c0 + textNormalized;
        }

        return textNormalized;
    }
}
