using Godot;
using System;

public interface IMount
{
    public void RemoveInstalledPart(int itemId, float condition, Vector3 position);

    public Error RpcId(long peerId, StringName method, params Variant[] args);
}
