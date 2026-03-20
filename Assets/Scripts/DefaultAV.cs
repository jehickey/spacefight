using UnityEngine;

public class DefaultAV : MonoBehaviour
{
    public static DefaultAV I { get; private set; }


    [Header("Engine Sounds")]
    public AudioClip BoostStart;
    public AudioClip BoostRunning;
    public AudioClip BoostEnd;
    public AudioClip BoostFail;
    public AudioClip BoostReady;

    [Header("Jumpdrive Sounds")]
    public AudioClip JumpAvailable;
    public AudioClip JumpHighlighted;
    public AudioClip JumpEngaging;
    public AudioClip JumpStart;
    public AudioClip JumpRunning;
    public AudioClip JumpEnd;
    public AudioClip JumpFail;




    private void Awake()
    {
        if (!Application.isPlaying) return;
        if (I && I != this)
        {
            Debug.Log("An instance of DefaultAV already exists!");
            return;
        }
        I = this;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }



    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
