using UnityEngine;

public class FollowDrawing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    [SerializeField] Transform mainCanvas;
    [SerializeField] RectTransform mainCanvasAgain;

    Vector2 border = new Vector2(20, 20);
    // Update is called once per frame
    void Update()
    {
        transform.localPosition = mainCanvas.localPosition;
        GetComponent<RectTransform>().sizeDelta = mainCanvasAgain.sizeDelta + border;
    }
}
