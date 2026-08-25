using System;
using RainWorldDesktopPet.Core;
using RainWorldDesktopPet.Desktop;

namespace RainWorldDesktopPet.Physics
{
    public enum DesktopFoodKind
    {
        DangleFruit
    }

    public enum DesktopFoodState
    {
        Free,
        Claimed,
        Held,
        Biting,
        Consumed,
        Expired
    }

    // A deliberately small desktop equivalent of Rain World's IPlayerEdible
    // contract. It preserves the item's visible and edible behavior without
    // importing Room/AbstractPhysicalObject/Creature graphs into the overlay.
    public sealed class DesktopFood
    {
        private static readonly string[] FrontElements =
            { "DangleFruit0A", "DangleFruit1A", "DangleFruit2A" };
        private static readonly string[] BackElements =
            { "DangleFruit0B", "DangleFruit1B", "DangleFruit2B" };
        public const int DangleFruitInitialBites = 3;
        public const int DangleFruitFoodPoints = 1;
        public const int DefaultLifetimeTicks = 1200;

        private const double Gravity = 0.9;
        private const double AirFriction = 0.999;
        private const double SurfaceFriction = 0.7;
        private const double Bounce = 0.2;
        private Vec2 rotation;
        private Vec2 lastRotation;

        public DesktopFood(DesktopFoodKind kind, Vec2 position)
        {
            Kind = kind;
            Chunk = new BodyChunk(0, position, 8.0, 0.2);
            State = DesktopFoodState.Free;
            BitesRemaining = DangleFruitInitialBites;
            FoodPoints = DangleFruitFoodPoints;
            rotation = Vec2.Down;
            lastRotation = rotation;
        }

        public DesktopFoodKind Kind { get; private set; }
        public readonly BodyChunk Chunk;
        public DesktopFoodState State { get; private set; }
        public int BitesRemaining { get; private set; }
        public int FoodPoints { get; private set; }
        public int AgeTicks { get; private set; }
        public Vec2 Rotation { get { return rotation; } }
        public Vec2 LastRotation { get { return lastRotation; } }
        public bool IsActive
        {
            get
            {
                return State != DesktopFoodState.Consumed &&
                    State != DesktopFoodState.Expired;
            }
        }
        public bool IsPhysical
        {
            get
            {
                return State == DesktopFoodState.Free ||
                    State == DesktopFoodState.Claimed;
            }
        }
        public int SpriteFrame
        {
            get { return MathUtil.Clamp(DangleFruitInitialBites - BitesRemaining, 0, 2); }
        }
        public string FrontElement { get { return FrontElements[SpriteFrame]; } }
        public string BackElement { get { return BackElements[SpriteFrame]; } }

        public void SetCreationVelocity(Vec2 velocity)
        {
            Chunk.Velocity = velocity;
        }

        public bool Claim()
        {
            if (State != DesktopFoodState.Free) return false;
            State = DesktopFoodState.Claimed;
            return true;
        }

        public bool PickUp(Vec2 position)
        {
            if (State != DesktopFoodState.Free &&
                State != DesktopFoodState.Claimed) return false;
            State = DesktopFoodState.Held;
            HoldAt(position);
            return true;
        }

        public void HoldAt(Vec2 position)
        {
            if (State != DesktopFoodState.Held &&
                State != DesktopFoodState.Biting) return;
            Chunk.LastPosition = Chunk.Position;
            Chunk.Position = position;
            Chunk.Velocity = Vec2.Zero;
            lastRotation = rotation;
            rotation = Vec2.Down;
        }

        public bool BeginBiting()
        {
            if (State != DesktopFoodState.Held) return false;
            State = DesktopFoodState.Biting;
            return true;
        }

        public bool Bite()
        {
            if (State != DesktopFoodState.Biting || BitesRemaining <= 0) return false;
            BitesRemaining--;
            if (BitesRemaining == 0) State = DesktopFoodState.Consumed;
            return true;
        }

        public void Drop(Vec2 velocity)
        {
            if (State != DesktopFoodState.Held &&
                State != DesktopFoodState.Biting) return;
            State = DesktopFoodState.Free;
            Chunk.Velocity = velocity;
        }

        public void StepPhysics(DesktopCollisionWorld world)
        {
            if (world == null) throw new ArgumentNullException("world");
            if (!IsPhysical) return;

            AgeTicks++;
            if (AgeTicks >= DefaultLifetimeTicks)
            {
                State = DesktopFoodState.Expired;
                return;
            }

            lastRotation = rotation;
            Chunk.BeginTick();
            Chunk.Integrate(Gravity, AirFriction);
            world.Resolve(Chunk, world.CurrentSnapshot, 0, SurfaceFriction, Bounce);
            if (Chunk.Velocity.LengthSquared > 0.05)
                rotation = Chunk.Velocity.Normalized;
        }

        public void ApplyMovingSurfaceDelta(DesktopCollisionWorld world)
        {
            if (world == null || !IsPhysical || Chunk.SupportingSurfaceId == 0) return;
            Vec2 delta = world.GetSurfaceMovement(Chunk.SupportingSurfaceId,
                Chunk.SupportingSurfaceKind);
            Chunk.Position += delta;
            Chunk.LastPosition += delta;
        }
    }
}
