using UnityEngine;
using UnityEngine.UI;

public class UISpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float framesPerSecond = 10f;
    public bool loop = true;

    Image image;
    int currentFrame;
    float timer;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (frames.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                if (loop) currentFrame = 0;
                else { currentFrame = frames.Length - 1; enabled = false; }
            }
            image.sprite = frames[currentFrame];
        }
    }

    public void Play(Sprite[] newFrames = null, float fps = -1f, bool shouldLoop = true)
    {
        if (newFrames != null) frames = newFrames;
        if (fps > 0) framesPerSecond = fps;
        loop = shouldLoop;
        currentFrame = 0;
        timer = 0;
        enabled = true;
    }
}
