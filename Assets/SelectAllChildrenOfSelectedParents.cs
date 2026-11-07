using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SelectDeepestChildrenOfSelected
{
    /*
    private static void SelectDeepestChildren()
    {
        Transform[] selectedRoots = Selection.transforms;
        List<Object> result = new List<Object>();

        foreach (Transform root in selectedRoots)
        {
            TraverseForLeaves(root, result);
        }

        Selection.objects = result.ToArray();
    }

    private static void TraverseForLeaves(Transform node, List<Object> result)
    {
        int childCount = node.childCount;

        if (childCount == 0)
        {
            // у узла нет детей -> это лист, но родителя не выбираем, 
            // поэтому добавляем только если node не является корнем,
            // на который ты кликнул руками
            if (!IsRootSelection(node) && !result.Contains(node.gameObject))
                result.Add(node.gameObject);
            return;
        }

        // есть дети -> идём ниже, сами узел не добавляем
        for (int i = 0; i < childCount; i++)
        {
            Transform child = node.GetChild(i);
            TraverseForLeaves(child, result);
        }
    }

    private static bool IsRootSelection(Transform node)
    {
        // корни текущей выделенной группы в редакторе
        Transform[] roots = Selection.transforms;
        foreach (Transform r in roots)
        {
            if (r == node)
                return true;
        }
        return false;
    }
    */
}