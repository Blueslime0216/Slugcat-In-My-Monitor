using System;
using System.Collections.Generic;

namespace RainWorldDesktopPet.Physics
{
    public enum DesktopFoodEffectKind
    {
        None,
        Mushroom
    }

    public sealed class DesktopFoodDefinition
    {
        internal DesktopFoodDefinition(DesktopFoodKind kind, int bites,
            int foodPoints, double radius, double mass, double gravity,
            double airFriction, double surfaceFriction, double bounce,
            double visualReach, string[] frontElements, string[] backElements,
            string[] detailElements, DesktopFoodEffectKind effectKind,
            int effectDurationTicks, int decorationMinimum,
            int decorationMaximum, double driftStrength)
        {
            Kind = kind;
            InitialBites = bites;
            FoodPoints = foodPoints;
            Radius = radius;
            Mass = mass;
            Gravity = gravity;
            AirFriction = airFriction;
            SurfaceFriction = surfaceFriction;
            Bounce = bounce;
            VisualReach = visualReach;
            this.frontElements = frontElements ?? new string[0];
            this.backElements = backElements ?? new string[0];
            this.detailElements = detailElements ?? new string[0];
            EffectKind = effectKind;
            EffectDurationTicks = effectDurationTicks;
            DecorationMinimum = decorationMinimum;
            DecorationMaximum = Math.Max(decorationMinimum, decorationMaximum);
            DriftStrength = driftStrength;
        }

        private readonly string[] frontElements;
        private readonly string[] backElements;
        private readonly string[] detailElements;

        public readonly DesktopFoodKind Kind;
        public readonly int InitialBites;
        public readonly int FoodPoints;
        public readonly double Radius;
        public readonly double Mass;
        public readonly double Gravity;
        public readonly double AirFriction;
        public readonly double SurfaceFriction;
        public readonly double Bounce;
        public readonly double VisualReach;
        public readonly DesktopFoodEffectKind EffectKind;
        public readonly int EffectDurationTicks;
        public readonly int DecorationMinimum;
        public readonly int DecorationMaximum;
        public readonly double DriftStrength;

        public string FrontElement(int frame)
        {
            return ElementAt(frontElements, frame);
        }

        public string BackElement(int frame)
        {
            return ElementAt(backElements, frame);
        }

        public string DetailElement(int frame)
        {
            return ElementAt(detailElements, frame);
        }

        public int DecorationCount(double variant)
        {
            if (DecorationMaximum <= DecorationMinimum) return DecorationMinimum;
            int count = DecorationMinimum + (int)Math.Floor(
                Math.Max(0.0, Math.Min(0.999999, variant)) *
                (DecorationMaximum - DecorationMinimum + 1));
            return Math.Min(DecorationMaximum, count);
        }

        private static string ElementAt(string[] elements, int frame)
        {
            if (elements.Length == 0) return null;
            int index = Math.Max(0, Math.Min(elements.Length - 1, frame));
            return elements[index];
        }
    }

    public static class DesktopFoodDefinitions
    {
        private static readonly IDictionary<DesktopFoodKind, DesktopFoodDefinition>
            Definitions = CreateDefinitions();

        public static DesktopFoodDefinition Get(DesktopFoodKind kind)
        {
            DesktopFoodDefinition definition;
            if (!Definitions.TryGetValue(kind, out definition))
                throw new ArgumentOutOfRangeException("kind", kind,
                    "Unknown desktop food kind.");
            return definition;
        }

        private static IDictionary<DesktopFoodKind, DesktopFoodDefinition>
            CreateDefinitions()
        {
            Dictionary<DesktopFoodKind, DesktopFoodDefinition> result =
                new Dictionary<DesktopFoodKind, DesktopFoodDefinition>();
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.DangleFruit,
                3, 1, 8.0, 0.2, 0.9, 0.999, 0.7, 0.2, 13.0,
                new[] { "DangleFruit0A", "DangleFruit1A", "DangleFruit2A" },
                new[] { "DangleFruit0B", "DangleFruit1B", "DangleFruit2B" },
                null, DesktopFoodEffectKind.None, 0, 0, 0, 0.0));
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.EggBugEgg,
                2, 1, 4.6, 0.2, 0.9, 0.999, 0.7, 0.2, 23.0,
                new[] { "DangleFruit0A", "DangleFruit1A" },
                new[] { "EggBugEggColor", "EggBugEggColorEaten" },
                new[] { "JetFishEyeA" }, DesktopFoodEffectKind.None, 0,
                5, 5, 0.0));
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.SlimeMold,
                3, 1, 5.0, 0.12, 0.9, 0.999, 0.7, 0.2, 25.0,
                new[] { "DangleFruit0A", "DangleFruit1A", "DangleFruit2A" },
                null, null, DesktopFoodEffectKind.None, 0, 8, 14, 0.0));
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.DandelionPeach,
                3, 1, 5.5, 0.34, 0.3, 0.996, 0.95, 0.4, 24.0,
                new[] { "DangleFruit0A", "DangleFruit1A", "DangleFruit2A" },
                new[] { "JellyFish0B", "JellyFish1B", "JellyFish2B" },
                new[] { "tinyStar" }, DesktopFoodEffectKind.None, 0,
                5, 7, 0.012));
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.GlowWeed,
                3, 1, 8.0, 0.2, 0.9, 0.999, 0.7, 0.2, 46.0,
                new[] { "DangleFruit0A" }, new[] { "DangleFruit0B" },
                new[] { "DangleFruit2A" }, DesktopFoodEffectKind.None, 0,
                2, 2, 0.0));
            Add(result, new DesktopFoodDefinition(DesktopFoodKind.Mushroom,
                1, 0, 2.0, 0.05, 0.9, 0.998, 0.7, 0.2, 38.0,
                new[] { "MushroomA" }, null, null,
                DesktopFoodEffectKind.Mushroom, 320, 6, 6, 0.0));
            return result;
        }

        private static void Add(
            IDictionary<DesktopFoodKind, DesktopFoodDefinition> definitions,
            DesktopFoodDefinition definition)
        {
            definitions.Add(definition.Kind, definition);
        }
    }
}
