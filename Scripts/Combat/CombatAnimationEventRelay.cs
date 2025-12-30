using System.Reflection;
using UnityEngine;

public interface ICombatSwingReceiver
{
    void SetSwingNumber(int number);
    void SetSwingRL();
    void SetSwingLR();
    void SetSlot();
    void ClearSwingNumber();
}

public class CombatAnimationEventRelay : MonoBehaviour
{
    public Component combat;

    private ICombatSwingReceiver _receiver;

    protected virtual Component GetDefaultCombatComponent()
    {
        return null;
    }

    private Component FindReceiverComponentInParentOrChildren()
    {
        Component parent = FindReceiverComponentInParent();
        if (parent != null) return parent;

        MonoBehaviour[] children = GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < children.Length; i++)
        {
            MonoBehaviour c = children[i];
            if (c is ICombatSwingReceiver)
                return c;
        }

        return null;
    }

    protected virtual void Awake()
    {
        if (combat == null)
            combat = GetDefaultCombatComponent();

        RefreshReceiver();
    }

    private void RefreshReceiver()
    {
        if (_receiver != null) return;

        if (combat != null)
        {
            _receiver = combat as ICombatSwingReceiver;
            if (_receiver != null) return;

            _receiver = FindReceiverInHierarchy(combat.gameObject);
            if (_receiver != null) return;
        }

        _receiver = FindReceiverInHierarchy(gameObject);
        if (_receiver != null) return;

        Component c = FindReceiverComponentInParentOrChildren();
        if (c != null)
        {
            combat = c;
            _receiver = c as ICombatSwingReceiver;
        }
    }

    private static ICombatSwingReceiver FindReceiverInHierarchy(GameObject root)
    {
        if (root == null) return null;

        MonoBehaviour[] components = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour c = components[i];
            if (c is ICombatSwingReceiver r)
                return r;
        }

        return null;
    }

    private Component FindReceiverComponentInParent()
    {
        MonoBehaviour[] components = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < components.Length; i++)
        {
            MonoBehaviour c = components[i];
            if (c is ICombatSwingReceiver)
                return c;
        }

        return null;
    }

    private void InvokeCombatNoArg(string methodName)
    {
        if (combat == null) return;
        if (ReferenceEquals(combat, this)) return;

        MethodInfo method = combat.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null) return;
        if (method.GetParameters().Length != 0) return;

        method.Invoke(combat, null);
    }

    public void SetSwingNumber(int number)
    {
        if (_receiver == null) RefreshReceiver();
        if (_receiver == null) return;
        _receiver.SetSwingNumber(number);
    }

    public void SetSwingRL()
    {
        if (_receiver == null) RefreshReceiver();
        if (_receiver == null) return;
        _receiver.SetSwingRL();
    }

    public void SetSwingLR()
    {
        if (_receiver == null) RefreshReceiver();
        if (_receiver == null) return;
        _receiver.SetSwingLR();
    }

    public void SetSlot()
    {
        if (_receiver == null) RefreshReceiver();
        if (_receiver == null) return;
        _receiver.SetSlot();
    }

    public void ClearSwingNumber()
    {
        if (_receiver == null) RefreshReceiver();
        if (_receiver == null) return;
        _receiver.ClearSwingNumber();
    }

    public void HighGuard()
    {
        InvokeCombatNoArg(nameof(HighGuard));
    }

    public void LightGuard()
    {
        InvokeCombatNoArg(nameof(LightGuard));
    }

    public void LeftGuard()
    {
        InvokeCombatNoArg(nameof(LeftGuard));
    }

    public void RightGuard()
    {
        InvokeCombatNoArg(nameof(RightGuard));
    }
}
