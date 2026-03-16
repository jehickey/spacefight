using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class MonitorTextDisplay : MonoBehaviour
{
    Text textbox;
    public float scrollRate = 10;
    public float scrollY = 0;
    public float maxScroll;
    RectTransform rect;
    public float ScrollSpeedRandFactor = .5f;

    void Start()
    {
        textbox = GetComponent<Text>();
        rect = textbox.rectTransform;
        scrollY = Random.value * 1000;
        scrollRate *= (1 + Random.Range(-ScrollSpeedRandFactor, ScrollSpeedRandFactor));
    }

    void Update()
    {
        maxScroll = textbox.preferredHeight;
        scrollY -= scrollRate * Time.deltaTime;
        if (Mathf.Abs(scrollY) > maxScroll) scrollY = 0;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, scrollY);
    }
}
