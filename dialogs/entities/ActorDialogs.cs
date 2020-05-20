using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorDialogs
{
    private string actor;

    // Lista de todos los dialogos de inicio del actor
    // private List<Dialog> listDialogsInit;
    private DictPriorityListItems<Dialog> dictPriorityListInitDialogs;

    // Lista de todos los dialogos de conversación del actor
    private List<Dialog> listAllDialogs;

    // Mapa de dialogos del actor, indexado por refDialog, donde para cada refDialog puede haber una lista de dialogos.
    private Dictionary<string, List<Dialog>> dictListDialogs;

    // Mapa de dialogos del actor, indexados por prioridad, donde para cada prioridad puede haber una lista de diálogos.
    // private Dictionary<int, List<Dialog>> dictListPriorityDialogs;
    // private SortedList<int, List<Dialog>> dictListPriorityDialogs;
    private DictPriorityListItems<Dialog> dictPriorityListDialogs;


    private Dictionary<string, dynamic> dictRefConditions;

    public ActorDialogs(string actor)
    {
        this.actor = actor;

        // listDialogsInit = new List<Dialog>();
        listAllDialogs = new List<Dialog>();
        dictListDialogs = new Dictionary<string, List<Dialog>>();
        // dictListPriorityDialogs = new SortedList<int, List<Dialog>>();

        dictPriorityListInitDialogs = new DictPriorityListItems<Dialog>();
        dictPriorityListDialogs = new DictPriorityListItems<Dialog>();

        dictRefConditions = new Dictionary<string, dynamic>();
    }

    public string GetActor()
    {
        return actor;
    }

    public int GetNumDialogs()
    {
        return listAllDialogs.Count;
    }

    public void AddConditions(Condition condition)
    {
        dynamic buf;
        if (dictRefConditions.TryGetValue(condition.GetRef(), out buf)) dictRefConditions.Remove(condition.GetRef());
        dictRefConditions.Add(condition.GetRef(), condition);
    }

    public void AddDialog(Dialog dialog)
    {
        if (dialog.IsInit())
        {
            dictPriorityListInitDialogs.AddPriorityItem(dialog.GetPriority(), dialog);
        }
        else
        {
            // Se guarda en la lista genérica de diálogos
            listAllDialogs.Add(dialog);

            // Se guarda en el mapa de dialogos por 'refDialog'
            string refDialog = dialog.GetRef();
            if (refDialog != null && refDialog.Trim().Length > 0)
            {
                List<Dialog> listDialogDict = GetDialogsDict(refDialog, true);
                listDialogDict.Add(dialog);
            }

            // Se guarda en el mapa de dialogos por 'priority'
            dictPriorityListDialogs.AddPriorityItem(dialog.GetPriority(), dialog);
        }
    }

    /*
    public void AddDialog(Dialog dialog)
    {
        if (dialog.IsInit())
        {
            dictPriorityListInitDialogs.AddPriorityItem(dialog.GetPriority(), dialog);
            // listDialogsInit.Add(dialog);
        }
        else
        {
            // Se guarda en la lista genérica de diálogos
            listAllDialogs.Add(dialog);

            // Se guarda en el mapa de dialogos por 'refDialog'
            string refDialog = dialog.GetRef();
            if (refDialog != null && refDialog.Trim().Length > 0)
            {
                List<Dialog> listDialogDict = GetDialogsDict(refDialog, true);
                listDialogDict.Add(dialog);
            }

            // Se guarda en el mapa de dialogos por 'priority'
            int priority = dialog.GetPriority();
            List<Dialog> listDialogPriority = GetDialogsPriority(priority, true);
            listDialogPriority.Add(dialog);
        }
    }
    */

    public Condition GetCondition(string refCondition)
    {
        dynamic condition;
        dictRefConditions.TryGetValue(refCondition, out condition);
        return condition;
    }

    public List<Dialog> GetDialogs()
    {
        return listAllDialogs;
    }

    public List<Dialog> GetDialogs(string refDialog)
    {
        List<Dialog> listDialogDict = GetDialogsDict(refDialog, false);
        return listDialogDict;
    }

    public DialogResponse DialogRequest(ActorMemoryDialogs actorMemory, string text)
    {
        DialogResponse dialogResponse = null;

        Debug.Log("DialogInit.0. text = '" + text + "' / isInit = " + actorMemory.IsInit());

        if (actorMemory.IsInit())
        {
            dialogResponse = DialogRequest(dictPriorityListInitDialogs, actorMemory, text);
            actorMemory.SetIsInit(false);
        } else
        {
            dialogResponse = DialogRequest(dictPriorityListDialogs, actorMemory, text);
        }
        return dialogResponse;

    }

    /*
    public DialogResponse DialogRequest2(ActorMemoryDialogs actorMemory, string text)
    {
        DialogResponse dialogResponse = null;
        DialogRequest dialogRequest = new DialogRequest(text);

        Debug.Log("DialogInit.0. text = '" + text + "' / isInit = "+ actorMemory.IsInit());

        if (actorMemory.IsInit())
        {
            dialogResponse = DialogRequest(dictPriorityListInitDialogs, actorMemory, text);
            actorMemory.SetIsInit(false);
            return dialogResponse;
        }

        dialogResponse = new DialogResponse();

        foreach (int priority in dictListPriorityDialogs.Keys)
        {
            Debug.Log("DialogInit.4. priority = " + priority);

            List<Dialog> listDialogs = GetDialogsPriority(priority, false);
            foreach (Dialog dialog in listDialogs)
            {
                dialog.Eval(dialogRequest, dialogResponse);
                if (dialogResponse.IsValid()) return dialogResponse;
            }
        }
        return null;
    }
    */

    public DialogResponse DialogRequest(DictPriorityListItems<Dialog> dictPriorityListItems, ActorMemoryDialogs actorMemory, string text)
    {
        DialogResponse dialogResponse;
        DialogRequest dialogRequest = new DialogRequest(text);

        Debug.Log("DialogInit.0. text = '" + text + "' / isInit = " + actorMemory.IsInit());

        dialogResponse = new DialogResponse();

        foreach (int priority in dictPriorityListItems.GetListPriorities())
        {
            Debug.Log("DialogInit.4. priority = " + priority);

            List<Dialog> listDialogs = dictPriorityListItems.GetPriorityList(priority);
            foreach (Dialog dialog in listDialogs)
            {
                dialog.Eval(dialogRequest, dialogResponse);
                if (dialogResponse.IsValid()) return dialogResponse;
            }
        }
        return null;
    }

    /*
    public DialogResponse DialogRequestInit(DialogRequest dialogRequest)
    {
        DialogResponse dialogResponse = new DialogResponse();

        Debug.Log("DialogInit.1. listDialogsInit.Count = " + listDialogsInit.Count);
        foreach (Dialog dialog in listDialogsInit)
        {
            Debug.Log("DialogInit.2. dialog = " + dialog + " / dialogName = "+ dialog.GetRef() + " / isInit = "+ dialog.IsInit());
            // dialog.SetMatchText(text);
            dialog.Eval(dialogRequest, dialogResponse);
            Debug.Log("DialogInit.3. dialogResponse = " + dialogResponse + " / isValid = " + dialogResponse.IsValid());
            if (dialogResponse.IsValid()) return dialogResponse;
        }
        return null;
    }
    */

    private List<Dialog> GetDialogsDict(string refDialog, bool create)
    {
        List<Dialog> listDialogDict;
        if (!dictListDialogs.TryGetValue(refDialog, out listDialogDict) && create)
        {
            listDialogDict = new List<Dialog>();
            dictListDialogs.Add(refDialog, listDialogDict);
        }
        return listDialogDict;
    }

    /*
    private List<Dialog> GetDialogsPriority(int priority, bool create)
    {
        List<Dialog> listDialogPriority;
        if (!dictListPriorityDialogs.TryGetValue(priority, out listDialogPriority) && create)
        {
            listDialogPriority = new List<Dialog>();
            dictListPriorityDialogs.Add(priority, listDialogPriority);
        }
        return listDialogPriority;
    }
    */

    public Dialog GetDialogInPath(Dialog dialogFrom, string path)
    {
        Dialog dialogRet = null;

        // Si la ruta a buscar no contiene barras ni dos puntos...
        if (!path.Contains("/") && !path.Contains(".."))
        {
            string refDialog = path;    // Se asume que el refDialog es el path.

            // Si el 'dialogFrom' tiene algún hermano con el 'refDialog' indicado, se devuelve ese hermano...
            List<Dialog> listBrotherDialogs = dialogFrom.GetListBrotherDialogs();
            foreach (Dialog brotherDialog in listBrotherDialogs)
            {
                if (brotherDialog.GetRef() != null && brotherDialog.GetRef() == refDialog) return brotherDialog;
            }

            // Si no existe el hermano indicado, se busca el 'refDialog' en la raíz.
            List<Dialog> listRootDialogs;
            dictListDialogs.TryGetValue(refDialog, out listRootDialogs);
            if (listRootDialogs != null)
            {
                int i = 0;
                foreach (Dialog rootDialog in listRootDialogs)
                {
                    if (rootDialog != dialogFrom) return listAllDialogs[i++];   // Se devuelve el primer diálogo que coincide con el 'refDialog', siempre que no sea el 'dialogFrom'
                }
            }
        }
        return dialogRet;
    }
}
