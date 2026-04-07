using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class MonitorTextDisplay : MonoBehaviour
{
    public string TextContent = "Testing";
    public bool Scrolling = false;
    public int FontSize = 50;
    public Color TextColor = Color.white;

    public Text textbox;
    public float scrollRate = 10;
    public float scrollY = 0;
    public float maxScroll;
    RectTransform rect;
    public float ScrollSpeedRandFactor = .5f;
    public bool wordWrap = false;

    void Start()
    {
        textbox = GetComponentInChildren<Text>();
        rect = textbox.rectTransform;
    }

    void Update()
    {
        if (!textbox) return;
        textbox.text = TextContent;
        textbox.fontSize = FontSize;
        textbox.color = TextColor;
        maxScroll = textbox.preferredHeight;

        if (wordWrap)
        {
            textbox.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
        else
        {
            textbox.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
        if (Scrolling)
        {
            scrollY -= scrollRate * Time.deltaTime;
            if (Mathf.Abs(scrollY) > maxScroll) scrollY = 0;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, scrollY);
        }
        else
        {
            scrollY = 0;
        }
    }

    public void RandomizeScrolling()
    {
        scrollY = Random.value * 1000;
        scrollRate *= (1 + Random.Range(-ScrollSpeedRandFactor, ScrollSpeedRandFactor));
        Scrolling = true;
    }

}
