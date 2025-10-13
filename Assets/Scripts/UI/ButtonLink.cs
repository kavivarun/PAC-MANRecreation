using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonLink : MonoBehaviour
{
    public enum TargetAction { LoadLevel1, LoadLevel2, LoadUpgrades, LoadMainMenu }

    [SerializeField] private TargetAction action;

    Button btn;

    void Awake() => btn = GetComponent<Button>();

    void OnEnable()
    {
        if (btn != null) btn.onClick.AddListener(InvokeAction);
    }

    void OnDisable()
    {
        if (btn != null) btn.onClick.RemoveListener(InvokeAction);
    }

    void InvokeAction()
    {
        var ui = UIManager.I != null ? UIManager.I : FindFirstObjectByType<UIManager>();
        if (ui == null) return;

        switch (action)
        {
            case TargetAction.LoadLevel1: ui.LoadLevel1(); break;
            case TargetAction.LoadLevel2: ui.LoadLevel2(); break;
            case TargetAction.LoadUpgrades: ui.LoadUpgrades(); break;
            case TargetAction.LoadMainMenu: ui.LoadMainMenu(); break;
        }
    }
}
