using Godot;
using System;

public partial class PlayerInteraction : Node3D
{
    Player player;
    GameManager gameManager;

    [ExportGroup("Interaction properties")]
    [Export] private Camera3D camera;
    [Export] private RayCast3D interactionCast;
    [Export] private Marker3D hand;
    [Export] private Marker3D equip;
    [Export] private StaticBody3D staticBody;
    [Export] private Node3D EquipHandPosition;
    private dynamic PickedItem;
    // private ItemResource itemResource;
    private int _heldItemId;
    private float _heldItemCondition;
    public HeldItem _heldItem;

    public override void _Ready()
    {
        gameManager = GetTree().Root.GetNode<GameManager>("GameManager");

        player = GetParent<Player>();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            if (IsMovingItem() && Input.IsActionPressed("interact"))
            {
                InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
                staticBody.RotateY(mouseMotion.Relative.X * player.sensitivity);
                staticBody.RotateX(mouseMotion.Relative.Y * player.sensitivity);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleHeldItem();

        if (!IsMultiplayerAuthority()) return;

        HandleItem();

        HandleInteraction();

        FlipCar();
    }

    public bool IsHolding()
    {
        return _heldItemId != 0;
    }

    public bool IsMovingItem()
    {
        if (PickedItem is null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public void FlipCar()
    {
        if (Input.IsActionPressed("leftClick") && PickedItem is null)
        {
            if (interactionCast.GetCollider() is Vehicle vehicle)
            {
                var axis = -GlobalBasis.X;
                vehicle.Rpc(nameof(vehicle.Flip), axis);
            }
        }
    }

    public void HandleHeldItem()
    {
        if (IsHolding() && _heldItem is null)
        {
            HeldItem item = gameManager.GetItemResource(_heldItemId).ItemInHand.Instantiate() as HeldItem;
            if (item.holdType == 0)
            {
                EquipHandPosition.Position = new Vector3(0.274f, -0.175f, -0.357f);
                EquipHandPosition.Rotation = Vector3.Zero;
            }
            else if (item.holdType == 1)
            {
                EquipHandPosition.Position = new Vector3(0.274f, -0.211f, -0.357f);
                EquipHandPosition.RotationDegrees = new Vector3(0, 0, 90);
            }
            item.SetMultiplayerAuthority((int)player.Id);
            equip.AddChild(item, true);
            _heldItem = item;
        }
        else if (_heldItem is not null && !IsHolding())
        {
            _heldItem = null;
            equip.GetChild(0).QueueFree();
        }
    }

    public void HandleInteraction()
    {
        if (PickedItem is null && _heldItem is null && Input.IsActionJustPressed("leftClick") && interactionCast.GetCollider() is Interactable interactable)
        {
            interactable.Rpc(nameof(interactable.Press));
        }
    }

    public void HandleItem()
    {
        // Install vehicle part if holding a toolbox
        if (Input.IsActionJustPressed("rightClick") && PickedItem is Installable part && part.canBeInstalled && _heldItem is Toolbox)
        {
            PickedItem = null;
            part.Install();
            GD.Print("INSTALL PART");
        }
        else if (Input.IsActionJustPressed("rightClick") && interactionCast.GetCollider() is CarPart installedPart && _heldItem is Toolbox && interactionCast.IsColliding() && PickedItem is null)
        {
            GD.Print("Uninstalling part");
            installedPart.Uninstall();
        }
        else if (Input.IsActionJustPressed("rightClick") && interactionCast.GetCollider() is Door installedDoor && _heldItem is Toolbox && interactionCast.IsColliding() && PickedItem is null)
        {
            installedDoor.Uninstall();
        }

        // Equip held item
        else if (Input.IsActionJustPressed("equip") && PickedItem is Item && _heldItem is null)
        {
            ItemResource itemResource = gameManager.GetItemResource(PickedItem.id);
            if (itemResource.Equippable)
            {
                GD.Print("Item picked");
                SetHeldItem(itemResource.Id, PickedItem.condition);
                PickedItem.RpcId(1, nameof(PickedItem.QueueItemDestruction));
                PickedItem = null;
            }
        }
        // Drop held item
        else if (Input.IsActionJustPressed("equip") && PickedItem is null && _heldItem is not null)
        {
            DropHeldItem(true);
        }

        // Stop picking items when item held
        if (_heldItem is not Toolbox && _heldItem is not null) return;

        // Pick and drop item
        if (Input.IsActionJustPressed("leftClick"))
        {
            dynamic collider = interactionCast.GetCollider();
            if ((collider is Item || collider is Door || collider is Bone) && PickedItem is null && collider.playerHolding == 0)
            {
                PickItem(collider);
            }
            else if (PickedItem is not null)
            {
                DropPickedItem();
            }
        }

        // Throw item
        else if (Input.IsActionJustPressed("rightClick") && PickedItem is not null && _heldItem is null && PickedItem is not Door)
        {
            Vector3 throwDirection = (hand.GlobalPosition - camera.GlobalPosition).Normalized();
            PickedItem.Rpc("Throw", throwDirection, player.strength);
            PickedItem = null;
        }

        // Drop item if forced into a wall
        else if (PickedItem is Item && (hand.GlobalPosition - PickedItem.GlobalPosition).Length() > 1 && PickedItem.IsColliding())
        {
            DropPickedItem();
        }
        else if ((PickedItem is Bone || PickedItem is Door) && (hand.GlobalPosition - PickedItem.GlobalPosition).Length() > 0.9f)
        {
            DropPickedItem();
        }


        // Move item
        if (PickedItem is not null)
        {
            staticBody.Position = hand.Position;
            PickedItem.Rpc("Move", hand.GlobalPosition, staticBody.GlobalBasis, player.Id);
        }

        // Move item closer and further
        if (Input.IsActionJustPressed("scrollDown") && hand.Position.Z < -0.5f)
        {
            hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z + 0.1f);
        }
        else if (Input.IsActionJustPressed("scrollUp") && hand.Position.Z > -2)
        {
            hand.Position = new Vector3(hand.Position.X, hand.Position.Y, hand.Position.Z - 0.1f);
        }
    }

    public void DropHeldItem(bool pickDroppedItem)
    {
        long id;

        if (pickDroppedItem)
        {
            id = player.Id;
        }
        else
        {
            id = 0;
        }

        GD.Print(player.Id + " : " + _heldItemId + " : " + hand.GlobalPosition);
        gameManager.RpcId(1, nameof(gameManager.SpawnItem), id, _heldItemId, _heldItemCondition, hand.GlobalPosition, hand.GlobalRotation);
        SetHeldItem(0, 0);
    }

    public void DropPickedItem()
    {
        if (PickedItem is not null)
        {
            PickedItem.Rpc(nameof(PickedItem.Drop));
            PickedItem = null;
            hand.Position = new Vector3(0, 0, -1);
        }
    }

    public void PickItem(dynamic item)
    {
        PickedItem = item;
        hand.GlobalPosition = item.GlobalPosition;
        staticBody.GlobalBasis = PickedItem.GlobalBasis;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SetPickedItem(string itemPath)
    {
        GD.Print($"Should pickup {itemPath}");
        PickedItem = GetTree().Root.GetNode<Item>(itemPath);
    }

    public void SetHeldItem(int id, float condition)
    {
        _heldItemId = id;
        _heldItemCondition = condition;
    }
}

