using Xunit;
using NetTopologySuite.Geometries;
using LasUtility.Nls;

namespace LasUtility.Tests
{
    public class NlsTileNamerTests
    {
        [Fact]
        public void Decode_1kmx1km()
        {
            string sTileName = "V5211G2_1";
            TileNamer.Decode(sTileName, out Envelope env);

            Assert.Equal(518000, env.MinX);
            Assert.Equal(7581000, env.MinY);
            Assert.Equal(519000, env.MaxX);
            Assert.Equal(7582000, env.MaxY);
        }

        [Fact]
        public void Decode_12kmx12km()
        {
            string sTileName = "V4323L";

            TileNamer.Decode(sTileName, out Envelope env);

            Assert.Equal(428000, env.MinX);
            Assert.Equal(7554000, env.MinY);
            Assert.Equal(440000, env.MaxX);
            Assert.Equal(7566000, env.MaxY);
        }

        [Fact]
        public void Encode_1kmx1km() 
        {
            string name = TileNamer.Encode(426502, 7214414, 1000);
            Assert.Equal("R4412H3_6", name);
        }

        [Fact]
        public void Encode_3kmx3km() 
        {
            string name = TileNamer.Encode(426502, 7214414, 3000);
            Assert.Equal("R4412H3", name);
        }

        [Fact]
        public void Encode_12kmx12km()
        {
            string name = TileNamer.Encode(426502, 7214414, 12000);
            Assert.Equal("R4412R", name);
        }
    }
}