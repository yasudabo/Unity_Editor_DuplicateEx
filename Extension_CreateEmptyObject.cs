using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


//本程式複製自：
//http://tsubakit1.hateblo.jp/entry/2017/04/28/005237

//複製後，選擇複製的物件的方式參考自：
//https://github.com/baba-s/unity-shortcut-key-plus/blob/master/Scripts/Editor/DuplicateWithoutSerialNumber.cs

//更改排序位置參閱 (SiblingIndex)；
//http://kan-kikuchi.hatenablog.com/entry/SiblingIndex

//修改熱鍵參閱：
//https://unity3d.com/learn/tutorials/topics/interface-essentials/unity-editor-extensions-menu-items
// ％ = CTRL
// ＃ = Shift
// ＆ = Alt
// LEFT/RIGHT/UP/DOWN = 箭頭鍵
// F1 ~ F12
// HOME，END，PGUP，PGDN
// 小寫英文 = 單按某顆英文鍵

// 修改記錄：
// 2020/01/15：自行添加顯示 New 標籤提示



/// <summary> 複製物件，且對後綴編號做不同處理 </summary>
public class Extension_CreateEmptyObject {


    /// <summary> 用來判斷是否使用過「名稱不變的複製功能」 </summary>
    static bool guiSelectedNew = false;
    /// <summary> 用來追蹤用「名稱不變的複製功能」複製出來物件 </summary>
    static List<int> guiSelectedList = new List<int>();
    /// <summary> 對「名稱不變的複製功能」複製出來物件的標籤風格 </summary>
    static GUIStyle guiSelectedStyle = new GUIStyle();
    


    static Extension_CreateEmptyObject() {

        //設置標籤風格
        guiSelectedStyle.normal.textColor = Color.green;

        // 由於若使用名稱不變的複製功能，
        // 我們會搞不清楚，哪一個是原本的物件，哪一個是複製的物件，
        // 這裡針對複製出來的同名稱物件，在它們的欄位右方後，動態加 New 的標籤文字
        // EditorApplication.hierarchyWindowItemOnGUI 好像是 Update類型的函式，且會歷遍所有 Hierarchy的物件，
        // 所以這裡也只在使用過「名稱不變的複製功能」時，才去處理顯示 New 標籤文字
        Selection.selectionChanged += () => {

            //有使用過「名稱不變的複製功能」- 添加標籤
            if (guiSelectedNew) {
                guiSelectedNew = false;
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
                return;
            }

            //沒使用過「名稱不變的複製功能」- 清除標籤
            if (guiSelectedList.Count > 0) {
                guiSelectedList.Clear();
                EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
            }
        };
    }


    /// <summary> 繪製 New 標籤 </summary>
    /// <param name="instanceID"   ></param>
    /// <param name="selectionRect"></param>
    private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect) {
        GameObject tGameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (guiSelectedList.Contains(instanceID)) {
            Rect r = new Rect(selectionRect);
            r.x = r.width - 20;
            r.width = 40;
            GUI.Label(r, "New", guiSelectedStyle);
        }
    }




    /// <summary> 【 Ctrl + Shift + D 】複製物件至原物件下方，且名稱不變 </summary>
    [MenuItem("GameObject/擴展集/複製物件 (省略後綴) %#d", false, -1)]
    static void CreateEmptyObjec_NoSerialNumber() {

        guiSelectedList.Clear();
        List<int> newSelectList = new List<int>();
        foreach (Object obj in Selection.objects) {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path == string.Empty) {
                GameObject tOrigin = obj as GameObject;
                GameObject tCopy = GameObject.Instantiate(tOrigin, tOrigin.transform.parent);
                tCopy.name = obj.name;
                tCopy.transform.SetSiblingIndex(tOrigin.transform.GetSiblingIndex() + 1);
                newSelectList.Add(tCopy.GetInstanceID());
                guiSelectedList.Add(tCopy.GetInstanceID());
                Undo.RegisterCreatedObjectUndo(tCopy, "DeplicateEx");
            }
            else {
                string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CopyAsset(path, newPath);
            }
        }
        if (newSelectList != null) {
            if (newSelectList.Count == 0) {
                return;
            }
            Selection.instanceIDs = newSelectList.ToArray();
            guiSelectedNew = true;
            newSelectList.Clear();
        }
    }


    /// <summary>【 Ctrl + Alt + D 】複製物件且名稱保留編號，但物件會在原物件下方，而不會像預設的複製把複製出來的物件移到最底下 </summary>
    [MenuItem("GameObject/擴展集/複製物件 (複製於原物件下方) %&d", false, -1)]
    static void CreateEmptyObjec_NotAtBottom() {
        int index = 0;                                    //記錄相同名稱物件的編號
        string tmpName = "";                              //記錄複製物件的名稱
        GameObject tmpObj = null;                         //記錄複製出來的物件
        GameObject goLastObj = null;                      //記錄含有相同名稱的物件中編號最大的那個
        List<GameObject> goList = new List<GameObject>(); //記錄含有相同名稱的物件有哪些

        List<int> newSelectList = new List<int>();
        foreach (Object obj in Selection.objects) {
            string path = AssetDatabase.GetAssetPath(obj);

            //複製物件
            if (path == string.Empty) {
                GameObject gameObject = obj as GameObject;
                tmpObj = GameObject.Instantiate(gameObject, gameObject.transform.parent);

                //記錄複製物件的名稱
                try {
                    //去編號
                    string[] tmp = obj.name.Split('(');
                    tmpName = tmp[0];
                }
                catch {
                    tmpName = obj.name;
                }

                //找尋場景含有相同名稱的物件
                goList.Clear();
                foreach (GameObject go in GameObject.FindObjectsOfType(typeof(GameObject))) {
                    if (go.name.Contains(tmpName)) {
                        goList.Add(go);
                    }
                }

                //分析含有相同名稱的物件中，編號最大的數值是多少
                for (int i = 0; i < goList.Count; i++) {
                    if (goList[i].name.Contains(")")) {
                        try {
                            string[] tmp = goList[i].name.Split('(')[1].Split(')');
                            int tmpIndex = int.Parse(tmp[0]);
                            if (index <= tmpIndex) {
                                index = tmpIndex;
                                goLastObj = goList[i];
                            }
                        }
                        catch {
                            index = 0;
                        }
                    }
                }


                //改名，給物件名稱加上括弧編號
                tmpObj.name = tmpName + " (" + (index + 1) + ")";

                //如果是從名稱有編號的物件去做複製，編號前的空格會變成兩個，所以這裡把兩個空格變成一個空格
                tmpObj.name = tmpObj.name.Replace("  ", " ");


                //排序
                if (goLastObj != null) {
                    tmpObj.transform.SetSiblingIndex(goLastObj.transform.GetSiblingIndex() + 1);
                }
                else {
                    tmpObj.transform.SetSiblingIndex(gameObject.transform.GetSiblingIndex() + 1);
                }
                newSelectList.Add(tmpObj.GetInstanceID());

                //記錄還原項目
                Undo.RegisterCreatedObjectUndo(tmpObj, "DeplicateEx");
            }

            //複製檔案
            else {
                string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CopyAsset(path, newPath);
            }
        }
        if (newSelectList != null) {
            if (newSelectList.Count == 0) {
                return;
            }
            Selection.instanceIDs = newSelectList.ToArray();
            newSelectList.Clear();
        }
    }


}
