using UnityEngine;

public class VolumeFader : MonoBehaviour
{
    public AudioSource obj;
    float duration;
    float sourceVolume;
    float destVolume;
    float time = 0;
    EasingFunction.Function function;


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
        time += Time.deltaTime / duration;

        obj.volume = function(sourceVolume, destVolume, time);
        if (time >= 1)
        {
            obj.volume = destVolume;
            Destroy(gameObject);
        }
    }


    public void set(AudioSource o, float dest, float dur)
    {
        obj = o;
        sourceVolume = obj.volume;
        destVolume = dest;
        duration = dur;

        if (dur == 0)
        {
            o.volume = dest;
            Destroy(gameObject);
        }
    }


}
