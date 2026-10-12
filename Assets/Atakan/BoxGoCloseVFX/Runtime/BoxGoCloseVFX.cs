using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace BoxGo.VFX
{
    // Instantiate this prefab OUTSIDE the box hierarchy, then call Play(visual).
    public sealed class BoxGoCloseVFX : MonoBehaviour
    {
        public Material dotMaterial, starMaterial, ringMaterial;
        [Tooltip("Only the visual child. Do not assign a Rigidbody/gameplay root.")]
        public Transform boxVisual;
        [Min(0.01f)] public float boxWidth = 1f;
        [Min(0.2f)] public float duration = 0.8f;
        public Vector3 surfaceNormal = Vector3.up;
        public Vector3 exitDirection = Vector3.up;
        public float exitDistance = 1.1f;
        public float ringHeightOffset = 0.04f;
        public bool animateVisual = true;
        public bool hideVisualAtEnd = true;
        public bool destroyEffectAtEnd = true;
        public bool useUnscaledTime = false;
        public Color gold = new Color(1f, 0.78f, 0.32f, 1f);
        [Tooltip("Connect your existing box-lid closing animation here.")]
        public UnityEvent onCloseStarted = new UnityEvent();
        public UnityEvent onCompleted = new UnityEvent();
        ParticleSystem dots, stars;
        LineRenderer ring;
        Coroutine routine;
        Transform moving;
        Vector3 initialLocalPosition, initialLocalScale;
        bool captured;
        public bool IsPlaying => routine != null;

        void Awake() { Build(); }
        void Build()
        {
            if (dots != null) return;
            dots = MakeParticles("Soft bubbles", dotMaterial);
            stars = MakeParticles("Four point sparkles", starMaterial);
            var go = new GameObject("Expanding ring"); go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.sharedMaterial = ringMaterial; ring.useWorldSpace = true;
            ring.loop = true; ring.positionCount = 64; ring.numCornerVertices = 2;
            ring.enabled = false;
        }
        ParticleSystem MakeParticles(string label, Material material)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main; m.loop = false; m.playOnAwake = false;
            m.duration = 2; m.startSpeed = 0; m.gravityModifier = 0;
            m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 64;
            m.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[] {new GradientAlphaKey(0,0),new GradientAlphaKey(1,0.08f),new GradientAlphaKey(0,1)});
            color.color = gradient;
            var size = ps.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0,1,1,0.15f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
        public void Play(Transform visual) { boxVisual = visual; Play(); }
        [ContextMenu("Play (during Play Mode)")]
        public void Play()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || IsPlaying) return;
            Build();
            if (!dotMaterial || !starMaterial || !ringMaterial) {
                Debug.LogError("BoxGo: use Tools > Box Go > Create Close VFX Prefab first.",this); return;
            }
            if (boxVisual && (transform == boxVisual || transform.IsChildOf(boxVisual))) {
                Debug.LogError("BoxGo: effect must be outside the animated box hierarchy.",this); return;
            }
            routine = StartCoroutine(Sequence());
        }
        IEnumerator Sequence()
        {
            moving = boxVisual;
            captured = moving && animateVisual;
            if(captured) { initialLocalPosition=moving.localPosition; initialLocalScale=moving.localScale; }
            Vector3 origin = moving ? moving.position : transform.position;
            Vector3 normal = surfaceNormal.sqrMagnitude > 0.001f ? surfaceNormal.normalized : Vector3.up;
            Vector3 exit = exitDirection.sqrMagnitude > 0.001f ? exitDirection.normalized : normal;
            Vector3 axis = Vector3.Cross(normal, Mathf.Abs(Vector3.Dot(normal,Vector3.right))<0.9f ? Vector3.right : Vector3.forward).normalized;
            Vector3 side = Vector3.Cross(normal,axis).normalized;
            float w=Mathf.Max(0.01f,boxWidth), seconds=Mathf.Max(0.2f,duration);
            var dm=dots.main; dm.useUnscaledTime=useUnscaledTime;
            var sm=stars.main; sm.useUnscaledTime=useUnscaledTime;
            dots.Clear(); stars.Clear(); dots.Play(); stars.Play();
            onCloseStarted.Invoke();
            for(int i=0;i<4;i++) {
                float a=(45+i*90)*Mathf.Deg2Rad;
                Emit(stars,origin+(axis*Mathf.Cos(a)+side*Mathf.Sin(a))*w*0.6f,normal*w*0.08f,w*0.22f,seconds*0.45f,gold);
            }
            bool burst=false; int trailIndex=0;
            float elapsed=0;
            while(elapsed<seconds)
            {
                float t=elapsed/seconds;
                float travel=Mathf.Clamp01((t-0.36f)/0.64f);
                Vector3 visualPosition=origin+exit*(exitDistance*w*travel*travel);
                if(captured && moving) {
                    float scale=t<0.36f ? 1+0.12f*Mathf.Sin(t/0.36f*Mathf.PI) : Mathf.Lerp(1,0.04f,travel*travel);
                    moving.position=visualPosition;
                    moving.localScale=initialLocalScale*scale;
                }
                if(t>=0.22f && !burst) {
                    burst=true;
                    for(int i=0;i<12;i++) {
                        float a=i*Mathf.PI*2/12; Vector3 radial=axis*Mathf.Cos(a)+side*Mathf.Sin(a);
                        Emit(i%3==0?stars:dots,origin+radial*w*0.52f,radial*w*0.45f+normal*w*0.1f,
                             w*(i%3==0?0.22f:0.13f),seconds*0.6f,gold);
                    }
                }
                if(t>=0.22f) {
                    float r=Mathf.Clamp01((t-0.22f)/0.65f);
                    ring.enabled=r<1; ring.widthMultiplier=w*0.022f*(1-r);
                    Color c=gold; c.a=(1-r)*0.85f; ring.startColor=c; ring.endColor=c;
                    for(int i=0;i<64;i++) {
                        float a=i*Mathf.PI*2/64;
                        ring.SetPosition(i,origin+normal*(w*ringHeightOffset)+(axis*Mathf.Cos(a)+side*Mathf.Sin(a))*w*Mathf.Lerp(0.48f,0.94f,r));
                    }
                }
                // Fixed threshold count: identical particle budget at low and high frame rates.
                while(trailIndex<9 && t>=0.40f+trailIndex*0.055f) {
                    float u=(0.40f+trailIndex*0.055f-0.36f)/0.64f;
                    Vector3 p=origin+exit*(exitDistance*w*u*u)+side*(Mathf.Sin(trailIndex*2.1f)*w*0.07f);
                    Emit(trailIndex%3==0?stars:dots,p,-exit*w*0.06f,w*0.14f,seconds*0.45f,gold);
                    trailIndex++;
                }
                elapsed+=useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime;
                yield return null;
            }
            ring.enabled=false;
            if(moving && captured && hideVisualAtEnd) moving.gameObject.SetActive(false);
            Restore();
            onCompleted.Invoke();
            float tail=0;
            while(tail<seconds*0.5f) { tail+=useUnscaledTime?Time.unscaledDeltaTime:Time.deltaTime; yield return null; }
            routine=null;
            if(destroyEffectAtEnd) Destroy(gameObject);
        }
        void Emit(ParticleSystem ps,Vector3 position,Vector3 velocity,float size,float life,Color color) {
            ps.Emit(new ParticleSystem.EmitParams { position=position,velocity=velocity,startSize=size,startLifetime=life,startColor=color },1);
        }
        void Restore() {
            if(captured && moving) { moving.localPosition=initialLocalPosition; moving.localScale=initialLocalScale; }
            captured=false;
        }
        void OnDisable() {
            if(routine!=null) StopCoroutine(routine);
            routine=null; Restore();
            if(ring) ring.enabled=false;
            if(dots) dots.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(stars) stars.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
