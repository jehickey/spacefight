using UnityEngine;
using UnityEngine.Video;

public class Monitor : MonoBehaviour
{
    
    public VideoClip Video;
    [TextArea]
    public string TextContent = "Testing";
    public Color BackgroundColor = Color.black;
    public Color TextColor = Color.white;
    public int FontSize = 50;
    public float StaticStrength = .5f;
    public bool WordWrap = false;

    public float ScrollRate = 10;
    public bool useClearScreen = true;
    public bool ShowText = true;
    public bool ShowVideo = true;

    public bool LoopVideo = false;
    public bool VideoIsPlaying = false;

    private StaticModulator staticModulator;

    public MonitorScreen screenDisplay;
    public MonitorTextDisplay textDisplay;
    private VideoPlayer player;

    void Start()
    {
        screenDisplay = GetComponentInChildren<MonitorScreen>();
        textDisplay = GetComponentInChildren<MonitorTextDisplay>();
        staticModulator = GetComponentInChildren<StaticModulator>();
        player = GetComponent<VideoPlayer>();
        //if (Scrolling) textDisplay.RandomizeScrolling();
    }

    void Update()
    {
        if (staticModulator) StaticStrength = staticModulator.Output;

        if (textDisplay)
        {
            if (ShowText)
            {
                textDisplay.gameObject.SetActive(true);
                textDisplay.TextContent = TextContent;
                textDisplay.FontSize = FontSize;
                textDisplay.Scrolling = ScrollRate != 0;
                textDisplay.scrollRate = ScrollRate;
                textDisplay.TextColor = TextColor;
                textDisplay.wordWrap = WordWrap;
            }
            else
            {
                textDisplay.gameObject.SetActive(false);
            }
        }
        if (screenDisplay)
        {
            screenDisplay.backgroundColor = BackgroundColor;
            screenDisplay.staticStrength = StaticStrength;
            screenDisplay.useClearScreen = useClearScreen;
        }



        if (player)
        {
            VideoIsPlaying = player.isPlaying;
            player.clip = Video;
            player.isLooping = LoopVideo;
            //if (Video && !player.isPlaying && ShowVideo) player.Play();
            if (player.isPlaying && !ShowVideo) player.Stop();
        }
        
    }

    public void PlayVideo(VideoClip setclip = null)
    {

        if (player)
        {
            Video = setclip;
            player.clip = Video;
            player.Stop();
            player.Play();
            VideoIsPlaying = player.isPlaying;
        }
    }

    public void StopVideo()
    {
        if (player)
        {
            player.Stop();
        }
    }
}
