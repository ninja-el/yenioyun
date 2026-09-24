using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace BoxSorterUFO2D
{
 public static class UFOSetup
 {
  static string Root;

  // Resolve the package from its runtime script, independent of nesting or folder name.
  static bool ResolveRoot()
  {
   AssetDatabase.Refresh();
   var matches=new List<string>();
   foreach(string guid in AssetDatabase.FindAssets("UFOBooster t:MonoScript",new[]{"Assets"}))
   {
    string path=AssetDatabase.GUIDToAssetPath(guid);
    var script=AssetDatabase.LoadAssetAtPath<MonoScript>(path);
    if(script && script.GetClass()==typeof(UFOBooster))
    {
     string folder=Path.GetDirectoryName(path).Replace('\\','/')+"/";
     if(!matches.Contains(folder))matches.Add(folder);
    }
   }
   if(matches.Count!=1)
   {
    Debug.LogError("UFO 2.5D: Expected one BoxSorterUFO2D.UFOBooster script under Assets, found "+matches.Count+". Keep one package copy and resolve Console compilation errors.");
    return false;
   }
   Root=matches[0];
   string[] required={"Textures/UFO_Original.jpeg","Textures/UFO_SilhouetteMask.png","OriginalSprite.shader","UFOBeam.shader","UFOToon.shader"};
   foreach(string relative in required)
   {
    string path=Root+relative;
    if(!AssetDatabase.LoadMainAssetAtPath(path))
    {Debug.LogError("UFO 2.5D: Required asset missing: "+path+". Copy the entire package, retaining its Textures and Editor subfolders.");return false;}
   }
   foreach(string relative in new[]{"OriginalSprite.shader","UFOBeam.shader","UFOToon.shader"})
   {
    var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+relative);
    if(!shader || ShaderUtil.ShaderHasError(shader))
    {Debug.LogError("UFO 2.5D: Shader import/compilation error: "+Root+relative+". Inspect the shader errors in Console.");return false;}
   }
   return true;
  }

  static Material Mat(string name,Color color,string shader="BoxSorterUFO2D/Toon"){
   string path=Root+"Generated/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat){mat.color=color;EditorUtility.SetDirty(mat);return mat;}
   string shaderFile=shader.EndsWith("/Beam")?"UFOBeam.shader":"UFOToon.shader";
   var sh=AssetDatabase.LoadAssetAtPath<Shader>(Root+shaderFile);if(!sh)throw new System.Exception("Missing shader: "+shader);mat=new Material(sh){name=name};mat.color=color;AssetDatabase.CreateAsset(mat,path);return mat;
  }
  [MenuItem("Tools/Box Sorter UFO 2.5D/Create Demo and Prefab")]
  public static void Create(){
   if(!ResolveRoot())return;
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   string texturePath=Root+"Textures/UFO_Original.jpeg";
   var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
   if(!importer){Debug.LogError("UFO 2.5D: TextureImporter missing at "+texturePath);return;}
   importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
   string maskPath=Root+"Textures/UFO_SilhouetteMask.png";
   var maskImporter=AssetImporter.GetAtPath(maskPath) as TextureImporter;
   if(!maskImporter){Debug.LogError("UFO 2.5D: TextureImporter missing at "+maskPath);return;}
   maskImporter.sRGBTexture=false;maskImporter.textureCompression=TextureImporterCompression.Uncompressed;maskImporter.mipmapEnabled=false;maskImporter.maxTextureSize=2048;maskImporter.npotScale=TextureImporterNPOTScale.None;maskImporter.wrapMode=TextureWrapMode.Clamp;maskImporter.SaveAndReimport();
   if(!AssetDatabase.IsValidFolder(Root+"Generated"))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Generated");
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var go=new GameObject("UFO_OriginalSprite_Booster");var booster=go.AddComponent<UFOBooster>();var model=new GameObject("Original_UFO_Billboard");model.transform.SetParent(go.transform,false);
   string meshPath=Root+"Generated/OriginalQuad.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(!mesh){mesh=new Mesh{name="Original image quad"};float h=2.8f*700/985;mesh.vertices=new[]{new Vector3(-1.4f,-h/2,0),new Vector3(1.4f,-h/2,0),new Vector3(-1.4f,h/2,0),new Vector3(1.4f,h/2,0)};mesh.uv=new[]{new Vector2(110f/1206,1-1090f/1503),new Vector2(1095f/1206,1-1090f/1503),new Vector2(110f/1206,1-390f/1503),new Vector2(1095f/1206,1-390f/1503)};mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,meshPath);}
   model.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=model.AddComponent<MeshRenderer>();string matPath=Root+"Generated/OriginalSprite.mat";var spriteMat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!spriteMat){spriteMat=new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root+"OriginalSprite.shader"));AssetDatabase.CreateAsset(spriteMat,matPath);}spriteMat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);spriteMat.SetTexture("_MaskTex",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));spriteMat.SetFloat("_Collected",0);EditorUtility.SetDirty(spriteMat);renderer.sharedMaterial=spriteMat;
   booster.originalSprite=renderer;booster.model=model.transform;booster.beamMaterial=Mat("Beam",new Color(.08f,.75f,1,.6f),"BoxSorterUFO2D/Beam");model.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(go,Root+"Generated/UFOOriginalSprite.prefab");
   var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,6,-9);camera.transform.LookAt(new Vector3(0,1.2f,0));camera.orthographic=true;camera.orthographicSize=4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.07f,.10f);
   var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="DemoBoard";board.transform.position=new Vector3(0,-.3f,0);board.transform.localScale=new Vector3(5,.2f,4.5f);board.GetComponent<Renderer>().sharedMaterial=Mat("Board",new Color(.12f,.13f,.17f));
   var demo=new GameObject("DemoControls").AddComponent<UFODemo>();demo.booster=booster;demo.targets=new Transform[3];
   for(int i=0;i<3;i++){var apple=GameObject.CreatePrimitive(PrimitiveType.Sphere);apple.name="DemoApple_"+i;apple.transform.position=new Vector3((i-1)*1.1f,.18f,i==1?-.5f:.25f);apple.transform.localScale=Vector3.one*.6f;apple.GetComponent<Renderer>().sharedMaterial=Mat("Apple",new Color(.95f,.045f,.08f));demo.targets[i]=apple.transform;var leaf=GameObject.CreatePrimitive(PrimitiveType.Sphere);leaf.transform.SetParent(apple.transform,false);leaf.transform.localPosition=new Vector3(.13f,.55f,0);leaf.transform.localScale=new Vector3(.35f,.09f,.16f);leaf.GetComponent<Renderer>().sharedMaterial=Mat("Leaf",new Color(.3f,.75f,.07f));}
   var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name="DemoBox_CompletionIndicator";box.transform.position=new Vector3(0,0,-1.8f);box.transform.localScale=new Vector3(.8f,.5f,.6f);demo.box=box.GetComponent<Renderer>();demo.box.sharedMaterial=Mat("Cardboard",new Color(.68f,.34f,.10f));demo.completedMaterial=Mat("Completed",new Color(.25f,.9f,.45f));
   EditorSceneManager.SaveScene(scene,Root+"Generated/UFODemo.unity");AssetDatabase.SaveAssets();Selection.activeGameObject=go;
   Debug.Log("UFO 2.5D ready. Package: "+Root+" | Demo: "+Root+"Generated/UFODemo.unity | Press Play, then PLAY UFO / REPLAY.");
  }
 }
}
