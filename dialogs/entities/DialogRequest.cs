using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogRequest
{ 
    private string text;

    public DialogRequest(string text)
    {
        this.text = text;
    }

    public string GetText()
    {
        return text;
    }
}
