using UnityEditor;
using UnityEngine;

namespace LivingTomorrow.CMSApi
{
    public class LTCmsHierarchyMenu : Editor
    {
        [MenuItem("GameObject/LivingTomorrow/LT_CMS", false, 10)]
        static void Create_LT_CMS(MenuCommand menuCommand)
        {
            // Load your prefab here
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.livingtomorrow.cmsapi/Prefabs/LT_CMS.prefab");
            // Instantiate the prefab
            GameObject instance = Instantiate(prefab);

            // Ensure it gets parented properly if any object in the hierarchy is selected
            GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
        }
    }
}

