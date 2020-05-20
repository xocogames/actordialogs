using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogsLoader
{
    private string debugTag = "DialogsLoader";

    private DialogsManager dialogsManager;

    private string pathBase;

    public DialogsLoader(DialogsManager dialogsManager)
    {
        this.dialogsManager = dialogsManager;
        dialogsManager.SetDialogsLoader(this);
    }

    public void SetPathBase(string pathBase)
    {
        this.pathBase = pathBase;
    }

    public string GetPathBase()
    {
        return pathBase;
    }

    public void SetDebugTag(string debugTag)
    {
        this.debugTag = debugTag;
    }

    public void Log(string text)
    {
        if (text != null && debugTag != null && debugTag.Length > 0)
        {
            Debug.Log(debugTag + ". " + text);
        }
    }

    public int LoadActorDialogs(string actor, string path)
    {
        int nfiles = 0;

        string pathFull = Path.Combine(pathBase, path);
        Log("Procesando ficheros de: " + pathFull);

        if (Directory.Exists(pathFull))
        {
            string[] dirEntries = Directory.GetDirectories(pathFull);
            Array.Sort(dirEntries);     // Se ordenan por nombre

            if (dirEntries.Length > 0)
            {
                foreach (string dirname in dirEntries)
                {
                    nfiles += LoadActorDialogs(actor, Path.Combine(path, dirname));
                }
            }


            string[] fileEntries = Directory.GetFiles(pathFull, "*.xml");   // Solo se cargan .xml´s
            Array.Sort(fileEntries);    // Se ordenan por nombre

            if (fileEntries.Length > 0)
            {
                foreach (string filename in fileEntries)
                {
                    LoadActorFile(actor, filename);
                    nfiles++;
                }
            }
        }

        Log("Ficheros procesados: "+ nfiles);
        
        return nfiles;
    }

    /// <summary>
    /// Carga de un solo fichero XML de díalogos para un actor determinado.
    /// </summary>
    /// <param name="actor">Nombre interno o referencia del actor</param>
    /// <param name="filename">Nombre del fichero XML que contiene diálogos del actor</param>
    private void LoadActorFile(string actor, string filename)
    {
        Log("Procesando fichero de diálogos: " + filename);

        XmlDocument doc = new XmlDocument();
        doc.Load(filename);
        LoadActorDocumentXML(actor, doc);
    }

    /// <summary>
    /// Carga de un documento XML de díalogos para un actor determinado.
    /// </summary>
    /// <param name="actor">Nombre interno o referencia del actor</param>
    /// <param name="doc">Documento XML que contiene los diálogos para el actor</param>
    private void LoadActorDocumentXML(string actor, XmlDocument doc)
    {
        ActorBrain actorBrain = dialogsManager.GetActorBrain(actor);

        XmlNodeList rootChildren = doc.DocumentElement.ChildNodes;

        foreach (XmlNode childNode in rootChildren)
        {
            switch (childNode.Name)
            {
                case "definitions":
                    ProcessDefinitions(actorBrain, childNode);
                    break;

                case "conditions":
                case "conditionsAnd":
                case "conditionsOr":
                    ConditionGroup conditions = ProcessConditionGroup(actorBrain, null, childNode);
                    actorBrain.GetDialogs().AddConditions(conditions);
                    break;

                case "dialog":
                    Dialog dialog = ProcessDialog(actorBrain, null, childNode);
                    dialogsManager.AddActorDialog(actor, dialog);
                    break;
            }
        }
    }


    private void ProcessDefinitions(ActorBrain actorBrain, XmlNode node)
    {
        List<dynamic> listSetElems = ProcessSets(actorBrain, null, node);
        foreach (SetElem setElem in listSetElems)
        {
            setElem.ApplySetDefinition();
        }
    }

    private Dialog ProcessDialog(ActorBrain actorBrain, Dialog parentDialog, XmlNode node)
    {
        Dialog dialog = new Dialog(actorBrain, parentDialog);

        XmlNode init = FindChildNode(node, "init");

        string refDialog = FindChildText(node, "ref");

        int priority = FindChildInt(node, "priority", 50);

        XmlNode nodeConditions = FindChildNode(node, "conditions");
        if (nodeConditions == null) nodeConditions = FindChildNode(node, "conditionsAnd");
        if (nodeConditions == null) nodeConditions = FindChildNode(node, "conditionsOr");

        XmlNode nodeSets = FindChildNode(node, "sets");
        XmlNode nodeTemplate = FindChildNode(node, "template");

        XmlNode nodeDialog = FindChildNode(node, "dialog");
        string actions = FindChildText(node, "actions");

        dialog.SetIsInit(init != null);
        dialog.SetRef(refDialog);
        dialog.SetPriority(priority);
        dialog.SetConditions(ProcessConditionGroup(actorBrain, dialog, nodeConditions));
        dialog.SetTemplate(ProcessTemplate(actorBrain, dialog, nodeTemplate));
        dialog.SetListSetElems(ProcessSets(actorBrain, dialog, nodeSets));
        dialog.SetActions(actions);

        foreach (XmlNode nodeChild in node.ChildNodes)
        {
            if (nodeChild.Name == "dialog")
            {
                ProcessDialog(actorBrain, dialog, nodeChild);
            }
        }

        return dialog;
    }

    private TemplateElem ProcessTemplate(ActorBrain actorBrain, Dialog dialog, XmlNode node)
    {
        if (node == null) return null;

        TemplateElem template = new TemplateElem(actorBrain, dialog);

        foreach (XmlNode childNode in node)
        {
            if (childNode.Name == "random")
            {
                template.AddRandomText(childNode.InnerText);
            }
        }

        if (template.CountRandom() == 0) template.SetText(node.InnerText);

        return template;
    }

    private Condition ProcessCondition(ActorBrain actorBrain, Dialog dialog, XmlNode childNode)
    {
        Condition condition = null;

        if (childNode.Name == "conditions" || childNode.Name == "conditionsAnd" || childNode.Name == "conditionsOr")
        {
            condition = ProcessConditionGroup(actorBrain, dialog, childNode);
        }
        else
        {
            switch (childNode.Name)
            {
                case "not":
                    condition = ProcessConditionNot(actorBrain, dialog, childNode);
                    break;
                case "refcondition":
                    condition = ProcessConditionRefCondition(actorBrain, childNode);
                    break;
                case "refexists":
                    condition = ProcessConditionRefExists(actorBrain, childNode);
                    break;
                case "refprev":
                    condition = ProcessConditionRefPrev(actorBrain, childNode);
                    break;
                case "exists":
                case "exist":
                case "existsvar":
                case "existvar":
                    condition = ProcessConditionExistsVar(actorBrain, childNode);
                    break;
                case "notexists":
                case "notexist":
                case "notexistsvar":
                case "notexistvar":
                    condition = ProcessConditionNotExistsVar(actorBrain, childNode);
                    break;
                case "saturated":
                    condition = ProcessConditionSaturated(actorBrain, dialog, childNode, false);
                    break;
                case "notsaturated":
                    condition = ProcessConditionSaturated(actorBrain, dialog, childNode, true);
                    break;
                case "ffunctrue":
                    condition = ProcessConditionFuncBool(actorBrain, childNode, true);
                    break;
                case "ffuncfalse":
                    condition = ProcessConditionFuncBool(actorBrain, childNode, false);
                    break;
                case "isinteger":
                    condition = ProcessConditionIsInteger(actorBrain, childNode);
                    break;
                case "exp":
                    condition = ProcessConditionExpr(actorBrain, childNode);
                    break;
                case "compare":
                    condition = ProcessConditionCompare(actorBrain, childNode);
                    break;
            }
        }

        if (condition != null) condition.SetElem(childNode.Name);
        return condition;
    }

    private ConditionGroup ProcessConditionGroup(ActorBrain actorBrain, Dialog dialog, XmlNode node)
    {
        ConditionGroup conditions = null;

        if (node == null) return null;

        string refCondition = FindChildText(node, "ref");

        if (node.Name == "conditions" || node.Name == "conditionsAnd") conditions = new ConditionAnd(actorBrain);
        else if (node.Name == "conditionsOr") conditions = new ConditionOr(actorBrain);

        foreach (XmlNode childNode in node)
        {
            Condition conditionChild = ProcessCondition(actorBrain, dialog, childNode);
            if (conditionChild != null) conditions.AddCondition(conditionChild);
        }

        if (conditions != null && refCondition != null)
        {
            conditions.SetRef(refCondition);
        }

        return conditions;
    }

    private ConditionIsInit ProcessConditionIsInit(ActorBrain actorBrain, XmlNode node)
    {
        ConditionIsInit condition = new ConditionIsInit(actorBrain);
        return condition;
    }

    private ConditionIsInit ProcessConditionNotIsInit(ActorBrain actorBrain, XmlNode node)
    {
        ConditionIsInit condition = new ConditionIsInit(actorBrain);
        condition.SetInvert(true);
        return condition;
    }

    private ConditionNot ProcessConditionNot(ActorBrain actorBrain, Dialog dialog, XmlNode node)
    {
        ConditionNot condition = new ConditionNot(actorBrain);
        Condition conditionIn = ProcessCondition(actorBrain, dialog, GetChildNode(node));
        condition.SetConditionIn(conditionIn);
        return condition;
    }

    private ConditionRefCondition ProcessConditionRefCondition(ActorBrain actorBrain, XmlNode node)
    {
        ConditionRefCondition condition = new ConditionRefCondition(actorBrain);
        condition.SetRefCondition(GetTextNode(node));
        return condition;
    }

    private ConditionRefPrev ProcessConditionRefPrev(ActorBrain actorBrain, XmlNode node)
    {
        Log("ProcessConditionRefPrev.xml = " + node);

        ConditionRefPrev condition = new ConditionRefPrev(actorBrain);

        int idxPrev = 1;
        string refPrev = FindChildText(node, "ref");
        if (refPrev != null)
        {
            idxPrev = ToInt(FindChildText(node, "idx"), 1);
        }
        else
        {
            refPrev = GetTextNode(node);
        }

        Log("ProcessConditionRefPrev.refPrev = " + refPrev + " / idxPrev = "+ idxPrev);

        condition.SetRefPrev(refPrev);
        condition.SetIdxPrev(idxPrev);

        return condition;
    }

    private ConditionRefPrev ProcessConditionRefExists(ActorBrain actorBrain, XmlNode node)
    {
        Log("ProcessConditionRefExists.xml = " + node);

        ConditionRefPrev condition = new ConditionRefPrev(actorBrain);

        int idxPrev = 1;
        string refExists = FindChildText(node, "ref");
        if (refExists == null)
        {
            refExists = GetTextNode(node);
        }

        Log("ProcessConditionRefExists.refExists = " + refExists);

        condition.SetRefPrev(refExists);
        condition.SetIdxPrev(0);

        return condition;
    }

    private ConditionExistsVar ProcessConditionExistsVar(ActorBrain actorBrain, XmlNode node)
    {
        ConditionExistsVar condition = new ConditionExistsVar(actorBrain);
        condition.SetVarName(node.InnerText);
        return condition;
    }

    private ConditionExistsVar ProcessConditionNotExistsVar(ActorBrain actorBrain, XmlNode node)
    {
        ConditionExistsVar condition = new ConditionExistsVar(actorBrain);
        condition.SetVarName(node.InnerText);
        condition.SetInvert(true);
        return condition;
    }

    private ConditionSaturated ProcessConditionSaturated(ActorBrain actorBrain, Dialog dialog, XmlNode node, bool not)
    {
        ConditionSaturated condition = new ConditionSaturated(actorBrain, dialog, not);
        condition.SetRefDialogCheck(node.InnerText);
        return condition;
    }

    private ConditionFunctionBool ProcessConditionFuncBool(ActorBrain actorBrain, XmlNode node, bool noinvert)
    {
        ConditionFunctionBool condition = new ConditionFunctionBool(actorBrain);
        condition.SetFuncName(node.InnerText);
        condition.SetInvert(!noinvert);
        return condition;
    }

    private ConditionIsInteger ProcessConditionIsInteger(ActorBrain actorBrain, XmlNode node)
    {
        ConditionIsInteger condition = new ConditionIsInteger(actorBrain);
        condition.SetValue(node.InnerText);
        return condition;
    }


    private ConditionExpr ProcessConditionExpr(ActorBrain actorBrain, XmlNode node)
    {
        ConditionExpr condition = new ConditionExpr(actorBrain);
        // Log("ProcessConditionExpr.nodeName=" + node.Name + " / nodeVal="+ node.Value + " / nodeInnerText="+ node.InnerText + " / nodeInnerXml=" + node.InnerXml + " / nodeOuterXml=" + node.OuterXml);
        condition.SetPattern(node.InnerText);
        return condition;
    }

    private ConditionCmp ProcessConditionCompare(ActorBrain actorBrain, XmlNode node)
    {
        ConditionCmp condition = new ConditionCmp(actorBrain);

        string oper = FindChildText(node, "op").ToLower();
        switch (oper)
        {
            case "=":
            case "==":
            case "equals":
                condition.SetOper(ConditionCmp.CmpOp.Equals);
                break;
            case "!=":
            case "&lt;&gt":
            case "different":
                condition.SetOper(ConditionCmp.CmpOp.Different);
                break;
            case "&gt;":
            case "greater":
                condition.SetOper(ConditionCmp.CmpOp.Greater);
                break;
            case "&lt;":
            case "less":
                condition.SetOper(ConditionCmp.CmpOp.Less);
                break;
            case "&gt;=":
            case "greaterequals":
                condition.SetOper(ConditionCmp.CmpOp.GreaterEquals);
                break;
            case "&lt;=":
            case "lessequals":
                condition.SetOper(ConditionCmp.CmpOp.LessEquals);
                break;
        }

        bool toLower = false;
        string strToLower = FindChildText(node, "tolower");
        if (strToLower != null)
        {
            toLower = true;
            strToLower = strToLower.Trim().ToLower();
            if (strToLower == "false" || strToLower == "none" || strToLower == "no" || strToLower == "f" || strToLower == "0") toLower = false;
        } 

        string param1 = null;
        string param2 = null;
        foreach (XmlNode childNode in node)
        {
            if (childNode.Name == "param" && param1 == null) param1 = childNode.InnerText;
            else if (childNode.Name == "param" && param1 != null && param2 == null) param2 = childNode.InnerText;
        }

        condition.SetParams(param1, param2);
        condition.SetToLower(toLower);

        string strType = FindChildText(node, "type");
        if (strType != null)
        {
            switch (strType)
            {
                case "string":
                    condition.SetType(ConditionCmp.CmpType.String);
                    break;
                case "int":
                    condition.SetType(ConditionCmp.CmpType.Int);
                    break;
                case "float":
                    condition.SetType(ConditionCmp.CmpType.Float);
                    break;
            }
        }

        return condition;
    }

    private List<dynamic> ProcessSets(ActorBrain actorBrain, Dialog dialog, XmlNode node)
    {
        List<dynamic> listSetElem = new List<dynamic>();
        if (node == null) return listSetElem;

        bool oneUse = ExistsChildNode(node, "oneuse");

        foreach (XmlNode childNode in node)
        {
            SetElem setElem = ProcessSetElem(actorBrain, dialog, childNode, oneUse);
            if (setElem != null) listSetElem.Add(setElem);
        }
        return listSetElem;
    }

    private SetElem ProcessSetElem(ActorBrain actorBrain, Dialog dialog, XmlNode node, bool oneUse)
    {
        dynamic setElem = null;
        
        switch (node.Name)
        {
            case "set":
                setElem = new SetElem(actorBrain, dialog);
                break;

            case "inc":
                setElem = new SetElemIncVar(actorBrain, dialog, false);
                break;

            case "dec":
                setElem = new SetElemIncVar(actorBrain, dialog, true);
                break;
        }

        if (setElem != null)
        {
            oneUse = oneUse || ExistsChildNode(node, "oneuse");

            string name = FindChildText(node, "name");
            string value = null;

            SetFunctionElem functionElem = null;

            XmlNode funcNode = FindChildNode(node, "value");
            if (funcNode != null)
            {
                functionElem = ProcessSetFunctionElem(actorBrain, GetChildNode(funcNode));
            } 

            if (functionElem == null)
            {
                value = FindChildText(node, "value");
                if (value != null && value.Length == 0) value = null;
            }


            if (name != null) name = name.ToLower();
            else
            {
                name = GetTextNode(node);
                if (name != null && name.Length == 0) name = null;
            }

            setElem.SetOneUse(oneUse);
            setElem.SetName(name);
            setElem.SetValue(value);
            setElem.SetFunction(functionElem);
        }

        return setElem;
    }

    private SetFunctionElem ProcessSetFunctionElem(ActorBrain actorBrain, XmlNode node)
    {
        if (node == null) return null;

        SetFunctionElem funcElem = null;

        // Obtención de valor o función hija, genérico para todas las funciones.
        string functionValue = GetTextNode(node);
        SetFunctionElem functionElemChild = ProcessSetFunctionElem(actorBrain, GetChildNode(node));

        Log("SetFunctionElem. node.Name = "+ node.Name);
        // Instanciación e incialización de la función específica.
        switch (node.Name.ToLower())
        {
            case "uppername":
                funcElem = ProcessSetFunctionUpperName(actorBrain, node);
                break;
            case "var":
            case "getvar":
            case "varvalue":
            case "getvarvalue":
                funcElem = ProcessSetFunctionGetVarValue(actorBrain, node);
                break;
        }

        if (funcElem != null)
        {
            // Asignación de valor y función hija, genérico para todas las funciones.
            funcElem.SetValue(functionValue);
            funcElem.SetFunction(functionElemChild);
        }
        return funcElem;
    }

    private SetFunctionUpperName ProcessSetFunctionUpperName(ActorBrain actorBrain, XmlNode node)
    {
        // Esta función no tiene ninguna inicialización específica a parte de las asignaciones genéricas de cualquier función

        SetFunctionUpperName funcElem = new SetFunctionUpperName(actorBrain);
        return funcElem;
    }

    private SetFunctionGetVarValue ProcessSetFunctionGetVarValue(ActorBrain actorBrain, XmlNode node)
    {
        // Esta función no tiene ninguna inicialización específica a parte de las asignaciones genéricas de cualquier función

        SetFunctionGetVarValue funcElem = new SetFunctionGetVarValue(actorBrain);
        return funcElem;
    }

    private XmlNode GetFunctionNodeChild(XmlNode node)
    {
        return node.FirstChild;
    }


    private XmlNode GetChildNode(XmlNode node)
    {
        return node.FirstChild;
    }

    private XmlNode FindChildNode(XmlNode node, string name)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.Name == name)
            {
                return child;
            }
        }
        return null;
    }

    private bool ExistsChildNode(XmlNode node, string name)
    {
        XmlNode childNode = FindChildNode(node, name);
        if (childNode != null) return true;
        return false;
    }

    private string FindChildText(XmlNode node, string name)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child.Name == name)
            {
                return child.InnerText;
            }
        }
        return null;
    }

    private string GetTextNode(XmlNode node)
    {
        string val = node.InnerText;
        if (val != null && val.Trim().Length == 0) val = null;
        return val;
    }

    private int FindChildInt(XmlNode node, string name, int def)
    {
        string valTxt = FindChildText(node, name);
        return ToInt(valTxt, def);
    }

    static public int ToInt(string val, int def)
    {
        int ret;
        if (val == null) return def;
        if (int.TryParse(val, out ret)) return ret;
        return def;
    }
}
