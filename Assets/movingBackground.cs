using UnityEngine;

public class movingBackground : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    [SerializeField] float speed;
    Vector3 ratio = new Vector3(-16, -9, 0);
    // Update is called once per frame
    void Update()
    {
        GetComponent<RectTransform>().localPosition = GetComponent<RectTransform>().localPosition + ratio * speed * Time.deltaTime;

        if (GetComponent<RectTransform>().localPosition.x <= -1600)
        {
            GetComponent<RectTransform>().localPosition = new Vector2(0, 0);
        }

    }
}
