using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Color = UnityEngine.Color;

[RequireComponent(typeof(RawImage))]
public class drawing : MonoBehaviour
{
    RectTransform rt;
    RawImage ri;
    Vector3 bottomLeft = Vector3.zero;
    Vector3 topRight = Vector3.zero;
    Texture2D canvas;

    float boxHeight;
    float boxWidth;

    [SerializeField] int width = 640;
    [SerializeField] int height = 640;
    [SerializeField] int brushSize = 10;

    [SerializeField] List<Color> colorList;

    [SerializeField] Camera cam;
    [SerializeField] float musicVolume;
    [SerializeField] float dialogueVolume;

    bool hasLastPosition = false;
    Vector2 lastPosition;

    Color penColor = Color.black;
    Color oldPenColor = Color.black;
    string stampName = "stampTest";

    //[SerializeField] RawImage blackCircle;
    //[SerializeField] RawImage whiteCircle;

    //[SerializeField] GameObject circleIndicator;
    //[SerializeField] GameObject squareIndicator;

    //[SerializeField] List<GameObject> sizeIndicators = new List<GameObject>();
    List<int> sizeNums = new List<int> { 2, 6, 10, 20, 40 };
    int sizeIndex = 2;

    //[SerializeField] TextMeshProUGUI xSnapText;
    //[SerializeField] TextMeshProUGUI ySnapText;
    //[SerializeField] TMP_InputField authorText;

    //[SerializeField] TextMeshProUGUI statusText;
    //[SerializeField] TextMeshProUGUI bigCharacter;
    public bool xSnap = false;
    public bool ySnap = false;

    bool isSquare = false;
    int lastSubmittedID = -1;

    List<Color32[]> timeslices = new List<Color32[]>();
    List<string[]> timeslicesAttributes = new List<string[]>();
    public int timesliceIndex = 0;
    bool isSending = false;

    Color32[] oldTexture;
    string[] pixelAttributes;

    [SerializeField] bool stampMode = false;
    [SerializeField] RectTransform stampShadow;
    [SerializeField] RectTransform canvasObject;
    [SerializeField] int level;

    [SerializeField] AudioSource music;
    [SerializeField] AudioSource soundFx;
    [SerializeField] AudioSource pencilSounds;
    [SerializeField] AudioSource dialogue;
    [SerializeField] TextMeshProUGUI artistNameText;
    [SerializeField] TextMeshProUGUI drawingNameText;

    bool gameActive = false;
    float timer = 0;
    int mood = 0;

    float afkTime = 0;
    float holdTime = 0;
    bool thinking = false;
    bool zenMode = false;


    // Start is called before the first frame update
    void Start()
    {
        // Getting the RectTransform, since this is a RawImage, which exists on the canvas and should have a rect transform
        rt = GetComponent<RectTransform>();
        // RawImage that we are going to be updating for our paint application.
        ri = GetComponent<RawImage>();


        if (PlayerPrefs.GetString("mode", "normal") == "zen")
        {
            zenMode = true;
            nextPageButton.text = "Back to Main Menu";
            transform.localPosition = new Vector2(-186f, 41);
        }

        authorName = PlayerPrefs.GetString("artistName", "Francine");
        artistNameText.text = "Drawn By: " + authorName;

        if (ri != null)
        {
            startLevel();
            getCoords();
        }

        //characterDrawn = potentialCharacters[Random.Range(0, potentialCharacters.Count)];
        //character.text = characterDrawn;
        //bigCharacter.text = characterDrawn;
    }

    [SerializeField] TextMeshProUGUI timerText;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
        if (!thinking && !Input.anyKey)
        {
            afkTime += Time.deltaTime;

            if (afkTime > 5)
            {
                thinking = true;
                artistFace.texture = Resources.Load<Texture>("Sprites/artist_thinking");
            }
        }
        else if (thinking && Input.anyKey)
        {
            changeToMainFace();
            afkTime = 0;
            thinking = false;
        }


