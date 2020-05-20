using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Dialog
{
    // private DialogsContext context;

    private ActorBrain actorBrain;

    private bool isInit;

    private int priority = 50;

    private string refDialog;

    private dynamic conditions;

    private TemplateElem template;

    public bool satureRandomTemplates;

    private List<dynamic> listSetElems;

    private List<string> listActions;

    // private List<Dialog> listChildDialogs;
    
    // Mapa de dialogos del actor, indexados por prioridad, donde para cada prioridad puede haber una lista de diálogos.
    private SortedList<int, List<Dialog>> dictListChildPriorityDialogs;

    private Dialog parentDialog;

    public static Regex regexVar = new Regex("(\\$\\{)([a-zA-Z0-9_]+)(\\})"); // Esto es la expresión de una variable como "${name}"

    public static Regex regexFunc = new Regex("(\\$\\{)(ffunc\\:[a-zA-Z0-9_]+)(\\})"); // Esto es la expresión de una variable como "${name}"

    static public string NormalizeSpaces(string text)
    {
        return Regex.Replace(text.Trim(), @"\s+", " "); // Elimina espacios al principio y al final y espacios dobles.
    }

    static private string GetMatchVarName(string text)
    {
        Match match = regexVar.Match(text);
        if (match != null)
        {
            GroupCollection groups = match.Groups;
            string varname = groups[2].Value;
            return varname;
        }
        return null;
    }

    static private string GetMatchFuncName(string text)
    {
        Debug.Log("Compare.GetMatchFuncName. text = '" + text + "'");
        Match match = regexFunc.Match(text);
        if (match != null)
        {
            GroupCollection groups = match.Groups;
            if (groups.Count > 1)
            {
                Debug.Log("Compare.GetMatchFuncName. groups = '" + groups + "' / size = " + groups.Count);
                Debug.Log("Compare.GetMatchFuncName. groups[0] = " + groups[0]);
                Debug.Log("Compare.GetMatchFuncName. groups[1] = " + groups[1]);
                Debug.Log("Compare.GetMatchFuncName. groups[2] = " + groups[2]);
                string funcname = groups[2].Value.Substring("ffunc:".Length);
                return funcname;
            }
        }
        return null;
    }

    static public string GetMatchExpValue(ActorBrain actorBrain, string text, string def)
    {
        Debug.Log("Compare.GetMatchExpValue. text = '" + text + "', def = "+ def);
        string varname = GetMatchVarName(text);
        Debug.Log("Compare.GetMatchExpValue. varname = '" + varname + "'");
        if (varname != null && varname.Length > 0)
        {
            string varvalue = actorBrain.GetVarValue(varname);
            Debug.Log("Compare.GetMatchExpValue. varvalue = '" + varvalue + "'");
            if (varvalue != null) return varvalue;
        } else
        {
            string funcname = GetMatchFuncName(text);
            Debug.Log("Compare.GetMatchExpValue. funcname = '" + funcname + "'");
            if (funcname != null && funcname.Length > 0)
            {
                string funcvalue = actorBrain.GetFuncValue(funcname);
                Debug.Log("Compare.GetMatchExpValue. funcvalue = '" + funcvalue + "'");
                if (funcvalue != null) return funcvalue;
            }
        }
        Debug.Log("Compare.GetMatchExpValue. return def = '" + def + "'");
        return def;
    }

    static public string ResolveMatchExpresions(ActorBrain actorBrain, string text)
    {
        if (text == null) return null;

        MatchCollection matchListVars = regexVar.Matches(text);
        MatchCollection matchListFunc = regexFunc.Matches(text);

        List<string> listTmpVars = new List<string>();
        List<string> listTmpFunc = new List<string>();

        // Obtiene los nombres de variables y los añade a la lista
        foreach (Match match in matchListVars)
        {
            GroupCollection groups = match.Groups;
            string varname = groups[2].Value;
            listTmpVars.Add(varname);
            // Debug.Log("GAIMLLoader. var = " + varname);
        }

        // Sustituye cada nombre de variable por su valor
        foreach (string varname in listTmpVars)
        {
            string varvalue = actorBrain.GetVarValue(varname);
            if (varvalue == null) varvalue = "VAR \"" + varname + "\" NO EXISTS!";
            text = Regex.Replace(text, "\\$\\{" + varname + "\\}", varvalue);
        }

        // Obtiene los nombres de las funciones y los añade a la lista
        foreach (Match match in matchListFunc)
        {
            GroupCollection groups = match.Groups;
            string funcname = groups[2].Value.Substring("ffunc:".Length);
            listTmpFunc.Add(funcname);
            // Debug.Log("GAIMLLoader. func = " + funcname);
        }

        // Sustituye cada nombre de función por su valor
        foreach (string funcname in listTmpFunc)
        {
            string funcvalue = actorBrain.GetFuncValue(funcname);
            if (funcvalue == null) funcvalue = "FUNC \"" + funcname + "\" NO EXISTS!";
            text = Regex.Replace(text, "\\$\\{ffunc\\:" + funcname + "\\}", funcvalue);
        }

        return text;
    }

    static public bool IsTrue(string val)
    {
        if (val == null || val.Trim().Length == 0) return false;

        int valInt;
        if (int.TryParse(val, out valInt))
        {
            if (valInt == 0) return false;
            else return true;
        }


        if (val == "true" || val.ToLower() == "true") return true;

        return false;
    }

    static public int ToInt(string val, int def)
    {
        int ret;
        if (val == null) return def;
        if (int.TryParse(val, out ret)) return ret;
        return def;
    }

    public Dialog(ActorBrain actorBrain, Dialog parentDialog)
    {
        this.actorBrain = actorBrain;
        this.parentDialog = parentDialog;

        listSetElems = new List<dynamic>();
        listActions = new List<string>();
        template = new TemplateElem(actorBrain, parentDialog);

        dictListChildPriorityDialogs = new SortedList<int, List<Dialog>>();

        if (parentDialog != null) parentDialog.AddChildDialog(this);

        satureRandomTemplates = false;

        isInit = false;
    }

    public void SetPriority(int priority)
    {
        this.priority = priority;
    }

    public int GetPriority()
    {
        return priority;
    }

    private void AddChildDialog(Dialog childDialog)
    {
        /*
        if (childDialog != null)
        {
            if (listChildDialogs == null) listChildDialogs = new List<Dialog>();
            listChildDialogs.Add(childDialog);
        }
        */

        List<Dialog> listDialogPriority = GetChildDialogsPriority(childDialog.GetPriority(), true);
        listDialogPriority.Add(childDialog);
    }

    private List<Dialog> GetChildDialogsPriority(int priority, bool create)
    {
        List<Dialog> listDialogPriority;
        if (!dictListChildPriorityDialogs.TryGetValue(priority, out listDialogPriority) && create)
        {
            listDialogPriority = new List<Dialog>();
            dictListChildPriorityDialogs.Add(priority, listDialogPriority);
        }
        return listDialogPriority;
    }

    public Dialog GetParentDialog()
    {
        return parentDialog;
    }

    public Dialog GetChildDialog(string refDialog)
    {
        /*
        if (listChildDialogs == null) return null;

        foreach (Dialog childDialog in listChildDialogs)
        {
            if (childDialog.GetRef() != null && childDialog.GetRef() == refDialog) return childDialog;
        }
        */

        foreach (int priority in dictListChildPriorityDialogs.Keys)
        {
            List<Dialog> listDialogs = GetChildDialogsPriority(priority, false);
            foreach (Dialog dialog in listDialogs)
            {
                if (dialog.GetRef() != null && dialog.GetRef() == refDialog) return dialog;
            }
        }

       return null;
    }

    // Obtiene la lista de hermanos de este diálogo (sin contar al propio diálogo), donde la lista está ya ordenada por prioridad y orden de creación.
    public List<Dialog> GetListBrotherDialogs()
    {
        List<Dialog> listBrotherDialogs = new List<Dialog>();

        if (parentDialog != null)
        {
            /*
            foreach (Dialog brotherDialog in parentDialog.listChildDialogs)
            {
                if (brotherDialog != this) listBrotherDialogs.Add(brotherDialog);
            }
            */


            foreach (int priority in dictListChildPriorityDialogs.Keys)
            {
                List<Dialog> listDialogs = GetChildDialogsPriority(priority, false);
                foreach (Dialog dialog in listDialogs)
                {
                    if (dialog != this) listBrotherDialogs.Add(dialog);
                }
            }

        }
        return listBrotherDialogs;
    }

    public void SetIsInit(bool isInit)
    {
        this.isInit = isInit;
    }

    public bool IsInit()
    {
        return isInit;
    }

    public void SetRef(string refDialog)
    {
        this.refDialog = refDialog;
    }

    public string GetRef()
    {
        return refDialog;
    }

    public string GetRefResolved()
    {
        return ResolveMatchExpresions(actorBrain, refDialog);
    }

    public void SetConditions(ConditionGroup conditions)
    {
        this.conditions  = conditions;
    }

    public ConditionGroup GetConditions()
    {
        return conditions;
    }

    public void SetTemplate(TemplateElem template)
    {
        this.template = template;
    }

    public string GetTemplate()
    {
        if (template == null) return null;

        string text = template.GetText();
        return text;
    }

    public string GetResolvedTemplate()
    {
        if (template == null) return null;

        string text = ResolveMatchExpresions(actorBrain, template.GetResolvedText());
        return text;
    }

    public void SetActions(string actions)
    {
        if (actions != null)
        {
            string[] arrActions = actions.Split('|');
            listActions = new List<string>(arrActions);
        }
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

    public void SetListSetElems(List<dynamic> listSetElem)
    {
        listSetElems = listSetElem;
    }

    /*
    public void SetMatchText(string text)
    {
        if (conditions == null) return;

        conditions.SetMatchText(text);


        foreach (int priority in dictListChildPriorityDialogs.Keys)
        {
            List<Dialog> listDialogs = GetChildDialogsPriority(priority, false);
            foreach (Dialog dialog in listDialogs)
            {
                // Debug.Log("DIALOGTREE.Eval(). refDialog = " + dialog.refDialog);
                dialog.SetMatchText(text);
            }
        }

    }
    */

    /*
    public bool Eval()
    {
        if (conditions == null) return true;

        return conditions.Eval();
    }
    */

    public void Eval(DialogRequest request, DialogResponse response)
    {
        bool okConditions = true;

        // Si no se cumplen las condiciones, se devuelve false.
        if (conditions != null)
        {
            okConditions = conditions.Eval(request);
        }
        if (!okConditions) return;

        // Si se cumplen las condiciones
        ApplySets(); // Se setean las variables

        // Se establece la respuesta
        response.SetDialog(this);

        /*
        if (listChildDialogs != null && listChildDialogs.Count > 0)
        {
            // Se evaluan los dialogos hijos
            foreach (Dialog childDialog in listChildDialogs)
            {
                childDialog.Eval(dialogResponse);
                if (dialogResponse.IsValid()) return;
            }
        }
        */

        Debug.Log("DIALOGTREE.Eval(). refDialog = "+ refDialog);
        foreach (int priority in dictListChildPriorityDialogs.Keys)
        {
            List<Dialog> listDialogs = GetChildDialogsPriority(priority, false);
            foreach (Dialog dialog in listDialogs)
            {
                Debug.Log("DIALOGTREE.Eval(). refDialog = " + dialog.refDialog);

                dialog.Eval(request, response);
                if (response.IsValid()) return;
            }
        }

        Debug.Log("DIALOGTREE.Eval(). respinse.refDialog = " + refDialog);

        // Solo se establece el texto y las acciones si los hijos no han devuelto una respuesta válida. De lo contrario el texto y las acciones serán las de los hijas.
        response.SetText(GetResolvedTemplate());
        response.SetActions(GetListActions());
    }

    public void ApplySets()
    {
        foreach (SetElem setElem in listSetElems)
        {
            setElem.ApplySet();
        }
    }
}

public class TemplateElem
{
    private ActorBrain actorBrain;

    private Dialog dialog;

    private System.Random random = new System.Random();

    private string text;

    private List<string> listRandomText = new List<string>();

    public TemplateElem(ActorBrain actorBrain, Dialog dialog)
    {
        this.actorBrain = actorBrain;
        this.dialog = dialog;
    }

    public Dialog GetDialog()
    {
        return dialog;
    }

    public void SetText(string text)
    {
        this.text = Dialog.NormalizeSpaces(text);
    }

    public string GetText()
    {
        int idxRand = GetRandomTextIdx();
        return GetTextRandom(idxRand);
    }

    public string GetResolvedText()
    {
        string textRand = actorBrain.GetMemory().GetTemplateRandom(this, dialog.satureRandomTemplates);
        return textRand;
    }

    public string GetTextRandom(int idxRand)
    {
        if (idxRand < 0 || idxRand >= listRandomText.Count) return text;
        return listRandomText[idxRand];
    }

    public void AddRandomText(string text)
    {
        listRandomText.Add(Dialog.NormalizeSpaces(text));
    }

    public int GetRandomTextIdx()
    {
        if (listRandomText.Count == 0) return -1;
        if (listRandomText.Count == 1) return 0;
        return random.Next(listRandomText.Count);
    }
    
    /*
    private string GetRandomText2()
    {
        if (listRandomText.Count == 0) return null;
        if (listRandomText.Count == 1) return listRandomText[0];
        return listRandomText[random.Next(listRandomText.Count)];
    }
    */

    public int CountRandom()
    {
        return listRandomText.Count;
    }
}

public abstract class Condition
{
    protected string elem;

    private string refCondition;

    protected ActorBrain actorBrain;

    protected Dialog dialog;

    public Condition(ActorBrain actorBrain)
    {
        this.actorBrain = actorBrain;
    }

    public Condition(ActorBrain actorBrain, Dialog dialog)
    {
        this.actorBrain = actorBrain;
        this.dialog = dialog;
    }

    public void SetDialog(Dialog dialog)
    {
        this.dialog = dialog;
    }

    public void SetElem(string elem)
    {
        this.elem = elem;
    }

    public void SetRef(string refCondition)
    {
        this.refCondition = refCondition;
    }

    public string GetRef()
    {
        return refCondition;
    }

    public virtual bool Eval(DialogRequest request)
    {
        LogEvalElemStart();
        LogEvalElemRet(false);
        return false;
    }

    /*
    public virtual void SetMatchText(string text)
    {

    }
    */

    protected void LogEvalElemStart()
    {
        Debug.Log("Condition.Eval. elem = " + elem);
    }

    protected void LogEvalElemStart(string text)
    {
        Debug.Log("Condition.Eval. elem = " + elem + " / "+ text);
    }

    protected void LogEvalElemRet(bool ret)
    {
        Debug.Log("Condition.Eval. elem = " + elem + " / ret = "+ ret);
    }
}

public class ConditionIsInit : Condition
{
    private bool invert;

    public ConditionIsInit(ActorBrain actorBrain) : base(actorBrain)
    {
    }

    public void SetInvert(bool invert)
    {
        this.invert = invert;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();
        bool ret = !invert ? actorBrain.GetMemory().IsInit() : !actorBrain.GetMemory().IsInit();
        LogEvalElemRet(ret);
        return ret;
    }
}

public class ConditionRefCondition : Condition
{
    private string refCondition;

    public ConditionRefCondition(ActorBrain actorBrain) : base(actorBrain)
    {
    }

    public void SetRefCondition(string refCondition)
    {
        this.refCondition = (refCondition != null && refCondition.Trim().Length > 0) ? refCondition : null;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart("refCondition = "+ refCondition);
        Debug.Log("ConditionRefCondition.Eval.refCondition = "+ refCondition);

        if (refCondition == null)
        {
            LogEvalElemRet(true);
            return true;
        }

        Condition condition = actorBrain.GetDialogs().GetCondition(refCondition);
        Debug.Log("ConditionRefCondition.Eval.condition = " + condition);

        if (condition != null)
        {
            Debug.Log("ConditionRefCondition.Eval.conditionRef = " + condition.GetRef());
            bool ret = condition.Eval(request);
            LogEvalElemRet(ret);
            return ret;
        }
        LogEvalElemRet(false);
        return false;
    }
}

public class ConditionRefPrev: Condition
{
    private string refPrev = null;
    private int idxPrev = 1;

    public ConditionRefPrev(ActorBrain actorBrain) : base(actorBrain)
    {
    }

    public void SetRefPrev(string refPrev)
    {
        this.refPrev = refPrev;
    }

    public void SetIdxPrev(int idxPrev)
    {
        this.idxPrev = idxPrev;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart("refPrev = "+ refPrev);
        Debug.Log("ConditionRefPrev.refPrev = " + refPrev + " / idxPrev = " + idxPrev);
        if (refPrev == null || refPrev.Length == 0)
        {
            LogEvalElemRet(false);
            return false;
        }

        bool ret;

        if (idxPrev == 0)
        {
            ret = actorBrain.GetMemory().ExistsDialogResponse(Dialog.ResolveMatchExpresions(actorBrain, refPrev));
            LogEvalElemRet(ret);
            return ret;
        }

        ret = actorBrain.GetMemory().ExistsPrevRefDialogResponse(Dialog.ResolveMatchExpresions(actorBrain, refPrev), idxPrev);
        LogEvalElemRet(ret);
        return ret;
    }
}


public class ConditionExistsVar : Condition
{
    private string varname;

    private bool invert;

    public ConditionExistsVar(ActorBrain actorBrain) : base(actorBrain)
    {
        invert = false;
    }

    public void SetVarName(string varname)
    {
        this.varname = varname;
    }

    public void SetInvert(bool invert)
    {
        this.invert = invert;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart("varname = "+ varname);

        bool ret;
        string varvalue = actorBrain.GetVarValue(varname);
        if (!invert) ret = (varvalue != null ? true : false);
        else ret = (varvalue != null ? false : true);

        LogEvalElemRet(ret);
        return ret;
    }
}

public class ConditionSaturated : Condition
{
    private string refDialogCheck;

    private bool not;

    public ConditionSaturated(ActorBrain actorBrain, Dialog dialog, bool not) : base(actorBrain, dialog)
    {
        this.not = not;
        dialog.satureRandomTemplates = true;
    }

    public void SetRefDialogCheck(string refDialogCheck)
    {
        this.refDialogCheck = refDialogCheck;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart("refDialogCheck = " + refDialogCheck);

        Dialog dialogCheck = dialog;
        if (refDialogCheck != null && refDialogCheck.Length > 0)
        {
            dialogCheck = actorBrain.GetDialogs().GetDialogInPath(dialog, refDialogCheck);
        }

        bool saturated = actorBrain.GetMemory().IsDialogSatured(dialogCheck);
        if (not) saturated = !saturated;

        LogEvalElemRet(saturated);
        return saturated;
    }
}

public class ConditionFunctionBool : Condition
{
    private string funcname;

    private bool invert;

    public ConditionFunctionBool(ActorBrain actorBrain) : base(actorBrain)
    {
        invert = false;
    }

    public void SetFuncName(string funcname)
    {
        this.funcname = funcname;
    }

    public void SetInvert(bool invert)
    {
        this.invert = invert;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();

        string varvalue = actorBrain.GetFuncValue(funcname);
        bool val = Dialog.IsTrue(varvalue);
        bool ret = (!invert ? val : !val);

        LogEvalElemRet(ret);
        return ret;
    }
}

public class ConditionIsInteger : Condition
{
    private string value;

    public ConditionIsInteger(ActorBrain actorBrain) : base(actorBrain)
    {
    }

    public void SetValue(string value)
    {
        this.value = value;
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();

        int num;
        string ret = Dialog.GetMatchExpValue(actorBrain, value, value);
        bool isInteger = int.TryParse(ret, out num);

        LogEvalElemRet(isInteger);
        return isInteger;
    }
}

public class ConditionCmp : Condition
{
    public ConditionCmp(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public enum CmpOp : int
    {
        Different = 0,
        Equals = 1,
        Greater = 2,
        GreaterEquals = 3,
        Less = 4,
        LessEquals = 5
    }

    public enum CmpType : int
    {
        String = 0,
        Int = 1,
        Float = 2
    }

    private CmpOp oper = CmpOp.Equals;

    private CmpType type = CmpType.String;

    string strA = null;
    string strB = null;

    bool toLower;

    private int intA = int.MinValue;
    private int intB = int.MinValue;

    private float floatA = float.MinValue;
    private float floatB = float.MinValue;

    public void SetOper(CmpOp oper)
    {
        this.oper = oper;
    }

    public void SetType(CmpType type)
    {
        this.type = type;
    }

    public string GetOperStr()
    {
        switch (oper)
        {
            case CmpOp.Equals: return "equals";
            case CmpOp.Different: return "different";
            case CmpOp.Greater: return "greater";
            case CmpOp.Less: return "less";
            case CmpOp.GreaterEquals: return "greaterequals";
            case CmpOp.LessEquals: return "lessequals";
        }
        return "";
    }

    public void SetToLower(bool toLower)
    {
        this.toLower = toLower;
    }

    public string GetParamStrA()
    {
        return strA;
    }

    public string GetParamStrB()
    {
        return strB;
    }


    public void SetParams(string strA, string strB)
    {
        this.strA = strA;
        this.strB = strB;
        intA = intB = 0;
        floatA = floatB = 0f;
    }

    public void SetParams(int intA, int intB)
    {
        type = CmpType.Int;
        this.intA = intA;
        this.intB = intB;
        strA = strB = null;
        floatA = floatB = 0f;
    }

    public void SetParams(float floatA, float floatB)
    {
        type = CmpType.Float;
        this.floatA = floatA;
        this.floatB = floatB;
        strA = strB = null;
        intA = intB = 0;
    }

    private void NormalizeTypeValues()
    {
        string valA = GetParsedValue(strA);
        string valB = GetParsedValue(strB);

        if (type != CmpType.String)
        {

            if (type == CmpType.Int)
            {
                if (valA != null) int.TryParse(valA, out intA);
                if (valB != null) int.TryParse(valB, out intB);
            }
            else
            {
                if (type == CmpType.Float)
                {
                    if (valA != null) float.TryParse(valA, out floatA);
                    if (valB != null) float.TryParse(valB, out floatB);
                }
            }
        }
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();

        NormalizeTypeValues();

        Debug.Log("Compare.EvalEquals (op=" + oper + ", type=" + type + "). strA=" + strA + " / strB=" + strB + "). intA=" + intA + " / intB=" + intB + "). floatA=" + floatA + " / floatB=" + floatB);

        switch (oper)
        {
            case CmpOp.Equals: return EvalEquals();
            case CmpOp.Different: return EvalDifferent();
            case CmpOp.Greater: return EvalGreater();
            case CmpOp.Less: return EvalLess();
            case CmpOp.GreaterEquals: return EvalGreaterEquals();
            case CmpOp.LessEquals: return EvalLessEquals();
        }
        return EvalEquals();
    }

    private string GetParsedValue(string text)
    {
        Debug.Log("Compare.GetParsedValue. text = '" + text +"'");
        string val = Dialog.GetMatchExpValue(actorBrain, text, text);
        Debug.Log("Compare.GetParsedValue.1 val = '" + val + "'");
        if (val != null && toLower) val = val.ToLower();
        Debug.Log("Compare.GetParsedValue.2 val = '" + val + "'");
        return val;
    }

    private int IntValue(string text, int def)
    {
        int ret;
        if (int.TryParse(text, out ret)) return ret;
        return def;
    }

    public bool EvalDifferent()
    {
        return !EvalEquals();
    }

    public bool EvalEquals()
    {
        Debug.Log("Compare.EvalEquals. strA=" + strA + " / strB=" + strB);
        Debug.Log("Compare.EvalEquals.Parsed (toLower=" + (toLower ? "true" : "false") + ") strA=" + GetParsedValue(strA) + " / strB=" + GetParsedValue(strB));
        if (type == CmpType.String && strA != null && strB != null && GetParsedValue(strA) == GetParsedValue(strB)) return true;
        if (type == CmpType.Int && intA != int.MinValue && intB != int.MinValue && intA == intB) return true;
        if (type == CmpType.Float && floatA != float.MinValue && floatB != float.MinValue && floatA == floatB) return true;
        if (strA == null && strB == null && intA == int.MinValue && intB == int.MinValue && floatA == float.MinValue && floatB == float.MinValue) return true;
        return false;
    }

    public bool EvalGreater()
    {
        Debug.Log("Compare.EvalGreater. strA=" + strA + " / strB=" + strB);
        if (type == CmpType.String && strA != null && strB != null && String.Compare(GetParsedValue(strA), GetParsedValue(strB)) > 0) return true;
        if (type == CmpType.Int && intA != int.MinValue && intB != int.MinValue && intA > intB) return true;
        if (type == CmpType.Float && floatA != float.MinValue && floatB != float.MinValue && floatA > floatB) return true;
        return false;
    }

    public bool EvalLess()
    {
        Debug.Log("Compare.EvalLess. strA=" + strA + " / strB=" + strB);
        if (type == CmpType.String && strA != null && strB != null && String.Compare(GetParsedValue(strA), GetParsedValue(strB)) < 0) return true;
        if (type == CmpType.Int && intA != int.MinValue && intB != int.MinValue && intA < intB) return true;
        if (type == CmpType.Float && floatA != float.MinValue && floatB != float.MinValue && floatA < floatB) return true;
        return false;
    }

    public bool EvalGreaterEquals()
    {
        Debug.Log("Compare.EvalGreaterEquals. strA=" + strA + " / strB=" + strB);
        if (type == CmpType.String && strA != null && strB != null && GetParsedValue(strA) == GetParsedValue(strB)) return true;
        if (type == CmpType.String && strA != null && strB != null && String.Compare(GetParsedValue(strA), GetParsedValue(strB)) >= 0) return true;
        if (type == CmpType.Int && intA != int.MinValue && intB != int.MinValue && intA >= intB) return true;
        if (type == CmpType.Float && floatA != float.MinValue && floatB != float.MinValue && floatA >= floatB) return true;
        if (strA == null && strB == null && intA == int.MinValue && intB == int.MinValue && floatA == float.MinValue && floatB == float.MinValue) return true;
        return false;
    }

    public bool EvalLessEquals()
    {
        Debug.Log("Compare.EvalLessEquals. strA=" + strA + " / strB=" + strB);
        if (type == CmpType.String && strA != null && strB != null && GetParsedValue(strA) == GetParsedValue(strB)) return true;
        if (type == CmpType.String && strA != null && strB != null && String.Compare(GetParsedValue(strA), GetParsedValue(strB)) <= 0) return true;
        if (type == CmpType.Int && intA != int.MinValue && intB != int.MinValue && intA <= intB) return true;
        if (type == CmpType.Float && floatA != float.MinValue && floatB != float.MinValue && floatA <= floatB) return true;
        if (strA == null && strB == null && intA == int.MinValue && intB == int.MinValue && floatA == float.MinValue && floatB == float.MinValue) return true;
        return false;
    }
}

public class ConditionExpr : Condition
{
    private string pattern;
    private Regex regex;

    // protected string text;

    private List<string> listTmpVars;

    public ConditionExpr(ActorBrain actorBrain) : base(actorBrain)
    {
        listTmpVars = new List<string>();
    }

    /*
    public override void SetMatchText(string text)
    {
        Debug.Log("GAIMLLoader. SetText. text = '"+ text +"' / listTmpVarsCount = " + listTmpVars.Count + " / listTmpVars = " + listTmpVars);
        this.text = text;
    }
    */

    public string GetPattern()
    {
        return pattern;
    }

    public void SetPattern(string expr)
    {
        pattern = expr;

        MatchCollection matchList = Dialog.regexVar.Matches(expr);
        Debug.Log("GAIMLLoader. total = " + matchList.Count);

        foreach (Match match in matchList)
        {
            GroupCollection groups = match.Groups;
            string varname = groups[2].Value;
            listTmpVars.Add(varname);
            Debug.Log("GAIMLLoader. var = " + varname);
        }

        expr = Regex.Replace(expr, "\\s+", "\\s+"); // Se sustituyen todos los espacios juntos por "\\s+" para la expresión regular.
        expr = Regex.Replace(expr, "\\*", ".*");    // Se sustituyen todos los astericos "*" por ".*" para la expresión regular.

        Debug.Log("GAIMLLoader. listTmpVarsCount = " + listTmpVars.Count + " / listTmpVars = " + listTmpVars);
        foreach (string varname in listTmpVars)
        {
            Debug.Log("GAIMLLoader. replace var = " + varname);
            expr = Regex.Replace(expr, "\\$\\{" + varname + "\\}", "[\\s\\,\\.\\;]*(\\w+)[\\s\\,\\.\\;]*");
            Debug.Log("GAIMLLoader. regex0= " + expr);
        }
        Debug.Log("GAIMLLoader. listTmpVarsCount = " + listTmpVars.Count + " / listTmpVars = " + listTmpVars);

        SetRegex(expr);
    }

    public void SetRegex(string expr)
    {
        Debug.Log("GAIMLLoader. regex= " + expr);
        Debug.Log("GAIMLLoader. SetRegex. listTmpVarsCount = " + listTmpVars.Count + " / listTmpVars = " + listTmpVars);
        regex = new Regex(expr);
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart("pattern = "+ pattern);

        string text = request.GetText();

        Debug.Log("GAIMLLoader. EVAL. text = '"+ text +"' / listTmpVarsCount = " + listTmpVars.Count + " / listTmpVars = " + listTmpVars);
        if (text == null)
        {
            LogEvalElemRet(false);
            return false;
        }

        Match match = regex.Match(text);
        if (match.Success)
        {
            GroupCollection groups = match.Groups;
            Debug.Log("GAIMLLoader. Group.count = " + groups.Count + " / listTmpVarsCount = "+ listTmpVars.Count + " / listTmpVars = "+ listTmpVars);

            for (int i = 0; i < groups.Count -1; i++)
            {
                Debug.Log("GAIMLLoader. Group [" + i + "] = " + i);
                string varname = listTmpVars[i];
                Debug.Log("GAIMLLoader. Group.varname ["+i+"] = " + varname);

                if (varname != null && varname.Length > 0)
                {
                    string varvalue = groups[i+1].Value;
                    Debug.Log("GAIMLLoader. Variable: " + varname +" = "+ varvalue);

                    actorBrain.GetMemory().SetPermanetVar(varname, varvalue);
                }
            }
            LogEvalElemRet(true);
            return true;
        }
        LogEvalElemRet(false);
        return false;
    }
}

public class ConditionNot : Condition
{
    protected dynamic conditionIn = null;

    public ConditionNot(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public dynamic GetCondition()
    {
        return conditionIn;
    }


    public void SetConditionIn(Condition condition)
    {
        conditionIn = condition;
    }

    /*
    public override void SetMatchText(string text)
    {
        if (conditionIn != null) conditionIn.SetMatchText(text);
    }
    */

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();
        bool ret = !conditionIn.Eval(request);
        LogEvalElemRet(ret);
        return ret;
    }
}

public abstract class ConditionGroup : Condition
{
    protected List<dynamic> listConditions = new List<dynamic>();

    public ConditionGroup(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public List<dynamic> GetConditions()
    {
        return listConditions;
    }


    public abstract void AddCondition(Condition condition);

    /*
    public override void SetMatchText(string text)
    {
        Debug.Log("ConditionGroup. self = "+ this);
        foreach (dynamic condition in listConditions)
        {
            Debug.Log("ConditionGroup. child = " + condition);
            condition.SetMatchText(text);
        }
    }
    */
}

public class ConditionAnd : ConditionGroup
{
    public ConditionAnd(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public override void AddCondition(Condition condition)
    {
        listConditions.Add(condition);
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();
        foreach (Condition condition in listConditions)
        {
            if (!condition.Eval(request))
            {
                LogEvalElemRet(false);
                return false;
            }
        }
        LogEvalElemRet(true);
        return true;
    }
}

public class ConditionOr : ConditionGroup
{
    public ConditionOr(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public override void AddCondition(Condition condition)
    {
        listConditions.Add(condition);
    }

    public override bool Eval(DialogRequest request)
    {
        LogEvalElemStart();
        foreach (Condition condition in listConditions)
        {
            if (condition.Eval(request))
            {
                LogEvalElemRet(true);
                return true;
            }
        }
        LogEvalElemRet(false);
        return false;
    }
}

public class SetElem
{
    protected ActorBrain actorBrain;

    protected Dialog dialog;

    protected string name;

    protected string value;

    protected dynamic function;

    protected bool oneUse;

    public SetElem(ActorBrain actorBrain, Dialog dialog)
    {
        this.actorBrain = actorBrain;
        this.dialog = dialog;

        name = null;
        value = null;
        function = null;

        oneUse = false;
    }

    public void SetName(string name)
    {
        this.name = name;
    }

    public void SetValue(string value)
    {
        this.value = value;
    }

    public void SetFunction(dynamic function)
    {
        this.function = function;
    }

    public void SetOneUse(bool oneUse)
    {
        this.oneUse = oneUse;
    }

    public string GetName()
    {
        return name;
    }

    public string GetPlainValue()
    {
        return value;
    }

    public string GetFunctionValue()
    {
        Debug.Log("DialogsElems. SetElem.GetFunctionValue (" + name + ")");
        if (function != null) return function.GetFunctionValue();
        return value;
    }

    public bool IsOneUsed()
    {
        if (!oneUse || dialog == null) return false;

        bool used = actorBrain.GetMemory().IsUsedDialog(dialog);
        return used;
    }

    public virtual void ApplySet()
    {
        Debug.Log("DialogsElems. SetElem.ApplySet (name="+ name +" / value="+ value +" / func="+ function +" / oneUse = "+ oneUse +")");

        if (!IsOneUsed())
        {
            value = GetFunctionValue();
            actorBrain.GetMemory().SetPermanetVar(name, value);
        }
    }

    public void ApplySetDefinition()
    {
        Debug.Log("DialogsElems. SetElem.ApplySetDefinition (name=" + name + " / value=" + value + " / func=" + function + " / oneUse = " + oneUse + ")");

        if (!IsOneUsed())
        {
            value = GetFunctionValue();
            actorBrain.GetMemory().SetDefinitionVar(name, value);
        }
    }
}

public class SetElemIncVar : SetElem
{
    private bool dec = false;

    public SetElemIncVar(ActorBrain actorBrain, Dialog dialog, bool dec) : base(actorBrain, dialog)
    {
        this.dec = dec;
    }

    public override void ApplySet()
    {
        Debug.Log("DialogsElems. SetElemIncVar.ApplySet (name=" + name + " / value=" + value + " / func=" + function + " / oneUse = " + oneUse + ")");

        if (!IsOneUsed())
        {
            value = GetFunctionValue();

            string varamount = value;
            if (varamount == null) varamount = "1";
            if (dec && !varamount.StartsWith("-")) varamount = "-" + varamount;

            actorBrain.GetMemory().IncPermanetVar(name, varamount);
        }
    }
}

public abstract class SetFunctionElem
{
    protected ActorBrain actorBrain;

    protected string value;

    protected dynamic function;

    public SetFunctionElem(ActorBrain actorBrain)
    {
        this.actorBrain = actorBrain;

        value = null;
        function = null;
    }

    public void SetValue(string value)
    {
        this.value = value;
    }

    public void SetFunction(dynamic function)
    {
        this.function = function;
    }

    public dynamic GetFunction()
    {
        return function;
    }

    public string GetPlainValue()
    {
        return value;
    }

    public string GetFunctionValue()
    {
        Debug.Log("DialogsElems. SetFunctionElem.GetFunctionValue (" + this + ")");

        string ret = value;
        if (function != null) ret = function.GetFunctionValue();
        ret = ApplyFunction(ret);
        return ret;
    }

    public abstract string ApplyFunction(string value);
}

public class SetFunctionUpperName : SetFunctionElem
{
    public SetFunctionUpperName(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public override string ApplyFunction(string value)
    {
        Debug.Log("DialogsElems. SetFunctionUpperName.ApplyFunction.value: " + value);
        if (value == null) return null;

        string ret = value.Trim();
        if (ret.Trim().Length == 0) return value;

        string char0 = value.Substring(0, 1);
        string part = value.Substring(1, value.Length -1);

        ret = char0.ToUpper() + part.ToLower();
        Debug.Log("DialogsElems. SetFunctionUpperName.ApplyFunction.ret: "+ ret);
        return ret;
    }
}

public class SetFunctionGetVarValue : SetFunctionElem
{
    public SetFunctionGetVarValue(ActorBrain actorBrain) : base(actorBrain)
    {

    }

    public override string ApplyFunction(string value)
    {
        Debug.Log("DialogsElems. SetFunctionGetVarValue.ApplyFunction.value: " + value);

        string ret = actorBrain.GetVarValue(value);
        Debug.Log("DialogsElems. SetFunctionGetVarValue.ApplyFunction.ret: " + ret);
        return ret;
    }
}
