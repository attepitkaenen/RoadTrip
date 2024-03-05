using Godot;
using System;

public interface IMounted
{
    public void Damage(float amount);

    public float GetCondition();

    public void SetCondition(float condition);

    public int GetId();

    public void SetId(int itemId);

    public void SetMount(PartMount mount);

    public void SetPositionAndRotation(Vector3 position, Vector3 rotation);

    public void Uninstall();
    void QueueFree();

}
