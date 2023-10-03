using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mover : MonoBehaviour
{
    public RectTransform obj;
    public Transform obj2;
    float duration;
    Vector2 sourceLoc;
    Vector2 destLoc;
    float time = 0;
    EasingFunction.Function function;

    bool isRect = true;


    // Start is called before the first frame update
    void Start()
    {
        EasingFunction.Ease movement = EasingFunction.Ease.EaseOutBack;
        function = EasingFunction.GetEasingFunction(movement);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime / duration;

        if (isRect)
        {
            if(obj == null)
            {
                Destroy(gameObject);
                return;
            }

            obj.localPosition = new Vector3(function(sourceLoc.x, destLoc.x, time), function(sourceLoc.y, destLoc.y, time), obj.localPosition.z);
            if (time >= 1)
            {
                obj.localPosition = new Vector3(destLoc.x, destLoc.y, obj.localPosition.z);
                Destroy(gameObject);
            }
        }
        else
        {
            if (obj2 == null)
            {
                Destroy(gameObject);
                return;
            }

            obj2.localPosition = new Vector3(function(sourceLoc.x, destLoc.x, time), function(sourceLoc.y, destLoc.y, time), obj2.localPosition.z);
            if (time >= 1)
            {
                obj2.localPosition = new Vector3(destLoc.x, destLoc.y, obj2.localPosition.z);
                Destroy(gameObject);
            }
        }

        
    }

    public void set(RectTransform o, Vector2 dS, float dur, bool easeIn)
    {
        obj = o;
        sourceLoc = o.localPosition;
        destLoc = dS;
        isRect = true;
        duration = dur;

        if (easeIn)
        {
            function = EasingFunction.GetEasingFunction(EasingFunction.Ease.EaseInBack);
        }

        if(dur < .1f)
        {
            o.localPosition = new Vector3(dS.x, dS.y, obj.localPosition.z);
            Destroy(gameObject);
        }
        
    }

    public void set2(Transform o, Vector2 dS, float dur)
    {
        obj2 = o;
        sourceLoc = o.localPosition;
        destLoc = dS;

        duration = dur;
        isRect = false;

        if (dur < .1f)
        {
            o.localPosition = new Vector3(dS.x, dS.y, obj2.localPosition.z);
            Destroy(gameObject);
        }
    }
}
