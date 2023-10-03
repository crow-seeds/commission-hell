using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AlphaFader : MonoBehaviour
{
    RawImage obj;
    float duration;
    float sourceAlpha;
    float destAlpha;
    float time = 0;
    EasingFunction.Function function;
    bool isText = false;
    TextMeshProUGUI textObj;
    bool isBeingDestroyed = false;

    // Start is called before the first frame update
    void Start()
    {
        EasingFunction.Ease movement = EasingFunction.Ease.EaseOutBack;
        function = EasingFunction.GetEasingFunction(movement);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isBeingDestroyed)
        {
            time += Time.deltaTime / duration;

            if (!isText)
            {
                obj.color = new Color(obj.color.r, obj.color.g, obj.color.b, function(sourceAlpha, destAlpha, time));
                if (time >= 1)
                {
                    obj.color = new Color(obj.color.r, obj.color.g, obj.color.b, destAlpha);
                    Destroy(gameObject);
                }
            }
            else
            {
                textObj.color = new Color(textObj.color.r, textObj.color.g, textObj.color.b, function(sourceAlpha, destAlpha, time));
                if (time >= 1)
                {
                    textObj.color = new Color(textObj.color.r, textObj.color.g, textObj.color.b, destAlpha);
                    Destroy(gameObject);
                }
            }
        }    
    }

    public void set(RawImage o, float dest, float dur)
    {
        obj = o;
        sourceAlpha = o.color.a;
        destAlpha = dest;
        duration = dur;

        if (dur == 0)
        {
            o.color = new Color(o.color.r, o.color.g, o.color.b, dest);
            Destroy(gameObject);
        }
    }

    public void set(TextMeshProUGUI o, float dest, float dur)
    {
        textObj = o;
        isText = true;
        sourceAlpha = o.color.a;
        destAlpha = dest;
        duration = dur;

        if (dur == 0)
        {
            textObj.color = new Color(o.color.r, o.color.g, o.color.b, dest);
            Destroy(gameObject);
        }
    }

    public void restart()
    {
        isBeingDestroyed = true;
        if (!isText)
        {
            obj.color = new Color(obj.color.r, obj.color.g, obj.color.b, destAlpha);
        }
        else
        {
            textObj.color = new Color(textObj.color.r, textObj.color.g, textObj.color.b, destAlpha);
        }
        Destroy(gameObject);
    }
}
