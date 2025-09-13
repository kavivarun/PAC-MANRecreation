using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    private readonly List<Tween> activeTweens = new List<Tween>();

    public bool TweenExists(Transform target)
    {
        for (int i = 0; i < activeTweens.Count; i++)
            if (activeTweens[i].Target == target) return true;
        return false;
    }

    public bool AddTween(Transform target, Vector3 startPos, Vector3 endPos, float duration)
    {
        if (TweenExists(target)) return false;

        var t = new Tween
        {
            Target = target,
            StartPos = startPos,
            EndPos = endPos,
            Duration = Mathf.Max(0.0001f, duration),
            StartTime = Time.time
        };

        target.position = startPos;
        activeTweens.Add(t);
        return true;
    }

    void Update()
    {
        if (activeTweens.Count == 0) return;

        float now = Time.time;
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            var tw = activeTweens[i];
            float t = Mathf.Clamp01((now - tw.StartTime) / tw.Duration);
            tw.Target.position = Vector3.LerpUnclamped(tw.StartPos, tw.EndPos, t);

            if (t >= 1f)
            {
                tw.Target.position = tw.EndPos;
                activeTweens.RemoveAt(i);
            }
        }
    }
}
