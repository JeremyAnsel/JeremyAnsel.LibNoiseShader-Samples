using JeremyAnsel.LibNoiseShader;
using JeremyAnsel.LibNoiseShader.Builders;
using JeremyAnsel.LibNoiseShader.IO;
using JeremyAnsel.LibNoiseShader.IO.Models;
using JeremyAnsel.LibNoiseShader.Maps;
using JeremyAnsel.LibNoiseShader.Modules;
using JeremyAnsel.LibNoiseShader.Renderers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JeremyAnsel.LibNoiseShaderSamples.Examples
{
    public static class TextureWood
    {
        public static void Build(int height, string outputDir)
        {
            Console.WriteLine("TextureWood");

            var noise = new Noise3D(0);

            // Base wood texture.  The base texture uses concentric cylinders aligned
            // on the z axis, like a log.
            CylinderModule baseWood = new()
            {
                Frequency = 16.0f
            };

            // Perlin noise to use for the wood grain.
            PerlinModule woodGrainNoise = new(noise)
            {
                SeedOffset = 0,
                Frequency = 48.0f,
                Persistence = 0.5f,
                Lacunarity = 2.20703125f,
                OctaveCount = 3
            };

            // Stretch the Perlin noise in the same direction as the center of the
            // log.  This produces a nice wood-grain texture.
            ScalePointModule scaledBaseWoodGrain = new(woodGrainNoise)
            {
                ScaleY = 0.25f
            };

            // Scale the wood-grain values so that they may be added to the base wood
            // texture.
            ScaleBiasModule woodGrain = new(scaledBaseWoodGrain)
            {
                Scale = 0.25f,
                Bias = 0.125f
            };

            // Add the wood grain texture to the base wood texture.
            AddModule combinedWood = new(baseWood, woodGrain);

            // Slightly perturb the wood texture for more realism.
            TurbulenceModule perturbedWood = new(noise, combinedWood)
            {
                SeedOffset = 1,
                Frequency = 4.0f,
                Power = 1.0f / 256.0f,
                Roughness = 4
            };

            // Cut the wood texture a small distance from the center of the "log".
            TranslatePointModule translatedWood = new(perturbedWood)
            {
                TranslateZ = 1.48f
            };

            // Cut the wood texture on an angle to produce a more interesting wood
            // texture.
            RotatePointModule rotatedWood = new(translatedWood);
            rotatedWood.SetAngles(84.0f, 0.0f, 0.0f);

            // Finally, perturb the wood texture to produce the final texture.
            TurbulenceModule finalWood = new(noise, rotatedWood)
            {
                SeedOffset = 2,
                Frequency = 2.0f,
                Power = 1.0f / 64.0f,
                Roughness = 4
            };

            // Given the wood noise module, create a non-seamless texture map, a
            // seamless texture map, and a spherical texture map.
            CreatePlanarTexture(noise, finalWood, false, height, string.Concat(outputDir, "TextureWoodPlane.png"));
            CreatePlanarTexture(noise, finalWood, true, height, string.Concat(outputDir, "TextureWoodSeamless.png"));
            CreateSphericalTexture(noise, finalWood, height, string.Concat(outputDir, "TextureWoodSphere.png"));
        }

        private static void CreateTextureColor(ImageRenderer renderer)
        {
            // Create a dark-stained wood palette (oak?)
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.00f, Color.FromArgb(255, 189, 94, 4));
            renderer.AddGradientPoint(0.50f, Color.FromArgb(255, 144, 48, 6));
            renderer.AddGradientPoint(1.00f, Color.FromArgb(255, 60, 10, 8));
        }

        private static void CreatePlanarTexture(Noise3D noise, IModule noiseModule, bool seamless, int height, string filename)
        {
            // Map the output values from the noise module onto a plane.  This will
            // create a two-dimensional noise map which can be rendered as a flat
            // texture map.
            PlaneBuilder plane = new(noiseModule, seamless, -1.0f, 1.0f, -1.0f, 1.0f);
            RenderTexture(noise, plane, filename, height, height);
        }

        private static void CreateSphericalTexture(Noise3D noise, IModule noiseModule, int height, string filename)
        {
            // Map the output values from the noise module onto a sphere.  This will
            // create a two-dimensional noise map which can be rendered as a spherical
            // texture map.
            SphereBuilder sphere = new(noiseModule, -90.0f, 90.0f, -180.0f, 180.0f);
            RenderTexture(noise, sphere, filename, height * 2, height);
        }

        private static void RenderTexture(Noise3D noise, IBuilder builder, string filename, int width, int height)
        {
            Console.WriteLine(filename);

            // Create the color gradients for the texture.
            ImageRenderer renderer = new(builder, false, false);
            CreateTextureColor(renderer);

            // Save to libnoise file
            LibNoiseShaderFile file = LibNoiseShaderFileWriteContext.BuildLibNoiseShaderFile(renderer, noise);
            file.Write(Path.ChangeExtension(filename, "libnoise"));

            // Render the texture.
            ColorMap data = MapGenerator.GenerateColorMapOnCpu(noise, renderer, width, height);

            // Write the texture.
            data.SaveBitmap(filename);
        }
    }
}
