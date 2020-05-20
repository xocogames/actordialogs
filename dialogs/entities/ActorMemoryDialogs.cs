using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorMemoryDialogs
{
    private string actor;

    private List<DialogResponse> listHistResponses;
    private Dictionary<string, DialogResponse> dictRefDialogResponses;

    private Dictionary<string, string> dictDefinitionVars = new Dictionary<string, string>();
    private Dictionary<string, string> dictPermanentVars = new Dictionary<string, string>();

    private HashSet<Dialog> hashDialogsUsed;

    // Uso de plantillas con randoms y saturación de diálogos 
    private HashSet<Dialog> hashDialogSaturation;
    private Dictionary<TemplateElem, List<int>> dictTemplateUsedRandoms;

    private bool isInit;

    public ActorMemoryDialogs(string actor)
    {
        this.actor = actor;
        // this.context = context;

        listHistResponses = new List<DialogResponse>();
        dictRefDialogResponses = new Dictionary<string, DialogResponse>();

        hashDialogsUsed = new HashSet<Dialog>();

        hashDialogSaturation = new HashSet<Dialog>();
        dictTemplateUsedRandoms = new Dictionary<TemplateElem, List<int>>();

        isInit = true;
    }

    public void AddDialogSatured(Dialog dialog)
    {
        hashDialogSaturation.Add(dialog);
    }

    public bool IsDialogSatured(Dialog dialog)
    {
        return hashDialogSaturation.Contains(dialog);
    }

    // Devuelve un elemento random de la plantilla de respuesta, pero lo añade a una lista para no devolverlo más. 
    // Si el parámetro "sature" es "true" y ya se han devuelto todos los posibles randoms, se guarda el diálogo correspondiente a la platntilla dentro de los diálogos saturados.
    public string GetTemplateRandom(TemplateElem templateElem, bool sature)
    {
        List<int> listIdxUsedRandoms;

        int idxRand = -1;
        bool isSaturate = false;

        int countRandoms = templateElem.CountRandom();
        Debug.Log("ActorMemory.GetTemplateRandom().0. templateElem = " + templateElem.GetDialog() + " / refDialog = " + templateElem.GetDialog().GetRef() + " / sature = "+ sature + " / countRandoms = "+ countRandoms);

        // Comprueba si hay lista de randoms usados
        if (dictTemplateUsedRandoms.TryGetValue(templateElem, out listIdxUsedRandoms))
        {
            // Sí la hay..

            Debug.Log("ActorMemory.GetTemplateRandom().1. listIdxUsedRandoms = " + listIdxUsedRandoms + " / listCount = " + listIdxUsedRandoms.Count + " / countRandoms = "+ countRandoms);

            if (listIdxUsedRandoms.Count == countRandoms)   // Si ya se alcanzó el uso de toda la lista de randoms...
            {
                idxRand = templateElem.GetRandomTextIdx();  // ...devuelve uno al azar.
                isSaturate = true;
                Debug.Log("ActorMemory.GetTemplateRandom().2. idxRand = " + idxRand + " / isSaturate = " + isSaturate);
            }
            else // Si todavía no se han usado todos los randoms de la lista...
            {
                Debug.Log("ActorMemory.GetTemplateRandom().3");
                for (int i = 0; i < countRandoms * 2; i++)  // Se recorre un máximo de dos veces la lista buscando un random no usuado.
                {
                    idxRand = templateElem.GetRandomTextIdx();
                    Debug.Log("ActorMemory.GetTemplateRandom().4. i = " + i + " / idxRand = " + idxRand);

                    if (!listIdxUsedRandoms.Contains(idxRand))
                    {
                        listIdxUsedRandoms.Add(idxRand);    // ...si lo encuentra lo añade a la lista.
                        Debug.Log("ActorMemory.GetTemplateRandom().5. i = " + i + " / idxRand = " + idxRand + " / listIdxUsedRandoms.Count = "+ listIdxUsedRandoms.Count);
                        break;
                    } else
                    {
                        idxRand = -1;
                        Debug.Log("ActorMemory.GetTemplateRandom().6. idxRand = " + idxRand);
                    }
                }

                Debug.Log("ActorMemory.GetTemplateRandom().7. idxRand = " + idxRand);
                if (idxRand < 0) // Si tras generar muchos randoms todavía no se han usado todos los de la lista...
                {
                    for (idxRand = 0; idxRand < countRandoms; idxRand++) // ...se recorre secuancialmente la lista para encontrar uno no usado.
                    {
                        Debug.Log("ActorMemory.GetTemplateRandom().8. idxRand = " + idxRand + " / countRandoms = "+ countRandoms);
                        if (!listIdxUsedRandoms.Contains(idxRand))
                        {
                            listIdxUsedRandoms.Add(idxRand);    // ...y cuando lo encuentra lo añade a la lista.
                            Debug.Log("ActorMemory.GetTemplateRandom().9. idxRand = " + idxRand + " / listIdxUsedRandoms.count = " + listIdxUsedRandoms.Count);
                            break;
                        }
                    }
                }
            }
        } 
        else // Si no hay lista de randoms usados, es que es el primero. Se crea la lista y se añade el random usado.
        {
            idxRand = templateElem.GetRandomTextIdx();
            listIdxUsedRandoms = new List<int>();
            dictTemplateUsedRandoms.Add(templateElem, listIdxUsedRandoms);
            listIdxUsedRandoms.Add(idxRand);

            Debug.Log("ActorMemory.GetTemplateRandom().10. idxRand = " + idxRand + " / listIdxUsedRandoms.count = " + listIdxUsedRandoms.Count);
        }

        // Si se establece el parámetro de saturación del diálogo, y se ha obtenido ya todos los randoms de la lista...
        if (sature && !isSaturate && listIdxUsedRandoms.Count == countRandoms)
        {
            Debug.Log("ActorMemory.GetTemplateRandom().11. sature = "+ templateElem.GetDialog() + " / refDialog = "+ templateElem.GetDialog().GetRef());
            AddDialogSatured(templateElem.GetDialog()); // ... entonces se indica que el diálogo está saturado.
        }

        // Se devuelve el texto correspondiente al random
        string text = templateElem.GetTextRandom(idxRand);

        Debug.Log("ActorMemory.GetTemplateRandom().12. idxRand = " + idxRand + " / text = " + text);
        return text;
    }

    public string GetActor()
    {
        return actor;
    }

    /*
    public ContextActors GetContext()
    {
        return context;
    }
    */

    public void AddDialogResponse(DialogResponse dialogResponse)
    {
        if (dialogResponse == null) return;

        if (isInit) isInit = false;

        listHistResponses.Add(dialogResponse);
        if (!hashDialogsUsed.Contains(dialogResponse.GetDialog())) hashDialogsUsed.Add(dialogResponse.GetDialog());

        string refDialog = dialogResponse.GetRefDialog();
        if (refDialog != null && refDialog.Length > 0)
        {
            dictRefDialogResponses.Remove(refDialog);
            dictRefDialogResponses.Add(refDialog, dialogResponse);
        }
    }

    public bool IsUsedDialog(Dialog dialog)
    {
        return hashDialogsUsed.Contains(dialog);
    }

    public void ClearLastActorDialogResponse()
    {
        if (listHistResponses.Count > 0) listHistResponses.RemoveAt(listHistResponses.Count - 1);
    }

    public bool ExistsDialogResponse(string refDialog)
    {
        return dictRefDialogResponses.ContainsKey(refDialog);
    }

    public string GetPrevRefDialogResponse(int idx)
    {
        Debug.Log("ProcessConditionRefPrev.GetPrevRefDialogResponse. idx = " + idx + " / listHistResponses.Count = "+ listHistResponses.Count);
        if (idx > 0 && idx <= listHistResponses.Count)
        {
            DialogResponse dialogResponse = listHistResponses[listHistResponses.Count - idx];
            Debug.Log("ProcessConditionRefPrev.GetPrevRefDialogResponse. listHistResponses[" + (listHistResponses.Count - idx) + "] / dialogResponse = " + dialogResponse);
            if (dialogResponse != null)
            {
                Debug.Log("ProcessConditionRefPrev.GetPrevRefDialogResponse. dialogResponse.GetRefDialog() = " + dialogResponse.GetRefDialog());
                return dialogResponse.GetRefDialog();
            }
        }
        return null;
    }

    public bool ExistsPrevRefDialogResponse(string refDialog, int idx)
    {
        for (int i = 0; i < listHistResponses.Count; i++)
        {
            DialogResponse dialogResponse = listHistResponses[i];
            Debug.Log("ProcessConditionRefPrev.ExistsPrevRefDialogResponse. DUMP. listHistResponses[" + i + "] / dialogResponse = " + dialogResponse + " / refDialog = "+ (dialogResponse != null ? dialogResponse.GetRefDialog() : ""));
        }

        Debug.Log("ProcessConditionRefPrev.ExistsPrevRefDialogResponse. refDialog = "+ refDialog);
        for (int i = idx; i > 0; i--)
        {
            Debug.Log("ProcessConditionRefPrev.ExistsPrevRefDialogResponse. i=" + i);
            string refDialogPrev = GetPrevRefDialogResponse(i);
            Debug.Log("ProcessConditionRefPrev.ExistsPrevRefDialogResponse. i=" + i + " / refDialogPrev="+ refDialogPrev);
            if (refDialogPrev == refDialog) return true;
        }
        return false;
    }

    public void SetDefinitionVar(string name, string val)
    {
        string valold;
        if (dictDefinitionVars.TryGetValue(name.ToLower(), out valold))
        {
            dictDefinitionVars.Remove(name.ToLower());
        }
        dictDefinitionVars.Add(name.ToLower(), val);
    }

    public string GetDefinitionVar(string name)
    {
        return GetDefinitionVar(name.ToLower(), null);
    }

    public string GetDefinitionVar(string name, string def)
    {
        string val;
        Debug.Log("DialogsContext.GetDefinitionVar. name: " + name.ToLower() + " / def: " + def);
        if (dictDefinitionVars.TryGetValue(name.ToLower(), out val))
        {
            Debug.Log("DialogsContext.GetDefinitionVar. name: " + name.ToLower() + " / val: " + val);
            return val;
        }
        return def;
    }

    public void SetPermanetVar(string name, string val)
    {
        string valold;
        if (dictPermanentVars.TryGetValue(name, out valold))
        {
            dictPermanentVars.Remove(name);
        }
        dictPermanentVars.Add(name, val);
    }

    public void IncPermanetVar(string name, string amount)
    {
        int val = 0;
        string valold;

        int valAmount = ToInt(amount, int.MinValue);
        if (valAmount == int.MinValue) return;

        if (dictPermanentVars.TryGetValue(name, out valold))
        {
            dictPermanentVars.Remove(name);

            val = ToInt(valold, 0);
        }

        val += valAmount;
        dictPermanentVars.Add(name, val.ToString());
    }

    public static int ToInt(string text, int def)
    {
        int val;
        if (int.TryParse(text, out val))
        {
            return val;
        }
        return def;
    }

    public string GetPermanentVar(string name)
    {
        return GetPermanentVar(name, null);
    }

    public string GetPermanentVar(string name, string def)
    {
        string val;
        if (dictPermanentVars.TryGetValue(name, out val)) return val;
        return def;
    }

    public bool ContainsPermanentVar(string name)
    {
        return dictPermanentVars.ContainsKey(name);
    }

    public string RemovePermanentVar(string name)
    {
        string val;
        if (dictPermanentVars.TryGetValue(name, out val))
        {
            dictPermanentVars.Remove(name);
        }
        return val;
    }

    public string GetVarValue(string name)
    {
        return GetVarValue(name, null);
    }

    public string GetVarValue(string name, string def)
    {
        string ret;
        ret = GetPermanentVar(name, null);
        if (ret == null) ret = GetDefinitionVar(name, null);

        if (ret == null) ret = def;
        return ret;
    }

    public bool IsInit()
    {
        return isInit;
    }

    public void SetIsInit(bool isInit)
    {
        this.isInit = isInit; ;
    }

    public void ClearAll()
    {
        isInit = true;

        dictPermanentVars.Clear();
        dictRefDialogResponses.Clear();

        hashDialogsUsed.Clear();
        hashDialogSaturation.Clear();
        dictTemplateUsedRandoms.Clear();
    }

}
