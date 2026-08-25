using System;
using RainWorldDesktopPet.Core;
using RainWorldDesktopPet.Desktop;

namespace RainWorldDesktopPet.Physics
{
    public enum DesktopFoodKind
    {
        DangleFruit,
        EggBugEgg,
        SlimeMold,
        DandelionPeach,
        GlowWeed,
        Mushroom
    }

    public enum DesktopFoodState
    {
        Free,
        Claimed,
        Held,
        Biting,
        Ignored,
        Consumed,
        Expired
    }

    // A deliberately small desktop equivalent of Rain World's IPlayerEdible
    // contract. It preserves the item's visible and edible behavior without
    // importing Room/AbstractPhysicalObject/Creature graphs into the overlay.
    public sealed class DesktopFood
    {
        public const int DangleFruitInitialBites = 3;
        public const int DangleFruitFoodPoints = 1;
        public const int EggBugEggInitialBites = 2;
        public const int EggBugEggFoodPoints = 1;
        public const int DefaultLifetimeTicks = 1200;

        private Vec2 rotation;
        private Vec2 lastRotation;

        public DesktopFood(DesktopFoodKind kind, Vec2 position)
            : this(kind, position, 0.13)
        {
        }

        public DesktopFood(DesktopFoodKind kind, Vec2 position, double visualHue)
            : this(kind, position, visualHue, visualHue)
        {
        }

        public DesktopFood(DesktopFoodKind kind, Vec2 position, double visualHue,
            double visualVariant)
        {
            Kind = kind;
            Definition = DesktopFoodDefinitions.Get(kind);
            Chunk = new BodyChunk(0, position, Definition.Radius,
                Definition.Mass);
            State = DesktopFoodState.Free;
            InitialBites = Definition.InitialBites;
            BitesRemaining = InitialBites;
            FoodPoints = Definition.FoodPoints;
            VisualHue = visualHue - Math.Floor(visualHue);
            VisualVariant = MathUtil.Clamp01(visualVariant);
            DecorationCount = Definition.DecorationCount(VisualVariant);
            rotation = Vec2.Down;
            lastRotation = rotation;
        }

        public DesktopFoodKind Kind { get; private set; }
        public DesktopFoodDefinition Definition { get; private set; }
        public readonly BodyChunk Chunk;
        public DesktopFoodState State { get; private set; }
        public int InitialBites { get; private set; }
        public int BitesRemaining { get; private set; }
        public int FoodPoints { get; private set; }
        public double VisualHue { get; private set; }
        public double VisualVariant { get; private set; }
        public int DecorationCount { get; private set; }
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
                    State == DesktopFoodState.Claimed ||
                    State == DesktopFoodState.Ignored;
            }
        }
        public int SpriteFrame
        {
            get { return MathUtil.Clamp(InitialBites - BitesRemaining, 0, InitialBites - 1); }
        }
        public string FrontElement
        {
            get { return Definition.FrontElement(SpriteFrame); }
        }
        public string BackElement
        {
            get { return Definition.BackElement(SpriteFrame); }
        }
        public string DetailElement
        {
            get { return Definition.DetailElement(SpriteFrame); }
        }

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

        public bool Ignore()
        {
            if (State != DesktopFoodState.Free) return false;
            State = DesktopFoodState.Ignored;
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
            // Keep the previous appetite decision after an interrupted bite.
            // The owning manager can reacquire a dropped accepted item without
            // rerolling it into an ignored one.
            State = DesktopFoodState.Claimed;
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
            Chunk.Integrate(Definition.Gravity, Definition.AirFriction);
            if (Definition.DriftStrength > 0.0)
                Chunk.Velocity.X += Math.Sin(AgeTicks * 0.075 +
                    VisualVariant * Math.PI * 2.0) * Definition.DriftStrength;
            world.Resolve(Chunk, world.CurrentSnapshot, 0,
                Definition.SurfaceFriction, Definition.Bounce);
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
