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
    public static class TextureGranite
    {
        public static void Build(int height, string outputDir)
        {
            Console.WriteLine("TextureGranite");

            var noise = new Noise3D(0);

            // Primary granite texture.  This generates the "roughness" of the texture
            // when lit by a light source.
            BillowModule primaryGranite = new(noise)
            {
                SeedOffset = 0,
                Frequency = 8.0f,
                Persistence = 0.625f,
                Lacunarity = 2.18359375f,
                OctaveCount = 6
            };

            // Use Voronoi polygons to produce the small grains for the granite texture.
            VoronoiModule baseGrains = new(noise)
            {
                SeedOffset = 1,
                Frequency = 16.0f,
                IsDistanceApplied = true
            };

            // Scale the small grain values so that they may be added to the base
            // granite texture.  Voronoi polygons normally generate pits, so apply a
            // negative scaling factor to produce bumps instead.
            ScaleBiasModule scaledGrains = new(baseGrains)
            {
                Scale = -0.5f,
                Bias = 0.0f
            };

            // Combine the primary granite texture with the small grain texture.
            AddModule combinedGranite = new(primaryGranite, scaledGrains);

            // Finally, perturb the granite texture to add realism.
            TurbulenceModule finalGranite = new(noise, combinedGranite)
            {
                SeedOffset = 2,
                Frequency = 4.0f,
                Power = 1.0f / 8.0f,
                Roughness = 6
            };

            // Given the granite noise module, create a non-seamless texture map, a
            // seamless texture map, and a spherical texture map.
            CreatePlanarTexture(noise, finalGranite, false, height, string.Concat(outputDir, "TextureGranitePlane.png"));
            CreatePlanarTexture(noise, finalGranite, true, height, string.Concat(outputDir, "TextureGraniteSeamless.png"));
            CreateSphericalTexture(noise, finalGranite, height, string.Concat(outputDir, "TextureGraniteSphere.png"));
        }

        private static void CreateTextureColor(ImageRenderer renderer)
        {
            // Create a gray granite palette.  Black and pink appear at either ends of
            // the palette; those colors provide the charactistic black and pink flecks
            // in granite.
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.0000f, Color.FromArgb(255, 0, 0, 0));
            renderer.AddGradientPoint(-0.9375f, Color.FromArgb(255, 0, 0, 0));
            renderer.AddGradientPoint(-0.8750f, Color.FromArgb(255, 216, 216, 242));
            renderer.AddGradientPoint(0.0000f, Color.FromArgb(255, 191, 191, 191));
            renderer.AddGradientPoint(0.5000f, Color.FromArgb(255, 210, 116, 125));
            renderer.AddGradientPoint(0.7500f, Color.FromArgb(255, 210, 113, 98));
            renderer.AddGradientPoint(1.0000f, Color.FromArgb(255, 255, 176, 192));
        }

        private static void CreatePlanarTexture(Noise3D noise, IModule noiseModule, bool seamless, int height, string filename)
        {
            // Map the output values from the noise module onto a plane.  This will
            // create a two-dimensional noise map which can be rendered as a flat
            // texture map.
            PlaneBuilder plane = new(noiseModule, noise.Seed, seamless, -1.0f, 1.0f, -1.0f, 1.0f);
            RenderTexture(noise, plane, filename, height, height);
        }

        private static void CreateSphericalTexture(Noise3D noise, IModule noiseModule, int height, string filename)
        {
            // Map the output values from the noise module onto a sphere.  This will
            // create a two-dimensional noise map which can be rendered as a spherical
            // texture map.
            SphereBuilder sphere = new(noiseModule, noise.Seed, -90.0f, 90.0f, -180.0f, 180.0f);
            RenderTexture(noise, sphere, filename, height * 2, height);
        }

        private static void RenderTexture(Noise3D noise, IBuilder builder, string filename, int width, int height)
        {
            Console.WriteLine(filename);

            // Create the color gradients for the texture.
            ImageRenderer renderer = new(builder, false, true);
            CreateTextureColor(renderer);

            // Set up us the texture renderer and pass the noise map to it.
            renderer.IsLightEnabled = true;
            renderer.LightAzimuth = 135.0f;
            renderer.LightElevation = 60.0f;
            renderer.LightContrast = 2.0f;
            renderer.LightColor = Color.FromArgb(0, 255, 255, 255);

            // Save to libnoise file
            LibNoiseShaderFile file = LibNoiseShaderFileWriteContext.BuildLibNoiseShaderFile(renderer, noise);
            file.Write(Path.ChangeExtension(filename, "libnoise"));

            // Render the texture.
            ColorMap data = MapGenerator.GenerateColorMapOnCpu(renderer, width, height);

            // Write the texture.
            data.SaveBitmap(filename);
        }
    }
}
