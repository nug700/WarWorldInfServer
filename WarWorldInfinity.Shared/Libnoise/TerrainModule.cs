﻿using System.Collections;
using WarWorldInfinity.LibNoise;
using WarWorldInfinity.LibNoise.Models;
using WarWorldInfinity.LibNoise.Modifiers;

namespace WarWorldInfinity.LibNoise
{
    public class TerrainModule : IModule
    {
        public IModule module;
        public int seed;

        public TerrainModule(int seed)
        {
            this.seed = seed;

            RidgedMultifractal mountains = new RidgedMultifractal();
            mountains.Seed = seed;
            mountains.Frequency = 0.5;

            Billow hills = new Billow();
            hills.Seed = seed;
            hills.Frequency = 2;

            ScaleBiasOutput scaleHill = new ScaleBiasOutput(hills);
            scaleHill.Scale = 0.04;
            scaleHill.Bias = 0;

            ScaleBiasOutput scaleMountain = new ScaleBiasOutput(mountains);
            scaleMountain.Scale = 1.5;

            Perlin selectorControl = new Perlin();
            selectorControl.Seed = seed;
            selectorControl.Frequency = 0.10;
            selectorControl.Persistence = 0.25;

            Select selector = new Select(selectorControl, scaleMountain, scaleHill);
            selector.SetBounds(0, 1000);
            selector.EdgeFalloff = 0.5;
			module = selector;

        }

        public double GetValue(double x, double y, double z)
        {
            return module.GetValue(x, 0, z) / 3.5;
        }

        public double GetValue(double x, double y)
        {
            return GetValue(x, 0, y);
        }
    }
}
