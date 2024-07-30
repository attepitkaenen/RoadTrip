using Godot;

enum Inputs
{
    up = 0,
    down,
    left,
    right,
    jump,
    crouch,
    sprint,
}

public struct InputPayload
{
    public int tick;
    public int playerId;
    public float rotation;
    public float[] inputs;
}

public struct PlayerStatePayload
{
    public int tick;
    public int playerId;
    public short health;
    public Vector3 syncVelocity;
    public Vector3 syncPosition;
    public float syncRotation;
    public float syncHeadRotationX;
    public float syncHeadRotationY;
    public ushort syncMovementStateId;
    public ushort syncHeldItemId;
}