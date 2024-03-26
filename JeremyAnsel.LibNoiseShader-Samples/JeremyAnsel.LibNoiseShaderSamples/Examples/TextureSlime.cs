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
    public static class TextureSlime
    {
        public static void Build(int height, string outputDir)
        {
            Console.WriteLine("TextureSlime");

            var noise = new Noise3D(0);

            // Large slime bubble texture.
            BillowModule largeSlime = new(noise)
            {
                SeedOffset = 0,
                Frequency = 4.0f,
                Lacunarity = 2.12109375f,
                OctaveCount = 1
            };

            // Base of the small slime bubble texture.  This texture will eventually
            // appear inside cracks in the large slime bubble texture.
            BillowModule smallSlimeBase = new(noise)
            {
                SeedOffset = 1,
                Frequency = 24.0f,
                Lacunarity = 2.14453125f,
                OctaveCount = 1
            };

            // Scale and lower the small slime bubble values.
            ScaleBiasModule smallSlime = new(smallSlimeBase)
            {
                Scale = 0.5f,
                Bias = -0.5f
            };

            // Create a map that specifies where the large and small slime bubble
            // textures will appear in the final texture map.
            RidgedMultiModule slimeMap = new(noise)
            {
                SeedOffset = 0,
                Frequency = 2.0f,
                Lacunarity = 2.20703125f,
                OctaveCount = 3
            };

            // Choose between the large or small slime bubble textures depending on the
            // corresponding value from the slime map.  Choose the small slime bubble
            // texture if the slime map value is within a narrow range of values,
            // otherwise choose the large slime bubble texture.  The edge falloff is
            // non-zero so that there is a smooth transition between the two textures.
            SelectorModule slimeChooser = new(largeSlime, smallSlime, slimeMap);
            slimeChooser.SetBounds(-0.375f, 0.375f);
            slimeChooser.EdgeFalloff = 0.5f;

            // Finally, perturb the slime texture to add realism.
            TurbulenceModule finalSlime = new(noise, slimeChooser)
            {
                SeedOffset = 2,
                Frequency = 8.0f,
                Power = 1.0f / 32.0f,
                Roughness = 2
            };

            // Given the slime noise module, create a non-seamless texture map, a
            // seamless texture map, and a spherical texture map.
            CreatePlanarTexture(noise, finalSlime, false, height, string.Concat(outputDir, "TextureSlimePlane.png"));
            CreatePlanarTexture(noise, finalSlime, true, height, string.Concat(outputDir, "TextureSlimeSeamless.png"));
            CreateSphericalTexture(noise, finalSlime, height, string.Concat(outputDir, "TextureSlimeSphere.png"));
        }

        private static void CreateTextureColor(ImageRenderer renderer)
        {
            // Create a green slime palette.  A dirt brown color is used for very low
            // values while green is used for the rest of the values.
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.0000f, Color.FromArgb(255, 160, 64, 42));
            renderer.AddGradientPoint(0.0000f, Color.FromArgb(255, 64, 192, 64));
            renderer.AddGradientPoint(1.0000f, Color.FromArgb(255, 128, 255, 128));
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
