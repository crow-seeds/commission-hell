using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class Results : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    List<int> ids = new List<int>();

    [SerializeField] List<TextMeshProUGUI> authors = new List<TextMeshProUGUI>();
    [SerializeField] List<TextMeshProUGUI> votes = new List<TextMeshProUGUI>();
    [SerializeField] List<TextMeshProUGUI> upvotes = new List<TextMeshProUGUI>();
    [SerializeField] List<TextMeshProUGUI> downvotes = new List<TextMeshProUGUI>();
    [SerializeField] List<RawImage> images = new List<RawImage>();

    [SerializeField] RawImage yourImage;

    public void setUpResultsPage(int lvl, Texture yourImg, int conditions_met)
    {
        if (yourImg != null)
        {
            yourImage.gameObject.SetActive(true);
            yourImage.texture = yourImg;
            yourImage.rectTransform.sizeDelta = new Vector2(yourImage.texture.width, yourImage.texture.height);
        }
        else
        {
            yourImage.gameObject.SetActive(false);
        }

        StopAllCoroutines();
        StartCoroutine(getFromDatabaseForResults(lvl, conditions_met));
    }

    void clearAll()
    {
        ids.Clear();
        for (int i = 0; i < 4; i++)
        {
            authors[i].gameObject.SetActive(false);
            votes[i].gameObject.SetActive(false);
            images[i].gameObject.SetActive(false);
            upvotes[i].gameObject.SetActive(false);
            downvotes[i].gameObject.SetActive(false);
            images[i].texture = null;
            upvotes[i].color = Color.white;
            downvotes[i].color = Color.white;
        }
    }

    public IEnumerator getFromDatabaseForResults(int lvl, int conditions)
    {
        WWWForm form = new WWWForm();
        form.AddField("sortMode", "random");
        form.AddField("time", "all");
        form.AddField("level", lvl);
        form.AddField("page", 0);
        form.AddField("conditions_met", conditions);
        clearAll();

        using (UnityWebRequest www = UnityWebRequest.Post("https://crowseeds.com/LIMITEDSPACE/receive.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
                Debug.Log(www.result);
            }
            else
            {
                string[] results = www.downloadHandler.text.Split('\t');
                //Debug.Log(www.downloadHandler.text);

                for (int i = 0; i < results.Length - 1; i += 5)
                {
                    ids.Add(int.Parse(results[i]));

                    authors[i / 4].gameObject.SetActive(true);
                    votes[i / 4].gameObject.SetActive(true);
                    upvotes[i / 4].gameObject.SetActive(true);
                    downvotes[i / 4].gameObject.SetActive(true);

                    StartCoroutine(GetTexture(int.Parse(results[i]), i / 4));
                    authors[i / 4].text = "Drawn by: " + results[i + 1];
                    votes[i / 4].text = results[i + 3];

                    if (PlayerPrefs.GetString(results[i], "none") == "upvoted")
                    {
                        upvotes[i / 4].color = new Color(.25f, .25f, .25f);
                    }
                    else if (PlayerPrefs.GetString(results[i], "none") == "downvoted")
                    {
                        downvotes[i / 4].color = new Color(.25f, .25f, .25f);
                    }

                }
            }
        }
    }

    IEnumerator GetTexture(int id, int index)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture("https://crowseeds.com/limitedspace/images/" + id.ToString() + ".png");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            images[index].gameObject.SetActive(true);
            images[index].texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            images[index].rectTransform.sizeDelta = new Vector2(images[index].texture.width / 2, images[index].texture.height / 2);
        }
    }

    public void like(int i)
    {
        upvotes[i].color = Color.white;
        downvotes[i].color = Color.white;


        if (PlayerPrefs.GetString(ids[i].ToString(), "none") != "upvoted")
        {
            if (PlayerPrefs.GetString(ids[i].ToString(), "none") == "downvoted")
            {
                StartCoroutine(vote("upvote", ids[i], i));
            }
            PlayerPrefs.SetString(ids[i].ToString(), "upvoted");
            upvotes[i].color = new Color(.25f, .25f, .25f);
            StartCoroutine(vote("upvote", ids[i], i));
        }
        else
        {
            PlayerPrefs.SetString(ids[i].ToString(), "none");
            upvotes[i].color = Color.white;
            StartCoroutine(vote("downvote", ids[i], i));
        }
    }

    public void dislike(int i)
    {
        upvotes[i].color = Color.white;
        downvotes[i].color = Color.white;

        if (PlayerPrefs.GetString(ids[i].ToString(), "none") != "downvoted")
        {
            if (PlayerPrefs.GetString(ids[i].ToString(), "none") == "upvoted")
            {
                StartCoroutine(vote("downvote", ids[i], i));
            }
            PlayerPrefs.SetString(ids[i].ToString(), "downvoted");
            downvotes[i].color = new Color(.25f, .25f, .25f);
            StartCoroutine(vote("downvote", ids[i], i));
        }
        else
        {
            PlayerPrefs.SetString(ids[i].ToString(), "none");
            downvotes[i].color = Color.white;
            StartCoroutine(vote("upvote", ids[i], i));
        }
    }

    IEnumerator vote(string mode, int id, int voteIndex)
    {
        WWWForm form = new WWWForm();
        form.AddField("type", mode);
        form.AddField("id", id);

        using (UnityWebRequest www = UnityWebRequest.Post("https://crowseeds.com/LIMITEDSPACE/vote.php", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                votes[voteIndex].text = www.downloadHandler.text;
            }
        }
    }

    [SerializeField] GameObject fullscreenedObject;
    [SerializeField] RawImage fullscreenedImage;
    [SerializeField] TextMeshProUGUI fullscreenAuthor;

    public void clickOnDrawing(int i)
    {
        if (i < ids.Count)
        {
            fullscreenedObject.SetActive(true);
            fullscreenedImage.rectTransform.sizeDelta = new Vector2(images[i].texture.width, images[i].texture.height);
            fullscreenedImage.texture = images[i].texture;
            fullscreenAuthor.text = authors[i].text;
        }
    }

    public void leaveFullscreen()
    {
        fullscreenedObject.SetActive(false);
    }
}
