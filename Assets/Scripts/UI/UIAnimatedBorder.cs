using UnityEngine;
using UnityEngine.UI;

public class UIAnimatedRectangleBorder : MonoBehaviour
{
    public Image dotPrefab;
    public int dotCount = 40;
    public float speed = 60f;

    RectTransform rect;
    Image[] dots;
    float offset;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        dots = new Image[dotCount];
        for (int i = 0; i < dotCount; i++)
        {
            dots[i] = Instantiate(dotPrefab, transform);
        }
    }

    void Update()
    {
        if (rect == null) return;
        float w = rect.rect.width / 2f;
        float h = rect.rect.height / 2f;
        float perimeter = 2f * (rect.rect.width + rect.rect.height);
        offset = (offset + speed * Time.deltaTime) % perimeter;

        for (int i = 0; i < dotCount; i++)
        {
            float d = (i / (float)dotCount * perimeter + offset) % perimeter;
            Vector2 p;

            if (d < rect.rect.width)
                p = new Vector2(-w + d, h);
            else if (d < rect.rect.width + rect.rect.height)
                p = new Vector2(w, h - (d - rect.rect.width));
            else if (d < rect.rect.width * 2 + rect.rect.height)
                p = new Vector2(w - (d - rect.rect.width - rect.rect.height), -h);
            else
                p = new Vector2(-w, -h + (d - rect.rect.width * 2 - rect.rect.height));

            dots[i].rectTransform.localPosition = p;
        }
    }
}