        // Make sure our stuff is valid
        if (rt != null)
        {
            if (ri != null && !stampBackground.activeSelf && gameActive)
            {
                HandleInput();

                if (stampMode)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasObject, Input.mousePosition, cam, out localPoint);
                    stampShadow.localPosition = localPoint - (Vector2)transform.localPosition;
                }
            }
        }

        if (gameActive)
        {
            timer -= Time.deltaTime;
            string secondsText = (((int)timer) % 60).ToString();
            if ((((int)timer) % 60) < 10)
            {
                secondsText = "0" + secondsText;
            }
            timerText.text = ((int)(timer / 60)).ToString() + ":" + secondsText;

            if (timer <= 0)
            {
                gameActive = false;
                timerText.text = "0:00";
                timer = 0;
                ranOutOfTime();
            }


        }
    }

    bool holding = false;

    void HandleInput()
    {
        // Since we can only paint on the canvas if the mouse button is press
        // May be best to revise this so the tool has a call back for example a 
        // fill tool selected would call its own "Handle" method,

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
        {
            holdTime += Time.deltaTime;

            if (holdTime > 1.5f && !holding)
            {
                artistFace.texture = Resources.Load<Texture>("Sprites/artist_focused");
                holding = true;
            }

            Vector2Int mousePos = Vector2Int.zero;
            // We have input, lets convert the mouse position to be relative to the canvas
            ConvertMousePosition(ref mousePos);
            if (xSnap)
            {
                mousePos.x = Mathf.RoundToInt(mousePos.x / 85f) * 85;
                if (mousePos.x == width)
                {
                    mousePos.x = width - 1;
                }
            }

            if (ySnap)
            {
                mousePos.y = Mathf.RoundToInt(mousePos.y / 85f) * 85;
                if (mousePos.y == height)
                {
                    mousePos.y = height - 1;
                }
            }


            // Checking that our mouse is in bounds, which is stored in our height and width variable and as long as it has a "positive value"
            if (MouseIsInBounds(mousePos))
            {
                // This method could be removed to be the tool method I mention above
                // you would pass in the mousePosition, and color similar to this.
                // This way each tool would be its "own" component that would be activated
                // through some form of UI.




                if (!stampMode)
                {
                    if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                    {
                        PaintTexture(mousePos, Color.white);
                    }
                    else
                    {
                        PaintTexture(mousePos, penColor); // Also the color you want would be here to...
                    }

                    if (hasLastPosition)
                    {
                        if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                        {
                            PaintBetweenTwoPoints(mousePos, Vector2Int.RoundToInt(lastPosition), Color.white);
                        }
                        else
                        {
                            PaintBetweenTwoPoints(mousePos, Vector2Int.RoundToInt(lastPosition), penColor);
                        }
                    }

                    pencilSounds.mute = false;
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/stamp"));
                        PaintTextureImage(mousePos, stampName);
                    }
                }



                if (!hasLastPosition)
                {
                    hasLastPosition = true;
                }
                lastPosition = mousePos;
            }
            else
            {
                if (hasLastPosition)
                {
                    saveSlice();
                }

                hasLastPosition = false;
            }
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            if (holding)
            {
                holding = false;
                holdTime = 0;
                changeToMainFace();
            }

            pencilSounds.mute = true;

            hasLastPosition = false;
            Vector2Int mousePos = Vector2Int.zero;
            // We have input, lets convert the mouse position to be relative to the canvas
            ConvertMousePosition(ref mousePos);

            // Checking that our mouse is in bounds, which is stored in our height and width variable and as long as it has a "positive value"
            if (MouseIsInBounds(mousePos) && !stampMode)
            {

                saveSlice();
            }
        }

        if (true)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    redo();
                }
                else
                {
                    undo();
                }
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                redo();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                clear();
            }


        }
    }

    public static T[] Copy<T>(T[] array)
    {
        int width = array.Length;
        T[] copy = new T[width];

        for (int w = 0; w < width; w++)
        {
            copy[w] = array[w];
        }

        return copy;
    }

    void saveSlice()
    {
        if (timeslices.Count < 30 && timesliceIndex == timeslices.Count - 1)
        {
            timeslices.Add(canvas.GetPixels32());
            timeslicesAttributes.Add(Copy(pixelAttributes));
            timesliceIndex++;
        }
        else if (timesliceIndex == 29)
        {
            for (int i = 0; i < 29; i++)
            {
                timeslices[i] = timeslices[i + 1];
                timeslicesAttributes[i] = timeslicesAttributes[i + 1];
            }
            timeslices[29] = canvas.GetPixels32();
            timeslicesAttributes[29] = pixelAttributes;
        }
        else
        {
            timeslices[timesliceIndex + 1] = canvas.GetPixels32();
            timeslicesAttributes[timesliceIndex + 1] = pixelAttributes;
            for (int i = timesliceIndex + 2; i < timeslices.Count; i++)
            {
                timeslices.RemoveAt(timesliceIndex + 2);
                timeslicesAttributes.RemoveAt(timesliceIndex + 2);
            }
            timesliceIndex++;
        }
        checkRequirements();
    }

    public void undo()
    {
        if (timesliceIndex > 0)
        {
            canvas.SetPixels32(timeslices[timesliceIndex - 1]);
            canvas.Apply(true);
            pixelAttributes = timeslicesAttributes[timesliceIndex - 1];
            timesliceIndex = timesliceIndex - 1;
            checkRequirements();
        }
    }

    public void redo()
    {
        if (timesliceIndex < timeslices.Count - 1)
        {
            canvas.SetPixels32(timeslices[timesliceIndex + 1]);
            canvas.Apply(true);
            pixelAttributes = timeslicesAttributes[timesliceIndex + 1];
            timesliceIndex = timesliceIndex + 1;
            checkRequirements();
        }
    }

    public void clear()
    {
        Color32 resetColor = new Color32(255, 255, 255, 255);
        Color32[] resetColorArray = canvas.GetPixels32();
        pixelAttributes = new string[width * height];

        for (int i = 0; i < resetColorArray.Length; i++)
        {
            resetColorArray[i] = resetColor;
        }

        canvas.SetPixels32(resetColorArray);
        canvas.Apply();
        saveSlice();
        checkRequirements();
    }

    void PaintBetweenTwoPoints(Vector2Int pos1, Vector2Int pos2, Color p)
    {
        Vector2 slope = Vector2.ClampMagnitude(pos2 - pos1, 1) * brushSize * .5f;
        Vector2 currentPos = pos1;

        while (Vector2.Distance(currentPos, pos2) > brushSize * .5f)
        {
            currentPos += slope;
            PaintTexture(Vector2Int.RoundToInt(currentPos), p);
        }
    }


    void PaintTexture(Vector2Int pos, Color color)
    {
        // In our method we don't allow transparency and we are just replacing the pixel,

        if (!isSquare)
        {
            for (int i = pos.y - brushSize; i < pos.y + brushSize; i++)
            {
                for (int j = pos.x; Mathf.Pow((j - pos.x), 2) + Mathf.Pow((i - pos.y), 2) <= Mathf.Pow(brushSize, 2); j--)
                {
                    if (i < height && j < width && i >= 0 && j >= 0)
                    {
                        canvas.SetPixel(j, i, color);
                        pixelAttributes[i * width + j] = null;
                    }

                }
                for (int j = pos.x + 1; (j - pos.x) * (j - pos.x) + (i - pos.y) * (i - pos.y) <= brushSize * brushSize; j++)
                {
                    if (i < height && j < width && i >= 0 && j >= 0)
                    {
                        canvas.SetPixel(j, i, color);
                        pixelAttributes[i * width + j] = null;
                    }
                }
            }
        }
        else
        {
            for (int i = pos.y - brushSize; i < pos.y + brushSize; i++)
            {
                for (int j = pos.x - brushSize; j < pos.x + brushSize; j++)
                {
                    if (i < height && j < width && i >= 0 && j >= 0)
                    {
                        canvas.SetPixel(j, i, color);
                        pixelAttributes[i * width + j] = "";
                    }

                }
            }
        }
        // Applying out change, we dont want to mip levels.
        // If you are doing some blending or transparency stuff that would be handled by your tool
        canvas.Apply(true);
    }

    void PaintTextureImage(Vector2Int pos, string stampName)
    {
        Texture2D stamp = Resources.Load<Texture2D>("Sprites/Stamps/" + stampName);
        int posX = pos.x - stamp.width / 2;
        int posY = pos.y - stamp.height / 2;
        int amt = 0;
        int amt2 = 0;

        for (int x = 0; x < stamp.width; x++)
        {
            for (int y = 0; y < stamp.height; y++)
            {
                Color cs = stamp.GetPixel(x, y);
                if (cs.a == 1)
                {
                    if (x + posX < canvas.width && y + posY < canvas.height && x + posX >= 0 && y + posY >= 0)
                    {
                        canvas.SetPixel(x + posX, y + posY, cs);

                        if (stampName == "dog_person")
                        {
                            if (y > stamp.height / 2)
                            {
                                pixelAttributes[(y + posY) * width + x + posX] = "dog_half";
                            }
                            else
                            {
                                pixelAttributes[(y + posY) * width + x + posX] = "human_half";
                            }
                        }
                        else
                        {
                            pixelAttributes[(y + posY) * width + x + posX] = stampName;
                        }
                    }

                    if (stampName == "dog_person")
                    {
                        if (y > stamp.height / 2)
                        {
                            amt++;
                        }
                        else
                        {
                            amt2++;
                        }
                    }
                    else
                    {
                        amt++;
                    }
                }

            }
        }

        if (!amountOfPixels.ContainsKey(stampName))
        {
            if (stampName == "dog_person" && !amountOfPixels.ContainsKey("dog_half"))
            {
                amountOfPixels.Add("dog_half", amt);
                amountOfPixels.Add("human_half", amt2);
            }
            else
            {
                amountOfPixels.Add(stampName, amt);
            }

        }

        canvas.Apply(true);
        saveSlice();
    }

    [SerializeField] List<TextMeshProUGUI> requirements;
    Dictionary<string, int> amountOfPixels = new Dictionary<string, int>();
    HashSet<string> fruits = new HashSet<string> { "eggplant", "banana", "orange", "apple", "strawberry", "banana2", "apple2", "blueberry1", "blueberry2" };
    HashSet<string> colors = new HashSet<string> { "blue", "red", "green", "black", "white" };
    HashSet<string> fishList = new HashSet<string> { "fish", "star", "star2" };
    HashSet<string> dogList = new HashSet<string> { "dog_half", "long_dog" };
    HashSet<string> starList = new HashSet<string> { "star", "star2" };
    HashSet<string> creatureList = new HashSet<string> { "dog_half", "star", "star2", "cig", "baby_eater", "disowned", "looking_up", "shocked", "sitting", "cool_guy" };
    HashSet<string> smokerList = new HashSet<string> { "strawberry", "cig" };
    HashSet<string> alienList = new HashSet<string> { "alien", "star", "star2" };
    HashSet<string> animalsList = new HashSet<string> { "fish", "dog_half", "long_dog", "cat" };



    public void checkRequirements()
    {
        if (zenMode)
        {
            return;
        }

        //Debug.Log("checking....");

        Color32[] colorArray = canvas.GetPixels32();
        Dictionary<string, int> frequency = new Dictionary<string, int>();
        HashSet<string> attributesInPainting = new HashSet<string>();

        frequency.Add("blue", 0);
        frequency.Add("red", 0);
        frequency.Add("green", 0);
        frequency.Add("black", 0);
        frequency.Add("white", 0);
        float brightness = 0;

        HashSet<string> set1 = new HashSet<string>();
        bool condition1 = true;
        bool condition2 = false;
        int lastRow = colorArray.Length - width - 1;
        int count1 = 0;
        int count2 = 0;

        for (int i = 0; i < colorArray.Length; i++)
        {
            if (colorArray[i].b > 178f && (colorArray[i].g < 128 || colorArray[i].r < 128))
            {
                frequency["blue"] += 1;
            }
            else if (colorArray[i].r > 178f && (colorArray[i].g < 128 || colorArray[i].b < 128))
            {
                frequency["red"] += 1;
            }
            else if (colorArray[i].g > 178f && (colorArray[i].r < 128 || colorArray[i].b < 0.5f))
            {
                frequency["green"] += 1;
            }
            else if (colorArray[i].r > 200f && colorArray[i].g > 200f && colorArray[i].b > 200f)
            {
                frequency["white"] += 1;
            }
            else if (colorArray[i].r < 60 && colorArray[i].g < 60 && colorArray[i].b < 60)
            {
                frequency["black"] += 1;
                if (level == 2 && i % width < width / 2)
                {
                    frequency["black"] -= 1;
                }
            }

            brightness += (colorArray[i].r + colorArray[i].g + colorArray[i].b);

            if (pixelAttributes[i] != null)
            {
                if (frequency.ContainsKey(pixelAttributes[i]))
                {
                    frequency[pixelAttributes[i]]++;
                }
                else
                {
                    frequency.Add(pixelAttributes[i], 1);
                    attributesInPainting.Add(pixelAttributes[i]);
                }

                if (level == 2 && pixelAttributes[i] == "cat" && i % width > width / 2)
                {
                    condition1 = false;
                }
            }

            //adjacency
            if (i > width && i < lastRow && i % width != 0 && i % width != width - 1)
            {
                if (level == 4 && condition1)
                {
                    if (pixelAttributes[i] != null && fruits.Contains(pixelAttributes[i]))
                    {
                        if (fruits.Contains(pixelAttributes[i + width]) && pixelAttributes[i + width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (fruits.Contains(pixelAttributes[i - width]) && pixelAttributes[i - width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (fruits.Contains(pixelAttributes[i + 1]) && pixelAttributes[i + 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (fruits.Contains(pixelAttributes[i - 1]) && pixelAttributes[i - 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                    }
                }
                else if (level == 1 && condition1)
                {
                    if (pixelAttributes[i] != null && smokerList.Contains(pixelAttributes[i]))
                    {
                        if (pixelAttributes[i + width] != null && pixelAttributes[i + width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i - width] != null && pixelAttributes[i - width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i + 1] != null && pixelAttributes[i + 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i - 1] != null && pixelAttributes[i - 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                    }
                }
                else if (level == 3)
                {
                    if (pixelAttributes[i] != null && fishList.Contains(pixelAttributes[i]))
                    {
                        if (pixelAttributes[i] != pixelAttributes[i + width] && colorArray[i + width].b < colorArray[i + width].r + colorArray[i + width].g)
                        {
                            count2++;
                        }
                        else if (pixelAttributes[i] != pixelAttributes[i - width] && colorArray[i - width].b < colorArray[i - width].r + colorArray[i - width].g)
                        {
                            count2++;
                        }
                        else if (pixelAttributes[i] != pixelAttributes[i - 1] && colorArray[i - 1].b < colorArray[i - 1].r + colorArray[i - 1].g)
                        {
                            count2++;
                        }
                        else if (pixelAttributes[i] != pixelAttributes[i + 1] && colorArray[i + 1].b < colorArray[i + 1].r + colorArray[i + 1].g)
                        {
                            count2++;
                        }
                    }
                }
                else if (level == 8 && condition1)
                {
                    if (pixelAttributes[i] != null)
                    {
                        if (pixelAttributes[i + width] != null && pixelAttributes[i + width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i - width] != null && pixelAttributes[i - width] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i + 1] != null && pixelAttributes[i + 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                        else if (pixelAttributes[i - 1] != null && pixelAttributes[i - 1] != pixelAttributes[i])
                        {
                            condition1 = false;
                        }
                    }
                }
            }
        }


        brightness = (brightness / 3) / (colorArray.Length * 255);
        Debug.Log("brightness is " + brightness.ToString());


        foreach (string s in attributesInPainting)
        {
            if ((s == "apple" || s == "apple2") && level == 1)
            {
                if (frequency[s] >= canvas.width * canvas.height * .2f)
                {
                    condition2 = true;
                }
            }

            if (level == 6 && creatureList.Contains(s))
            {
                count1 += frequency[s];
                print("count1 is now: " + count1.ToString());
            }


            frequency[s] = Mathf.RoundToInt((float)frequency[s] / amountOfPixels[s]);

            if (level == 1 && smokerList.Contains(s))
            {
                count1 += frequency[s];
            }

            if ((level == 0 || level == 7) && alienList.Contains(s))
            {
                count1 += frequency[s];
            }

            if ((level == 4 || level == 8) && frequency[s] >= 1 && fruits.Contains(s))
            {
                set1.Add(s);
            }

            if (level == 6 && creatureList.Contains(s))
            {
                count2 += frequency[s];
            }

            if (level == 3 && fishList.Contains(s))
            {
                count1 += frequency[s];
            }

            if (level == 5 && dogList.Contains(s))
            {
                count1 += frequency[s];
            }

            if (level == 5 && starList.Contains(s))
            {
                count2 += frequency[s];
            }
        }



        foreach (TextMeshProUGUI t in requirements)
        {
            t.color = Color.red;
        }

        switch (level)
        {
            case 0:
                if (frequency["blue"] > canvas.width * canvas.height * .4f)
                {
                    requirements[0].color = Color.black;
                }
                if (count1 == 2)
                {
                    requirements[1].color = Color.black;
                }
                if (frequency.ContainsKey("heart") && frequency["heart"] == 1)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 1:
                if (count1 == 1)
                {
                    requirements[0].color = Color.black;
                }
                if (condition2)
                {
                    requirements[1].color = Color.black;
                }
                if (condition1)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 2:
                if (frequency.ContainsKey("bible") && frequency["bible"] == 3)
                {
                    requirements[0].color = Color.black;
                }
                if (condition1 && frequency.ContainsKey("cat") && frequency["cat"] == 1)
                {
                    requirements[1].color = Color.black;
                }
                if (frequency["black"] > .2f * canvas.width * canvas.height)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 3:
                if (count1 >= 2)
                {
                    requirements[1].color = Color.black;
                }
                if (frequency["blue"] < canvas.width * canvas.height * .4f && frequency["blue"] > canvas.width * canvas.height * .2f)
                {
                    requirements[0].color = Color.black;
                }
                if (count2 < 50)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 4:
                if (set1.Count == 2)
                {
                    requirements[0].color = Color.black;
                }
                if (condition1)
                {
                    requirements[1].color = Color.black;
                }
                if (frequency["red"] > canvas.width * canvas.height * .2f && frequency["red"] < canvas.width * canvas.height * .4f)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 5:
                if (frequency["black"] > canvas.width * canvas.height * .3f && frequency["black"] < canvas.width * canvas.height * .5f)
                {
                    requirements[0].color = Color.black;
                }
                if (count1 >= 1)
                {
                    requirements[1].color = Color.black;
                }
                if (count2 == 2)
                {
                    requirements[2].color = Color.black;
                }
                break;
            case 6:
                if (frequency["black"] > canvas.width * canvas.height * .2f)
                {
                    requirements[0].color = Color.black;
                }

                if (count1 > canvas.width * canvas.height * .4f)
                {
                    requirements[1].color = Color.black;
                }

                if (count2 <= 3)
                {
                    requirements[2].color = Color.black;
                }

                if (frequency.ContainsKey("baby_eater") && frequency["baby_eater"] == 1)
                {
                    requirements[3].color = Color.black;
                }
                break;
            case 7:
                if (frequency.ContainsKey("cat") && frequency["cat"] > count1)
                {
                    requirements[0].color = Color.black;
                }
                if (frequency["green"] > canvas.width * canvas.height * .3f)
                {
                    requirements[1].color = Color.black;
                }
                if (frequency["white"] < canvas.width * canvas.height * .25f)
                {
                    requirements[2].color = Color.black;
                }
                if (count1 > 2)
                {
                    requirements[3].color = Color.black;
                }
                break;
            case 8:
                if (frequency.ContainsKey("sad_emoji") && frequency["sad_emoji"] == 1)
                {
                    requirements[0].color = Color.black;
                }
                if (brightness < 0.5f)
                {
                    requirements[1].color = Color.black;
                }
                if (set1.Count >= 2)
                {
                    requirements[2].color = Color.black;
                }
                if (condition1)
                {
                    requirements[3].color = Color.black;
                }
                break;



        }

        conditionsAttained[level] = "";

        for (int i = 0; i < requirements.Count; i++)
        {
            if (requirements[i].color.r == 0)
            {
                conditionsAttained[level] = conditionsAttained[level] + i.ToString();
                checkboxes[i].texture = Resources.Load<Texture>("Sprites/checkbox_tick");
            }
            else
            {
                checkboxes[i].texture = Resources.Load<Texture>("Sprites/checkbox_untick");
            }
        }
    }

    List<List<string>> previousRequirements = new List<List<string>>();
    [SerializeField] TextMeshProUGUI letterHeader;
    [SerializeField] List<RawImage> checkboxes;
    int roundNumber = 0;
    List<int> roundEndings = new List<int>() { 3, 6, 9 };


    public void startLevel()
    {
        foreach (TextMeshProUGUI t in requirements)
        {
            t.color = Color.red;
            t.text = "";
        }

        if (!zenMode)
        {
            int beginningLevel = 0;
            int endingLevel = roundEndings[roundNumber];
            if (roundNumber > 0)
            {
                beginningLevel = roundEndings[roundNumber - 1];
            }

            amountOfDrawingsLeftText.text = (level - beginningLevel + 1).ToString() + " of " + (endingLevel - beginningLevel).ToString();
        }


        if (zenMode)
        {
            amountOfDrawingsLeftText.text = "";
        }

        if (!zenMode)
        {
            checkboxes[0].gameObject.SetActive(true);
            checkboxes[1].gameObject.SetActive(true);
            checkboxes[2].gameObject.SetActive(true);
            checkboxes[3].gameObject.SetActive(false);

            dialogue.Stop();
            dialogue.clip = Resources.Load<AudioClip>("Sounds/dialogue" + level.ToString());
            dialogue.Play();
        }


        gameActive = true;
        switch (level)
        {
            case 0:
                GetComponent<RectTransform>().sizeDelta = new Vector2(564, 250);
                timer = 90;
                requirements[0].text = "More than 40% blue";
                requirements[1].text = "Two aliens";
                requirements[2].text = "Exactly one heart";
                letterHeader.text = "Dear " + authorName + ",\nCan you draw this? It's very important to me.";
                break;
            case 1:
                timer = 90;
                GetComponent<RectTransform>().sizeDelta = new Vector2(400, 400);
                requirements[0].text = "1 cigarette man smoking";
                requirements[1].text = "More than 20% apples";
                requirements[2].text = "Other stamps can't touch cigarette man";
                letterHeader.text = authorName + "!!!!\nDude you have to draw this!!!";
                break;
            case 2:
                timer = 90;
                GetComponent<RectTransform>().sizeDelta = new Vector2(600, 250);
                requirements[0].text = "Three whole bibles";
                requirements[1].text = "One cat on the left side";
                requirements[2].text = "Right side is at least 40% black";
                letterHeader.text = "Hey " + authorName + ",\nThis portrait is a secret. Don't tell anyone...";
                break;
            case 3:
                timer = 75;
                GetComponent<RectTransform>().sizeDelta = new Vector2(500, 320);
                requirements[0].text = ">20% and <40% blue";
                requirements[1].text = "At least 2 fish";
                requirements[2].text = "Fish can only touch blue";
                letterHeader.text = "I love fish!!\n" + authorName + " please draw some fish!!!!";
                break;
            case 4:
                timer = 75;
                GetComponent<RectTransform>().sizeDelta = new Vector2(300, 500);
                requirements[0].text = "Two different fruits";
                requirements[1].text = "Fruits cannot touch";
                requirements[2].text = "Between 20% and 40% red";
                letterHeader.text = "Yo " + authorName + ",\nI this poster for an event. The Fruit Cup is upon us!";
                break;
            case 5:
                timer = 75;
                GetComponent<RectTransform>().sizeDelta = new Vector2(400, 400);
                requirements[0].text = ">30% and 50% black";
                requirements[1].text = "A dog";
                requirements[2].text = "Two star shaped people";
                letterHeader.text = "Hey " + authorName + ",\nIt would be amazing if you drew this lol";
                break;
            case 6:
                timer = 60;
                requirements[0].text = ">20% black";
                requirements[1].text = ">40% are creatures";
                requirements[2].text = "3 creatures or less";
                requirements[3].text = "One baby eater";
                letterHeader.text = "Ayo it's Tony!\n" + "My daughter needs this!";
                GetComponent<RectTransform>().sizeDelta = new Vector2(600, 350);
                checkboxes[3].gameObject.SetActive(true);
                break;
            case 7:
                timer = 60;
                requirements[0].text = "More cats than aliens";
                requirements[1].text = "More than 30% green";
                requirements[2].text = "Less than 25% white";
                requirements[3].text = "More than 2 aliens";
                letterHeader.text = authorName + ",\n I just think aliens and cats are really cool...";
                GetComponent<RectTransform>().sizeDelta = new Vector2(400, 400);
                checkboxes[3].gameObject.SetActive(true);
                break;
            case 8:
                timer = 60;
                requirements[0].text = "One sad emoji";
                requirements[1].text = "Average brightness < 50%";
                requirements[2].text = ">2 different fruits";
                requirements[3].text = "Stamps can't be touching";
                letterHeader.text = authorName + ",\n PLEASE PLEASE PLEASE PLEASEEEEE";
                GetComponent<RectTransform>().sizeDelta = new Vector2(600, 350);
                checkboxes[3].gameObject.SetActive(true);
                break;


        }

        //timer = 5;


        if (zenMode)
        {
            timer = 2400;
            letterHeader.text = "Dear " + authorName + ",\n No rules! Have fun!";
            GetComponent<RectTransform>().sizeDelta = new Vector2(600, 500);
            requirements[0].text = "";
            requirements[1].text = "";
            requirements[2].text = "";
            requirements[3].text = "";
        }

        CreateTexture2D();
        checkRequirements();

        List<string> temp = new List<string>();
        foreach (TextMeshProUGUI t in requirements)
        {
            temp.Add(t.text);
        }
        previousRequirements.Add(temp);
    }

    public void submitDrawing()
    {
        if (gameActive)
        {
            gameActive = false;
            StartCoroutine(submitDrawingCo());
        }
    }

    public void ranOutOfTime()
    {
        StartCoroutine(submitDrawingCo());
    }

    [SerializeField] RectTransform drawingObject;
    [SerializeField] RectTransform requirementsPage;

    Texture2D[] savedDrawings = new Texture2D[24];

    [SerializeField] RectTransform container;
    [SerializeField] int[] conditionsNeeded = new int[12];
    string[] conditionsAttained = new string[12];

    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] RectTransform endTextHolder;
    [SerializeField] RawImage blackOverlay;
    int resultsIndex = 0;
    [SerializeField] TextMeshProUGUI amountOfDrawingsLeftText;

    [SerializeField] List<GameObject> stampPages;
    [SerializeField] List<TextMeshProUGUI> stampPageTexts;
    public void setStampPage(int i)
    {
        foreach (GameObject g in stampPages)
        {
            g.SetActive(false);
        }

        stampPages[i].SetActive(true);

        foreach (TextMeshProUGUI g in stampPageTexts)
        {
            g.color = Color.white;
        }

        stampPageTexts[i].color = Color.gray;
    }


    IEnumerator submitDrawingCo()
    {
        soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/paper" + UnityEngine.Random.Range(0, 3)));
        pencilSounds.mute = true;

        Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(drawingObject, new Vector2(-190, -800), 0.5f, false);
        Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(requirementsPage, new Vector2(900, 400), 0.5f, false);
        yield return new WaitForSeconds(0.6f);
        checkRequirements();
        savedDrawings[level] = canvas;





        statusText.text = "";
        int amountOff = 0;

        if (zenMode)
        {

        }
        else
        {
            amountOff = conditionsNeeded[level] - conditionsAttained[level].Length;
        }

        if (amountOff == 0 || zenMode)
        {
            mood++;
            StartCoroutine(changeExpression("artist_beaming", 3));
            statusText.text += "Perfect Comission! (+500)";
            money += 500;

            soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/coins"));
        }
        else if (amountOff == 1)
        {
            StartCoroutine(changeExpression("artist_happy", 3));
            statusText.text += "A little bit off! (+200)";
            money += 200;
            soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/coins"));
        }
        else if (amountOff == 2)
        {
            mood--;
            StartCoroutine(changeExpression("artist_sad", 3));
            statusText.text += "You tried... (+50)";
            money += 50;
            soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/wrong"));
        }
        else
        {
            mood -= 2;
            //StartCoroutine(changeExpression("artist_sad", 3));
            StartCoroutine(changeExpression("artist_game_over", 3));
            statusText.text += "What is this??? (+0)";
            soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/wrong"));
        }

        mood = Mathf.Clamp(mood, -3, 3);

        moneyText.text = "$" + money.ToString();
        StartCoroutine(showStatus());
        level++;



        if (zenMode || level == roundEndings[roundNumber])
        {
            if (!zenMode)
            {
                roundNumber++;
            }

            StartCoroutine(transitionSongs(music, null));
            StartCoroutine(transitionSongs(dialogue, null));
            endTextHolder.localPosition = new Vector2(1300, 0);
            Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(endTextHolder, new Vector2(0, 0), 1f, false);
            yield return new WaitForSeconds(3f);
            Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(endTextHolder, new Vector2(-1300, 0), 1f, true);
            yield return new WaitForSeconds(1.5f);
            Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(blackOverlay, 1, 1f);
            yield return new WaitForSeconds(1.1f);
            showRequirementResults(resultsIndex);
            yield return new WaitForSeconds(1.4f);
            music.Stop();
            music.clip = Resources.Load<AudioClip>("Sounds/results");
            music.Play();
            Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/VolumeFader")).GetComponent<VolumeFader>().set(music, musicVolume, 2f);
            container.localPosition = new Vector2(0, 900);
            Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(blackOverlay, 0, 2f);
            //Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(container, new Vector2(0, 900), 1f, false);

            dialogue.Stop();

        }
        else
        {
            startLevel();
            Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(drawingObject, new Vector2(-190, -205 + (drawingObject.sizeDelta.y / 2)), 0.5f, false);
            Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(requirementsPage, new Vector2(228, 400), 0.5f, false);
            soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/paper" + UnityEngine.Random.Range(0, 3)));
            yield return new WaitForSeconds(0.6f);
            getCoords();

        }

    }

    IEnumerator showStatus()
    {
        foreach (AlphaFader i in FindObjectsOfType<AlphaFader>())
        {
            i.restart();
        }
        statusText.rectTransform.localPosition = new Vector2(-180, 500);
        statusText.color = new Color(statusText.color.r, statusText.color.g, statusText.color.b, 1);
        Mover m = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>();
        m.set(statusText.rectTransform, new Vector2(-180, 300), 1.5f, false);
        yield return new WaitForSeconds(1f);
        AlphaFader a = Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>();
        a.set(statusText, 0, 4f);
        yield return new WaitForSeconds(2f);
    }

    IEnumerator transitionSongs(AudioSource a, AudioClip c)
    {
        foreach (VolumeFader i in FindObjectsOfType<VolumeFader>())
        {
            if (i.obj == a)
            {
                Destroy(i);
            }
        }
        //Debug.Log("fading");
        float oldVolume = a.volume;
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/VolumeFader")).GetComponent<VolumeFader>().set(a, 0, 1f);
        yield return new WaitForSeconds(1.1f);
        a.Stop();
        if (c != null)
        {
            a.clip = c;
            //Debug.Log(c.name);
            a.Play();
        }
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/VolumeFader")).GetComponent<VolumeFader>().set(a, oldVolume, 1f);
    }

    [SerializeField] RawImage artistFace;


    IEnumerator changeExpression(string exp, int time)
    {
        //Debug.Log(exp);
        artistFace.texture = Resources.Load<Texture>("Sprites/" + exp);
        yield return new WaitForSeconds(time);
        changeToMainFace();


    }

    void changeToMainFace()
    {
        if (mood == 0)
        {
            artistFace.texture = Resources.Load<Texture>("Sprites/artist_neutral");
        }
        else if (mood == 2 || mood == 3 || mood == 1)
        {
            artistFace.texture = Resources.Load<Texture>("Sprites/artist_happy");
        }
        else if (mood > 3)
        {
            artistFace.texture = Resources.Load<Texture>("Sprites/artist_beaming");
        }
        else if (mood == -1 || mood == -2)
        {
            artistFace.texture = Resources.Load<Texture>("Sprites/artist_sad");
        }
        else
        {
            //artistFace.texture = Resources.Load<Texture>("Sprites/artist_sad");
            artistFace.texture = Resources.Load<Texture>("Sprites/artist_done_for");
        }
    }


    bool MouseIsInBounds(Vector2Int mousePos)
    {
        // The position is already relative to the texture so if it is >= to 0 and less then the texture
        // width and height it is in bounds.
        //Debug.Log(mousePos);
        if (mousePos.x >= 0 && mousePos.x < width)
        {
            if (mousePos.y >= 0 && mousePos.y < height)
            {
                return true;
            }
        }
        return false;
    }

    void ConvertMousePosition(ref Vector2Int mouseOut)
    {
        // The mouse Position, and the RawImage position are returned in the same space
        // So we can just update based off of that
        Vector3 real = cam.ScreenToWorldPoint(Input.mousePosition) - bottomLeft;
        mouseOut.x = Mathf.RoundToInt((real.x / boxWidth) * width);
        mouseOut.y = Mathf.RoundToInt((real.y / boxHeight) * height);
    }

    void getCoords()
    {
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            // Setting our corners  based on the fact GetCorners returns them in clockwise order starting from BL TL TR BR.
            bottomLeft = corners[0];
            topRight = corners[2];

            boxHeight = topRight.y - bottomLeft.y;
            boxWidth = topRight.x - bottomLeft.x;


            //Debug.Log("done!");
        }
    }


    void CreateTexture2D()
    {
        width = (int)GetComponent<RectTransform>().sizeDelta.x;
        height = (int)GetComponent<RectTransform>().sizeDelta.y;
        hasLastPosition = false;

        // Creating our "Draw" texture to be the same size as our RawImage.
        canvas = new Texture2D(width, height);
        ri.texture = canvas;

        Color32 resetColor = new Color32(255, 255, 255, 255);
        Color32[] resetColorArray = canvas.GetPixels32();

        for (int i = 0; i < resetColorArray.Length; i++)
        {
            resetColorArray[i] = resetColor;
        }

        oldTexture = resetColorArray;
        canvas.SetPixels32(resetColorArray);
        canvas.Apply();
        timeslices.Clear();
        timeslicesAttributes.Clear();
        timeslices.Add(canvas.GetPixels32());
        pixelAttributes = new string[width * height];
        timeslicesAttributes.Add(Copy(pixelAttributes));
        timesliceIndex = 0;
    }

    public void changeColor(int i)
    {
        penColor = colorList[i];
        oldPenColor = penColor;
    }

    [SerializeField] List<RawImage> tools = new List<RawImage>();

    public void changeTool(int i)
    {
        foreach (RawImage r in tools)
        {
            r.color = Color.white;
        }

        soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/paper" + UnityEngine.Random.Range(0, 3)));

        tools[i].color = Color.gray;

        if (i == 0)
        {
            stampMode = false;
            penColor = oldPenColor;
        }
        else if (i == 1)
        {
            stampMode = false;
            penColor = Color.white;
        }
        else
        {
            stampMode = true;
            openUpStamps();
        }
    }


    [SerializeField] GameObject stampBackground;
    [SerializeField] GameObject stampUnderline;
    [SerializeField] RawImage stampImage;

    public void openUpStamps()
    {
        stampBackground.SetActive(true);
    }

    public void chooseStamp(string s)
    {
        stampBackground.SetActive(false);
        //stampImage.texture = Resources.Load<Texture2D>("Sprites/Stamps/" + s);
        stampShadow.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("Sprites/Stamps/" + s);
        stampShadow.sizeDelta = new Vector2(stampShadow.GetComponent<RawImage>().texture.width, stampShadow.GetComponent<RawImage>().texture.height);
        stampName = s;
        stampShadow.gameObject.SetActive(true);
    }

    [SerializeField] RectTransform brushSizeImage;
    public void changeSize(bool up)
    {
        if (up)
        {
            sizeIndex++;
            sizeIndex = Mathf.Min(sizeIndex, sizeNums.Count - 1);
        }
        else
        {
            sizeIndex--;
            sizeIndex = Mathf.Max(0, sizeIndex);
        }

        brushSize = sizeNums[sizeIndex];
        brushSizeImage.sizeDelta = new Vector2(brushSize * 3, brushSize * 3);

    }

    bool timeCheck()
    {
        if ((int)((System.DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds) - PlayerPrefs.GetInt("lastUpload", 0) < 180)
        {
            return false;
        }

        return true;
    }

    public void backToMenuZen()
    {
        if (zenMode)
        {
            SceneManager.LoadScene("Menu");
        }
    }

    [SerializeField] List<TextMeshProUGUI> resultsRequirements = new List<TextMeshProUGUI>();
    [SerializeField] TextMeshProUGUI resultsSummary;
    void showRequirementResults(int lvl)
    {
        if (zenMode)
        {

        }
        else
        {
            for (int i = 0; i < resultsRequirements.Count; i++)
            {
                resultsRequirements[i].text = previousRequirements[lvl][i];
                //Debug.Log(conditionsAttained[lvl]);
                if (conditionsAttained[lvl].Contains(i.ToString()))
                {
                    resultsRequirements[i].color = Color.black;
                }
                else
                {
                    resultsRequirements[i].color = Color.red;
                }
            }
        }



        drawingNameText.text = "Commission " + (lvl + 1).ToString();
        if (zenMode)
        {
            drawingNameText.text = "Free Draw";
        }

        if (!zenMode)
        {
            resultsSummary.text = conditionsAttained[lvl].Length.ToString() + " of " + conditionsNeeded[lvl].ToString() + " requirements";
            resultsSummary.color = Color.Lerp(Color.red, Color.black, (float)conditionsAttained[lvl].Length / conditionsNeeded[lvl]);
        }
        else
        {
            resultsSummary.text = "Well Done!";
        }


        if (zenMode)
        {
            resultsPageHandler.setUpResultsPage(-1, savedDrawings[lvl], 0);
        }
        else
        {
            resultsPageHandler.setUpResultsPage(lvl, savedDrawings[lvl], conditionsNeeded[lvl]);
        }


        if (zenMode || (!zenMode && conditionsAttained[lvl].Length == conditionsNeeded[lvl]))
        {
            if (zenMode)
            {
                StartCoroutine(uploadToDatabase(-1, savedDrawings[lvl], "Untitled", 0));
            }
            else
            {
                StartCoroutine(uploadToDatabase(lvl, savedDrawings[lvl], "Untitled", conditionsAttained[lvl].Length));

            }

        }
        else
        {
            Debug.Log("did not upload");
        }

    }

    [SerializeField] TextMeshProUGUI timeOfDay;
    [SerializeField] List<AudioClip> backgroundMusic;

    IEnumerator leaveResults()
    {
        mood = 0;
        changeToMainFace();
        //Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(container, new Vector2(0, 0), 0.5f, false);
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/VolumeFader")).GetComponent<VolumeFader>().set(music, 0, 1f);
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(blackOverlay, 1, 2f);
        timeOfDay.text = (roundNumber + 3).ToString() + ":00 am";
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(timeOfDay, 1, 2f);
        yield return new WaitForSeconds(3f);
        container.localPosition = new Vector2(0, 0);
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(blackOverlay, 0, 2f);
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(timeOfDay, 0, 2f);
        music.Stop();
        music.clip = backgroundMusic[roundNumber];
        music.Play();
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/VolumeFader")).GetComponent<VolumeFader>().set(music, musicVolume, 1f);
        startLevel();
        yield return new WaitForSeconds(2f);
        soundFx.PlayOneShot(Resources.Load<AudioClip>("Sounds/paper" + UnityEngine.Random.Range(0, 3)));
        Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(drawingObject, new Vector2(-190, -205 + (drawingObject.sizeDelta.y / 2)), 0.5f, false);
        Instantiate(Resources.Load<GameObject>("Prefabs/Mover")).GetComponent<Mover>().set(requirementsPage, new Vector2(228, 400), 0.5f, false);
        yield return new WaitForSeconds(0.6f);
        getCoords();
    }

    [SerializeField] TextMeshProUGUI nextPageButton;
    public void nextPageResults()
    {
        if (zenMode)
        {
            SceneManager.LoadScene("Menu");
            return;
        }

        nextPageButton.text = "Inspect Next Drawing";
        resultsIndex++;
        if (resultsIndex < roundEndings[roundNumber - 1])
        {
            if (resultsIndex == roundEndings[roundNumber - 1] - 1)
            {
                nextPageButton.text = "Back to Work!";

            }
            showRequirementResults(resultsIndex);
        }
        else
        {
            if (roundNumber == roundEndings.Count)
            {
                StartCoroutine(ending());
                return;
            }
            StartCoroutine(leaveResults());

            //startLevel();
        }

    }

    IEnumerator ending()
    {
        Instantiate<GameObject>(Resources.Load<GameObject>("Prefabs/AlphaFader")).GetComponent<AlphaFader>().set(blackOverlay, 1, 2);
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("ending");

    }

    int money = 0;
    [SerializeField] TextMeshProUGUI moneyText;
    string authorName = "Francine";
    [SerializeField] Results resultsPageHandler;


    bool blankCheck(Texture2D t)
    {
        Color32[] colorArray = t.GetPixels32();
        for (int i = 0; i < colorArray.Length; i++)
        {
            if (colorArray[i].r < 250 || colorArray[i].g < 250 || colorArray[i].b < 250)
            {
                return false;
            }
        }

        Debug.Log("is blank!");
        return true;
    }

    [SerializeField] bool upload;
    IEnumerator uploadToDatabase(int l, Texture2D img, string dN, int cM)
    {
        if (blankCheck(img))
        {
            yield break;
        }


        if (upload)
        {
            yield return new WaitForSeconds(1);
            if (img == null)
            {
                yield break;
            }

            WWWForm form = new WWWForm();
            form.AddField("level", l);
            form.AddField("author", authorName);
            form.AddField("drawing", dN); //drawing name
            form.AddField("conditions", cM);

            form.AddBinaryData("userImage", img.EncodeToPNG());

            using (UnityWebRequest www = UnityWebRequest.Post("https://crowseeds.com/LIMITEDSPACE/drawImage.php", form))
            {
                yield return www.SendWebRequest();
                isSending = false;

                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log(www.error);
                    //StartCoroutine(fadeStatus("Server Error, Try Again Later!"));
                }
                else
                {
                    // Print response
                    Debug.Log(www.downloadHandler.text);
                    if (int.Parse(www.downloadHandler.text) >= 0)
                    {
                        lastSubmittedID = int.Parse(www.downloadHandler.text);
                        Debug.Log(lastSubmittedID);
                        //PlayerPrefs.SetInt("lastUpload", (int)((System.DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds));
                    }
                    else
                    {

                    }
                }
            }
        }
    }






}