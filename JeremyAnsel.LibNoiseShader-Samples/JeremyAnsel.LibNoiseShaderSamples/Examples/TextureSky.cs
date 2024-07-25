using JeremyAnsel.LibNoiseShader;
using JeremyAnsel.LibNoiseShader.Builders;
using JeremyAnsel.LibNoiseShader.IO;
using JeremyAnsel.LibNoiseShader.IO.Models;
using JeremyAnsel.LibNoiseShader.Maps;
using JeremyAnsel.LibNoiseShader.Modules;
using JeremyAnsel.LibNoiseShader.Renderers;
using System.Drawing;

namespace JeremyAnsel.LibNoiseShaderSamples.Examples
{
    public static class TextureSky
    {
        public static void Build(int height, string outputDir)
        {
            Console.WriteLine("TextureSky");

            var noise = new Noise3D(0);

            // This texture map is made up two layers.  The bottom layer is a wavy water
            // texture.  The top layer is a cloud texture.  These two layers are
            // combined together to create the final texture map.

            // Lower layer: water texture
            // --------------------------

            // Base of the water texture.  The Voronoi polygons generate the waves.  At
            // the center of the polygons, the values are at their lowest.  At the edges
            // of the polygons, the values are at their highest.  The values smoothly
            // change between the center and the edges of the polygons, producing a
            // wave-like effect.
            VoronoiModule baseWater = new(noise)
            {
                SeedOffset = 0,
                Frequency = 8.0f,
                IsDistanceApplied = true,
                Displacement = 0.0f
            };

            // Stretch the waves along the z axis.
            ScalePointModule baseStretchedWater = new(baseWater);
            baseStretchedWater.SetScale(1.0f, 1.0f, 3.0f);

            // Smoothly perturb the water texture for more realism.
            TurbulenceModule finalWater = new(noise, baseStretchedWater)
            {
                SeedOffset = 1,
                Frequency = 8.0f,
                Power = 1.0f / 32.0f,
                Roughness = 1
            };

            // Upper layer: cloud texture
            // --------------------------

            // Base of the cloud texture.  The billowy noise produces the basic shape
            // of soft, fluffy clouds.
            BillowModule cloudBase = new(noise)
            {
                SeedOffset = 2,
                Frequency = 2.0f,
                Persistence = 0.375f,
                Lacunarity = 2.12109375f,
                OctaveCount = 4
            };

            // Perturb the cloud texture for more realism.
            TurbulenceModule finalClouds = new(noise, cloudBase)
            {
                SeedOffset = 3,
                Frequency = 16.0f,
                Power = 1.0f / 64.0f,
                Roughness = 2
            };

            // Given the water and cloud noise modules, create a non-seamless texture
            // map, a seamless texture map, and a spherical texture map.
            CreatePlanarTexture(noise, finalWater, finalClouds, false, height, string.Concat(outputDir, "TextureSkyPlane.png"));
            CreatePlanarTexture(noise, finalWater, finalClouds, true, height, string.Concat(outputDir, "TextureSkySeamless.png"));
            CreateSphericalTexture(noise, finalWater, finalClouds, height, string.Concat(outputDir, "TextureSkySphere.png"));
        }

        private static void CreateTextureColorLayer1(ImageRenderer renderer)
        {
            // Create a water palette with varying shades of blue.
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.00f, Color.FromArgb(255, 48, 64, 192));
            renderer.AddGradientPoint(0.50f, Color.FromArgb(255, 96, 192, 255));
            renderer.AddGradientPoint(1.00f, Color.FromArgb(255, 255, 255, 255));
        }

        private static void CreateTextureColorLayer2(ImageRenderer renderer)
        {
            // Create an entirely white palette with varying alpha (transparency) values
            // for the clouds.  These transparent values allows the water to show
            // through.
            renderer.ClearGradient();
            renderer.AddGradientPoint(-1.00f, Color.FromArgb(0, 255, 255, 255));
            renderer.AddGradientPoint(-0.50f, Color.FromArgb(0, 255, 255, 255));
            renderer.AddGradientPoint(1.00f, Color.FromArgb(255, 255, 255, 255));
        }

        private static void CreatePlanarTexture(Noise3D noise, IModule lowerNoiseModule, IModule upperNoiseModule, bool seamless, int height, string filename)
        {
            // Map the output values from both noise module onto two planes.  This will
            // create two two-dimensional noise maps which can be rendered as two flat
            // texture maps.
            PlaneBuilder lowerPlane = new(lowerNoiseModule, noise.Seed, seamless, -1.0f, 1.0f, -1.0f, 1.0f);
            PlaneBuilder upperPlane = new(upperNoiseModule, noise.Seed, seamless, -1.0f, 1.0f, -1.0f, 1.0f);

            // Given these two noise maps, render the lower texture map, then render the
            // upper texture map on top of the lower texture map.
            RenderTexture(noise, lowerPlane, upperPlane, filename, height, height);
        }

        private static void CreateSphericalTexture(Noise3D noise, IModule lowerNoiseModule, IModule upperNoiseModule, int height, string filename)
        {
            // Map the output values from both noise module onto two spheres.  This will
            // create two two-dimensional noise maps which can be rendered as two
            // spherical texture maps.
            SphereBuilder lowerSphere = new(lowerNoiseModule, noise.Seed, -90.0f, 90.0f, -180.0f, 180.0f);
            SphereBuilder upperSphere = new(upperNoiseModule, noise.Seed, -90.0f, 90.0f, -180.0f, 180.0f);

            // Given these two noise maps, render the lower texture map, then render the
            // upper texture map on top of the lower texture map.
            RenderTexture(noise, lowerSphere, upperSphere, filename, height * 2, height);
        }

        private static void RenderTexture(Noise3D noise, IBuilder lowerNoise, IBuilder upperNoise, string filename, int width, int height)
        {
            Console.WriteLine(filename);

            // Create the color gradients for the lower texture.
            ImageRenderer lowerRenderer = new(lowerNoise, false, false);
            CreateTextureColorLayer1(lowerRenderer);

            // Set up us the texture renderer and pass the lower noise map to it.
            lowerRenderer.IsLightEnabled = true;
            lowerRenderer.LightAzimuth = 135.0f;
            lowerRenderer.LightElevation = 60.0f;
            lowerRenderer.LightContrast = 2.0f;
            lowerRenderer.LightColor = Color.FromArgb(0, 255, 255, 255);

            // Create the color gradients for the upper texture.
            ImageRenderer upperRenderer = new(lowerNoise, false, false);
            CreateTextureColorLayer2(upperRenderer);

            // Set up us the texture renderer and pass the upper noise map to it.  Also
            // use the lower texture map as a background so that the upper texture map
            // can be rendered on top of the lower texture map.
            upperRenderer.IsLightEnabled = false;

            // Set up the final renderer
            BlendRenderer finalRenderer = new(lowerRenderer, upperRenderer);

            // Save to libnoise file
            LibNoiseShaderFile file = LibNoiseShaderFileWriteContext.BuildLibNoiseShaderFile(finalRenderer, noise);
            file.Write(Path.ChangeExtension(filename, "libnoise"));

            // Render the texture.
            ColorMap data = MapGenerator.GenerateColorMapOnCpu(finalRenderer, width, height);

            // Write the texture.
            data.SaveBitmap(filename);
        }
    }
}
