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
    public static class TextureJade
    {
        public static void Build(int height, string outputDir)
        {
            Console.WriteLine("TextureJade");

            var noise = new Noise3D(0);

            // Primary jade texture.  The ridges from the ridged-multifractal module
            // produces the veins.
            RidgedMultiModule primaryJade = new(noise)
            {
                SeedOffset = 0,
                Frequency = 2.0f,
                Lacunarity = 2.20703125f,
                OctaveCount = 6
            };

            // Base of the secondary jade texture.  The base texture uses concentric
            // cylinders aligned on the z axis, which will eventually be perturbed.
            CylinderModule baseSecondaryJade = new()
            {
                Frequency = 2.0f
            };

            // Rotate the base secondary jade texture so that the cylinders are not
            // aligned with any axis.  This produces more variation in the secondary
            // jade texture since the texture is parallel to the y-axis.
            RotatePointModule rotatedBaseSecondaryJade = new(baseSecondaryJade);
            rotatedBaseSecondaryJade.SetAngles(90.0f, 25.0f, 5.0f);

            // Slightly perturb the secondary jade texture for more realism.
            TurbulenceModule perturbedBaseSecondaryJade = new(noise, rotatedBaseSecondaryJade)
            {
                SeedOffset = 1,
                Frequency = 4.0f,
                Power = 1.0f / 4.0f,
                Roughness = 4
            };

            // Scale the secondary jade texture so it contributes a small part to the
            // final jade texture.
            ScaleBiasModule secondaryJade = new(perturbedBaseSecondaryJade)
            {
                Scale = 0.25f,
                Bias = 0.0f
            };

            // Add the two jade textures together.  These two textures were produced
            // using different combinations of coherent noise, so the final texture will
            // have a lot of variation.
            AddModule combinedJade = new(primaryJade, secondaryJade);

            // Finally, perturb the combined jade textures to produce the final jade
            // texture.  A low roughness produces nice veins.
            TurbulenceModule finalJade = new(noise, combinedJade)
            {
                SeedOffset = 2,
                Frequency = 4.0f,
                Power = 1.0f / 16.0f,
                Roughness = 2
            };

            // Given the jade noise module, create a non-seamless texture map, a
            // seamless texture map, and a spherical texture map.
            CreatePlanarTexture(noise, finalJade, false, height, string.Concat(outputDir, "TextureJadePlane.png"));
            CreatePlanarTexture(noise, finalJade, true, height, string.Concat(outputDir, "TextureJadeSeamless.png"));
            CreateSphericalTexture(noise, finalJade, height, string.Concat(outputDir, "TextureJadeSphere.png"));
        }

        private static void CreateTextureColor(ImageRenderer renderer)
        {
            // Create a nice jade palette.
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.000f, Color.FromArgb(255, 24, 146, 102));
            renderer.AddGradientPoint(0.000f, Color.FromArgb(255, 78, 154, 115));
            renderer.AddGradientPoint(0.250f, Color.FromArgb(255, 128, 204, 165));
            renderer.AddGradientPoint(0.375f, Color.FromArgb(255, 78, 154, 115));
            renderer.AddGradientPoint(1.000f, Color.FromArgb(255, 29, 135, 102));
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
