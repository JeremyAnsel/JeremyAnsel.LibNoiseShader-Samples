namespace JeremyAnsel.LibNoiseShaderSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string outputDir = @"..\..\";

                Examples.TextureGranite.Build(256, outputDir);
                Examples.TextureJade.Build(256, outputDir);
                Examples.TextureSky.Build(256, outputDir);
                Examples.TextureSlime.Build(256, outputDir);
                Examples.TextureWood.Build(256, outputDir);
                Examples.ComplexPlanet.Build(outputDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
