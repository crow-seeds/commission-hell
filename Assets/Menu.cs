using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    // Start is called before the first frame update
    void Start()
    {
        input.placeholder.GetComponent<TextMeshProUGUI>().text = PlayerPrefs.GetString("artistName", "Francine");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void loadGame()
    {
        if (input.text != "")
        {
            PlayerPrefs.SetString("artistName", input.text);
        }
        PlayerPrefs.SetString("mode", "normal");
        SceneManager.LoadScene("Draw");

    }

    public void loadGameZen()
    {
        if (input.text != "")
        {
            PlayerPrefs.SetString("artistName", input.text);
        }
        PlayerPrefs.SetString("mode", "zen");

        SceneManager.LoadScene("Draw");
    }

}
