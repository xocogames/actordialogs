using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DictPriorityListItems<T>
{
    private SortedList<int, List<T>> dictPriorityList;

    public DictPriorityListItems()
    {
        dictPriorityList = new SortedList<int, List<T>>();
    }

    public IList<int> GetListPriorities()
    {
        return dictPriorityList.Keys;
    }

    public List<T> GetPriorityList(int priority)
    {
        return GetPriorityList(priority, false);
    }

    private List<T> GetPriorityList(int priority, bool create)
    {
        List<T> listPriority;
        if (!dictPriorityList.TryGetValue(priority, out listPriority) && create)
        {
            listPriority = new List<T>();
            dictPriorityList.Add(priority, listPriority);
        }
        return listPriority;
    }

    public void AddPriorityItem(int priority, T item)
    {
        List<T> listPriority = GetPriorityList(priority, true);
        listPriority.Add(item);
    }
}
