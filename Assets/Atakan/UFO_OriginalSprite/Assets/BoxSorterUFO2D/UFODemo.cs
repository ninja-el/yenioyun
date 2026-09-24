using UnityEngine;
namespace BoxSorterUFO2D
{
 public sealed class UFODemo:MonoBehaviour
 {
  public UFOBooster booster;public Transform[] targets;public Renderer box;public Material completedMaterial;
  Material original;Vector3[] positions;Quaternion[] rotations;Vector3[] scales;
  void Start(){original=box.sharedMaterial;positions=new Vector3[3];rotations=new Quaternion[3];scales=new Vector3[3];for(int i=0;i<3;i++){positions[i]=targets[i].position;rotations[i]=targets[i].rotation;scales[i]=targets[i].localScale;}booster.onThreeCollected.AddListener(Complete);}
  void Complete(){if(box)box.sharedMaterial=completedMaterial;}
  void OnGUI(){if(GUI.Button(new Rect(20,20,200,55),"PLAY UFO / REPLAY")&&!booster.IsPlaying){for(int i=0;i<3;i++){targets[i].position=positions[i];targets[i].rotation=rotations[i];targets[i].localScale=scales[i];targets[i].gameObject.SetActive(true);}box.sharedMaterial=original;booster.Play(targets,Vector3.zero);}GUI.Label(new Rect(20,80,280,35),"3 objects collected = box complete");}
 }
}
