// using Godot;
// using Godot.Collections;
// using System;

// public partial class Interpolator : Node
// {
//     Node3D parent;

//     private double timeElapsed = 0f;
//     private double timeToReachTarget = 0.05f;
//     private float movementThreshold = 0.05f;

//     private readonly Array<TransformUpdate> futureTransformUpdates = new Array<TransformUpdate>();

//     private float squareMovementThreshold;
//     private TransformUpdate to;
//     private TransformUpdate from;
//     private TransformUpdate previous;
//     // Called when the node enters the scene tree for the first time.
//     public override void _Ready()
//     {
//         parent = GetParent<Node3D>();

//         squareMovementThreshold = movementThreshold * movementThreshold;
//         to = new TransformUpdate(RiptideClient.ServerTick, parent.GlobalPosition);
//         from = new TransformUpdate(RiptideClient.InterpolationTick, parent.GlobalPosition);
//         previous = new TransformUpdate(RiptideClient.InterpolationTick, parent.GlobalPosition);
//     }

//     // Called every frame. 'delta' is the elapsed time since the previous frame.
//     public override void _Process(double delta)
//     {
//         if (RiptideServer.IsServerRunning()) return;

//         for (int i = 0; i < futureTransformUpdates.Count; i++)
//         {
//             if (RiptideClient.ServerTick >= futureTransformUpdates[i].Tick)
//             {
//                 previous = to;
//                 to = futureTransformUpdates[i];
//                 from = new TransformUpdate(RiptideClient.InterpolationTick, parent.GlobalPosition);

//                 futureTransformUpdates.RemoveAt(i);
//                 i--;
//                 timeElapsed = 0f;
//                 timeToReachTarget = (to.Tick - from.Tick) * GetPhysicsProcessDeltaTime();
//             }
//         }

//         timeElapsed += delta;
//         if (timeToReachTarget <= 0)
//         {
//             InterpolatePosition(0.02f);
//         }
//         else
//         {
//             InterpolatePosition(timeElapsed / timeToReachTarget);
//         }
//     }

//     private void InterpolatePosition(double lerpAmount)
//     {
//         if ((to.Position - previous.Position).LengthSquared() < squareMovementThreshold)
//         {
//             if (to.Position != from.Position)
//             {
//                 parent.GlobalPosition = parent.GlobalPosition.Lerp(to.Position, (float)lerpAmount);
//                 GD.Print($"From position: {from.Position}, To position: {to.Position}");
//                 return;
//             }
//         }
//         parent.GlobalPosition = LerpUnclamped(from.Position, to.Position, (float)lerpAmount);
//     }

//     public void NewUpdate(ushort tick, Vector3 position)
//     {
//         if (tick <= RiptideClient.InterpolationTick)
//             return;

//         for (int i = 0; i < futureTransformUpdates.Count; i++)
//         {
//             if (tick < futureTransformUpdates[i].Tick)
//             {
//                 futureTransformUpdates.Insert(i, new TransformUpdate(tick, position));
//                 return;
//             }
//         }

//         futureTransformUpdates.Add(new TransformUpdate(tick, position));
//     }

//     public Vector3 LerpUnclamped(Vector3 start_value, Vector3 end_value, float t)
//     {
//         return new Vector3(
//             start_value.X + (end_value.X - start_value.X) * t,
//             start_value.Y + (end_value.Y - start_value.Y) * t,
//             start_value.Z + (end_value.Z - start_value.Z) * t
//             );
//     }
// }
