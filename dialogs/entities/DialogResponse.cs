using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogResponse
{
    private Dialog dialog;

    private string refDialogResponse;

    private string text;

    private List<string> listActions;

    public DialogResponse()
    {
        refDialogResponse = null;
        listActions = new List<string>();
    }

    public void SetDialog(Dialog dialog)
    {
        this.dialog = dialog;
        refDialogResponse = dialog.GetRefResolved();
    }

    public Dialog GetDialog()
    {
        return dialog;
    }

    /*
    public void SetRefDialog(string refDialog)
    {
        this.refDialogResponse = refDialog;
    }
    */

    public string GetRefDialog()
    {
        return refDialogResponse;
    }

    public void SetText(string text)
    {
        if (text != null)
        {
            this.text = text.Trim();
        }
    }

    public string GetText()
    {
        return text;
    }

    public void SetActions(List<string> listActions)
    {
        this.listActions = new List<string>(listActions);
    }

    public List<string> GetListActions()
    {
        return listActions;
    }

    public string GetAction(int idx)
    {
        if (idx < 0 || idx >= listActions.Count) return null;
        return listActions[idx];
    }

    public bool IsValid()
    {
        if (text != null || listActions.Count > 0) return true;
        return false;
    }
}
