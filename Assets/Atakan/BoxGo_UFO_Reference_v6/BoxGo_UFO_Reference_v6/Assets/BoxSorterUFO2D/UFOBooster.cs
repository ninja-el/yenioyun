using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BoxSorterUFO2D
{
    // Owns visuals only. The game owns selection, reservation, scoring and pooling.
    public sealed class UFOBooster : MonoBehaviour
    {
        public Transform model;
        public Material beamMaterial;
        public float hoverHeight = 3f;
        public float saucerScale = .85f;
        public float enterDuration = 1.35f, pullDuration = .45f, stagger = .24f, exitDuration = .65f;
        public UnityEvent onThreeCollected = new UnityEvent();
        public bool IsPlaying { get; private set; }
        public event Action<bool> Finished; // true only after three successful captures
        sealed class Saved
        {
            public Transform t; public Vector3 p,s; public Quaternion r; public bool active;
            public Collider[] colliders; public bool[] enabled; public Rigidbody[] bodies; public bool[] kinematic;
        }
        readonly List<Saved> saved = new List<Saved>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Transform beam; LineRenderer[] rings; bool committed;
        Vector3 origin;
        public Renderer originalSprite;
        public Camera effectCamera;
        public float introScaleMultiplier=2.6f;
        [Range(0,2)] public int entryTurns=0;
        [Range(0,1)] public float flashStrength=.28f;
        public float flashDuration=.18f;
        public Material flashMaterial;
        Material liveFlash;
        GameObject flashQuad;
        float visualRoll;
        void Flash(float elapsed)
        {
            var cam=effectCamera?effectCamera:Camera.main;
            if(!cam || !flashMaterial)return;
            if(!flashQuad){
                flashQuad=GameObject.CreatePrimitive(PrimitiveType.Quad);
                flashQuad.name="UFO Short Screen Dim";
                Destroy(flashQuad.GetComponent<Collider>());
                liveFlash=new Material(flashMaterial);
                flashQuad.GetComponent<Renderer>().sharedMaterial=liveFlash;
            }
            float d=cam.nearClipPlane+.05f;
            flashQuad.transform.SetParent(cam.transform,false);
            flashQuad.transform.localPosition=new Vector3(0,0,d);
            flashQuad.transform.localRotation=Quaternion.identity;
            float h=cam.orthographic?cam.orthographicSize*2:2*d*Mathf.Tan(cam.fieldOfView*Mathf.Deg2Rad*.5f);
            flashQuad.transform.localScale=new Vector3(h*cam.aspect*1.02f,h*1.02f,1);
            float alpha=flashStrength*Ease(elapsed/Mathf.Max(.05f,flashDuration));
            liveFlash.SetColor("_Color",new Color(0,0,0,alpha));
            flashQuad.SetActive(alpha>.001f);
        }
        void HideFlash(){if(flashQuad)flashQuad.SetActive(false);}
        TrailRenderer flightTrail;
        MaterialPropertyBlock properties;
        int collected;
        void FindLamps() { }
        void LightLamp(int index,bool on)
        {
            if(on)collected=Mathf.Max(collected,index+1);
            ApplyLights();
        }
        // Unity native objects are allocated from lifecycle callbacks, not field initializers.
        void EnsureProperties()
        {
            if(properties == null) properties = new MaterialPropertyBlock();
        }
        bool ResolveSprite()
        {
            if(originalSprite && originalSprite.sharedMaterial && originalSprite.sharedMaterial.HasProperty("_Collected")) return true;
            if(model)
            {
                foreach(var candidate in model.GetComponentsInChildren<Renderer>(true))
                {
                    var material = candidate.sharedMaterial;
                    if(material && material.HasProperty("_Collected")) { originalSprite=candidate;return true; }
                }
            }
            return false;
        }
        void ApplyLights()
        {
            if(!originalSprite) return;
            EnsureProperties();
            originalSprite.GetPropertyBlock(properties);
            properties.SetFloat("_Collected",collected);
            originalSprite.SetPropertyBlock(properties);
        }
        void ResetLamps(){collected=0;ApplyLights();}
        void LateUpdate(){var c=effectCamera?effectCamera:Camera.main;if(model && c)model.rotation=c.transform.rotation*Quaternion.Euler(0,0,visualRoll);}
        void SetTrail(bool enabled)
        {
            if(!flightTrail && model && beamMaterial) {
                var go=new GameObject("Soft Flight Ribbon");go.transform.SetParent(model,false);
                flightTrail=go.AddComponent<TrailRenderer>();flightTrail.sharedMaterial=beamMaterial;
                flightTrail.time=.24f;flightTrail.minVertexDistance=.025f;
                flightTrail.startWidth=.11f;flightTrail.endWidth=0;
                flightTrail.startColor=new Color(.55f,.3f,1,.55f);flightTrail.endColor=new Color(.2f,.85f,1,0);
            }
            if(flightTrail){flightTrail.emitting=enabled;if(enabled)flightTrail.Clear();}
        }

        void Awake() { EnsureProperties(); BuildFX(); if(model) model.gameObject.SetActive(false); }
        void BuildFX()
        {
            if(beam || !beamMaterial) return;
            var go=new GameObject("TractorBeam"); go.transform.SetParent(transform,false); beam=go.transform;
            var vs=new Vector3[66];var ts=new int[192];var cs=new Color[66];
            for(int i=0;i<=32;i++) { float a=i*Mathf.PI*2/32; vs[i*2]=new Vector3(Mathf.Cos(a)*.15f,1,Mathf.Sin(a)*.15f);vs[i*2+1]=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));cs[i*2]=new Color(1,1,1,.4f);cs[i*2+1]=new Color(1,1,1,.04f);if(i<32){int k=i*6,b=i*2;ts[k]=b;ts[k+1]=b+1;ts[k+2]=b+2;ts[k+3]=b+2;ts[k+4]=b+1;ts[k+5]=b+3;}}
            var mesh=new Mesh {name="UFO Beam"};mesh.vertices=vs;mesh.triangles=ts;mesh.colors=cs;mesh.RecalculateBounds();meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=beamMaterial;
            rings=new LineRenderer[5];
            for(int k=0;k<5;k++){var r=new GameObject("AscendingRing");r.transform.SetParent(transform,false);var lr=r.AddComponent<LineRenderer>();lr.sharedMaterial=beamMaterial;lr.useWorldSpace=true;lr.loop=true;lr.positionCount=48;lr.widthMultiplier=.022f;rings[k]=lr;}
            ShowFX(false);
        }
        void ShowFX(bool visible){if(beam)beam.gameObject.SetActive(visible);if(rings!=null)foreach(var r in rings)if(r)r.gameObject.SetActive(visible);}
        public bool Play(Transform[] targets, Vector3 boardCenter)
        {
            if(IsPlaying || !isActiveAndEnabled || !model || !beamMaterial || targets==null || targets.Length!=3) return false;
            for(int i=0;i<3;i++){if(!targets[i] || !targets[i].gameObject.activeInHierarchy || targets[i]==model || targets[i].IsChildOf(model) || model.IsChildOf(targets[i]))return false;for(int j=0;j<i;j++)if(targets[i]==targets[j] || targets[i].IsChildOf(targets[j]) || targets[j].IsChildOf(targets[i]))return false;}
            if(!ResolveSprite())
            {
                Debug.LogError("UFO 2.5D: Original Sprite renderer with the OriginalSprite material is missing. Assign it on UFOBooster or regenerate the demo.",this);
                return false;
            }
            // Validate and initialize lights before reserving or changing any target physics.
            EnsureProperties();ResetLamps();
            BuildFX();saved.Clear();committed=false;origin=boardCenter;
            foreach(var t in targets){var s=new Saved {t=t,p=t.position,r=t.rotation,s=t.localScale,active=t.gameObject.activeSelf,colliders=t.GetComponentsInChildren<Collider>(),bodies=t.GetComponentsInChildren<Rigidbody>()};s.enabled=new bool[s.colliders.Length];for(int i=0;i<s.colliders.Length;i++){s.enabled[i]=s.colliders[i].enabled;s.colliders[i].enabled=false;}s.kinematic=new bool[s.bodies.Length];for(int i=0;i<s.bodies.Length;i++){s.kinematic[i]=s.bodies[i].isKinematic;s.bodies[i].isKinematic=true;}saved.Add(s);}
            model.rotation=Quaternion.identity;FindLamps();ResetLamps();IsPlaying=true;StartCoroutine(Sequence());return true;
        }
        // Camera-relative route measured from the supplied reference clip.
        static readonly Vector2[] route = { new Vector2(-1.25f,-.75f),new Vector2(-.65f,-.25f),new Vector2(-.52f,1.45f),new Vector2(.65f,1.8f),new Vector2(1.3f,.75f),new Vector2(.6f,-.18f),new Vector2(0,0) };
        static Vector2 Route(float q){
            float k=Mathf.Clamp01(q)*(route.Length-1);int i=Mathf.Min(Mathf.FloorToInt(k),route.Length-2);float u=k-i;
            Vector2 a=route[Mathf.Max(0,i-1)],b=route[i],c=route[i+1],d=route[Mathf.Min(route.Length-1,i+2)];
            return .5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
        }
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        IEnumerator Sequence()
        {
            Vector3 hover=origin+Vector3.up*hoverHeight;
            Camera cam=effectCamera?effectCamera:Camera.main;
            Vector3 right=cam?cam.transform.right:Vector3.right, up=cam?cam.transform.up:Vector3.up;
            Vector2 first=Route(0);
            model.position=hover+right*first.x+up*first.y;
            model.localScale=Vector3.one*saucerScale*introScaleMultiplier;
            visualRoll=-12;model.gameObject.SetActive(true);SetTrail(true);Flash(0);
            for(float t=0;t<enterDuration;t+=Time.deltaTime){
                float q=Mathf.Clamp01(t/Mathf.Max(.01f,enterDuration));Vector2 path=Route(q);
                model.position=hover+right*path.x+up*path.y;
                model.localScale=Vector3.one*saucerScale*Mathf.Lerp(introScaleMultiplier,1,Ease(q/.65f));
                visualRoll=12*Mathf.Sin(q*Mathf.PI*2)-12*(1-q);Flash(t);
                yield return null;
            }
            model.localScale=Vector3.one*saucerScale;visualRoll=0;SetTrail(false);
            // Fade the held dim out just before tractor-beam capture starts.
            for(float f=0;f<.18f;f+=Time.deltaTime){Flash(Mathf.Max(0,flashDuration*(1-f/.18f)));yield return null;}
            HideFlash();
            model.position=hover;model.rotation=Quaternion.identity;FindLamps();ResetLamps();bool[] captured=new bool[3];ShowFX(true);float total=stagger*2+pullDuration;
            for(float t=0;t<total;t+=Time.deltaTime)
            {
                foreach(var s in saved)if(!s.t){Cancel();yield break;}
                model.position=hover+Vector3.up*(Mathf.Sin(t*5)*.035f);visualRoll=0;
                Vector3 intake=model.position-Vector3.up*(.86f*saucerScale);
                for(int i=0;i<3;i++) {var s=saved[i];float q=Mathf.Clamp01((t-i*stagger)/Mathf.Max(.01f,pullDuration));float e=Ease(q);s.t.position=Vector3.Lerp(s.p,intake,e)+new Vector3(Mathf.Sin(q*Mathf.PI*2),0,Mathf.Cos(q*Mathf.PI*2))*(Mathf.Sin(q*Mathf.PI)*.22f);s.t.rotation=s.r*Quaternion.Euler(q*25,q*220,q*15);s.t.localScale=s.s*Mathf.Pow(1-e,.6f);if(q>=1 && !captured[i]){s.t.gameObject.SetActive(false);captured[i]=true;LightLamp(i,true);}}
                UpdateFX(t,intake);yield return null;
            }
            foreach(var s in saved){if(!s.t){Cancel();yield break;}s.t.gameObject.SetActive(false);}
            for(int i=0;i<3;i++)LightLamp(i,true);
            // Restore transforms and physics while objects are inactive, so pooled objects are reusable.
            Restore(false);committed=true;ShowFX(false);onThreeCollected.Invoke();
            yield return new WaitForSeconds(.18f);
            SetTrail(true);
            for(float t=0;t<exitDuration;t+=Time.deltaTime){float q=Ease(t/Mathf.Max(.01f,exitDuration));model.position=hover+right*(-.55f*Mathf.Sin(q*Mathf.PI)+q*q*5)+up*(-.45f*Mathf.Sin(q*Mathf.PI)+q*.6f);model.localScale=Vector3.one*saucerScale*Mathf.Lerp(1,.2f,q);visualRoll=0;yield return null;}
            SetTrail(false);visualRoll=0;model.localScale=Vector3.one*saucerScale;
            model.gameObject.SetActive(false);saved.Clear();IsPlaying=false;Finished?.Invoke(true);
        }
        void UpdateFX(float t,Vector3 intake)
        {
            float height=Mathf.Max(.01f,intake.y-origin.y);beam.position=origin;beam.localScale=new Vector3(1.5f,height,1.5f);
            for(int k=0;k<rings.Length;k++){float q=Mathf.Repeat(t*.8f+k/5f,1);float radius=Mathf.Lerp(1.45f,.10f,q);var c=new Color(.2f,.9f,1,(1-q)*.8f);rings[k].startColor=c;rings[k].endColor=c;for(int j=0;j<48;j++){float a=j*Mathf.PI*2/48;rings[k].SetPosition(j,origin+new Vector3(Mathf.Cos(a)*radius,q*height,Mathf.Sin(a)*radius));}}
        }
        void Restore(bool reactivate)
        {
            foreach(var s in saved){if(!s.t)continue;s.t.position=s.p;s.t.rotation=s.r;s.t.localScale=s.s;for(int i=0;i<s.bodies.Length;i++)if(s.bodies[i])s.bodies[i].isKinematic=s.kinematic[i];for(int i=0;i<s.colliders.Length;i++)if(s.colliders[i])s.colliders[i].enabled=s.enabled[i];if(reactivate)s.t.gameObject.SetActive(s.active);}
        }
        public void Cancel(){if(!IsPlaying)return;StopAllCoroutines();if(!committed){Restore(true);ResetLamps();}saved.Clear();ShowFX(false);SetTrail(false);HideFlash();visualRoll=0;if(model)model.gameObject.SetActive(false);IsPlaying=false;Finished?.Invoke(committed);}
        void OnDisable(){Cancel();HideFlash();}
        void OnDestroy(){if(flashQuad)Destroy(flashQuad);if(liveFlash)Destroy(liveFlash);foreach(var m in meshes)if(m)Destroy(m);}
    }
}
