using UnityEngine;
using UnityEditor;
using BoxGo.VFX;

public static class BoxGoCloseVFXInstaller
{
    [MenuItem("Tools/Box Go/Create Close VFX Prefab")]
    public static void Create()
    {
        const string root="Assets/BoxGoCloseVFX";
        if(!AssetDatabase.IsValidFolder(root+"/Generated")) AssetDatabase.CreateFolder(root,"Generated");
        string folder=AssetDatabase.GenerateUniqueAssetPath(root+"/Generated/CloseVFX");
        AssetDatabase.CreateFolder(root+"/Generated",System.IO.Path.GetFileName(folder));
        var shader=Shader.Find("BoxGo/SoftParticle");
        if(!shader) { Debug.LogError("BoxGo/SoftParticle shader missing. Import the complete folder."); return; }
        var go=new GameObject("BoxGo_CloseVFX");
        try {
            var fx=go.AddComponent<BoxGoCloseVFX>();
            fx.dotMaterial=Material(shader,folder,"Bubble",0);
            fx.starMaterial=Material(shader,folder,"Star",1);
            fx.ringMaterial=Material(shader,folder,"Ring",2);
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,folder+"/BoxGo_CloseVFX.prefab");
            AssetDatabase.SaveAssets(); Selection.activeObject=prefab; EditorGUIUtility.PingObject(prefab);
            Debug.Log("BoxGo Close VFX ready: "+folder+"/BoxGo_CloseVFX.prefab");
        } finally { Object.DestroyImmediate(go); }
    }
    static Material Material(Shader shader,string folder,string label,float shape)
    {
        var m=new Material(shader); m.name=label; m.SetFloat("_Shape",shape);
        AssetDatabase.CreateAsset(m,folder+"/"+label+".mat"); return m;
    }
}
