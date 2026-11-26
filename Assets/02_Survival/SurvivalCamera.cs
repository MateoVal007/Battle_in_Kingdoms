using UnityEngine;

namespace Kingdoms.Survival
{
    [RequireComponent(typeof(Camera))]
    public sealed class SurvivalCamera : MonoBehaviour
    {
        public SurvivalArena arena;
        public float minimumDistance=32,maximumDistance=120,padding=2;
        public float fittedDistance;
        Camera view;
        Vector3 focus;
        bool initialized;
        readonly Vector3 offset=new Vector3(0,.38f,-.925f).normalized;
        void Awake(){view=GetComponent<Camera>();}
        void LateUpdate(){Fit(Time.deltaTime);}
        public void Fit(float dt,bool immediate=false)
        {
            if(!view)view=GetComponent<Camera>();
            if(!arena||arena.heroes==null||arena.heroes.Length!=2)return;
            var a=arena.heroes[0].transform.position;var b=arena.heroes[1].transform.position;
            var center=(a+b)*.5f+Vector3.up;
            focus=immediate||!initialized?center:Vector3.Lerp(focus,center,1-Mathf.Exp(-6*dt));initialized=true;
            var rotation=Quaternion.LookRotation(-offset,Vector3.up);
            Vector3 right=rotation*Vector3.right,up=rotation*Vector3.up,forward=-offset;
            float tangent=Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad),desired=minimumDistance;
            foreach(var p in new[]{a,b,a+Vector3.up*2,b+Vector3.up*2})
            {
                var d=p-focus;float depth=Vector3.Dot(d,forward);
                desired=Mathf.Max(desired,(Mathf.Abs(Vector3.Dot(d,right))+padding)/(tangent*view.aspect*.90f)-depth,(Mathf.Abs(Vector3.Dot(d,up))+padding)/(tangent*.62f)-depth);
            }
            desired=Mathf.Clamp(desired,minimumDistance,maximumDistance);
            // Zoom out immediately enough to preserve the margin, ease back in when reunited.
            fittedDistance=immediate||desired>fittedDistance||fittedDistance<1?desired:Mathf.Lerp(fittedDistance,desired,1-Mathf.Exp(-2*dt));
            transform.position=focus+offset*fittedDistance;
            transform.rotation=rotation;
        }
    }
}
